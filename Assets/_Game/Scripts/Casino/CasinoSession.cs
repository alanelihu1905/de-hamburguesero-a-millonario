using System;
using Tycoon3D.Economy;

namespace Tycoon3D.Casino
{
    public enum SpinStatus { Started, AdjustedBet, FreeSpin, Busy }

    /// <summary>
    /// Reglas de una sesión de casino sobre un monedero. Sin Unity: se prueba con dotnet y la misma lógica puede ejecutarse
    /// en el anfitrión. El casino siempre está abierto: si no hay pesos ni fichas para la apuesta mínima, el giro es de cortesía.
    /// Fichas: 1 ficha = $25, canje en ambos sentidos sin comisión (los pesos se conservan). Una apuesta se paga con fichas si
    /// alcanzan y, si no, con pesos; el premio vuelve en la misma moneda. Cada ronda se registra al aceptarse y se liquida una
    /// sola vez, aunque la app se cierre durante el giro (ver Snapshot y Restore).
    /// </summary>
    public sealed class CasinoSession
    {
        public static readonly int[] BetTiers = { 25, 100, 500, 2000 };
        public const int MinBet = 25;
        public const int ChipValue = 25;

        private readonly IRandomSource rng;
        private readonly Func<int> balance;
        private readonly Func<int, bool> trySpend;
        private readonly Action<int> earn;
        private SlotRound pending;
        private bool hasPending;
        private bool pendingWithChips;

        public CasinoSession(IRandomSource rng, Func<int> balance, Func<int, bool> trySpend, Action<int> earn)
        {
            this.rng = rng ?? throw new ArgumentNullException(nameof(rng));
            this.balance = balance ?? throw new ArgumentNullException(nameof(balance));
            this.trySpend = trySpend ?? throw new ArgumentNullException(nameof(trySpend));
            this.earn = earn ?? throw new ArgumentNullException(nameof(earn));
        }

        /// <summary>Se dispara cuando cambia algo que debe guardarse (apuesta aceptada, liquidación, canje de fichas).</summary>
        public event Action StateChanged;
        public event Action<MovementKind, int, MovementAccount, MovementAccount, int> MovementRecorded;

        public int BetIndex { get; private set; }
        public int CurrentBet => BetTiers[BetIndex];
        public bool Busy => hasPending;
        public int TotalSpins { get; private set; }
        public int BestPayout { get; private set; }
        public int TotalWon { get; private set; }
        public SlotRound? LastRound { get; private set; }
        public bool LastPaidWithChips { get; private set; }

        // Libro del casino (spec 15.4 / 19.7)
        public int RoundsStarted { get; private set; }
        public int PendingRoundId => hasPending ? RoundsStarted : 0;
        public int FreeSpins { get; private set; }
        /// <summary>Pesos (o su equivalente en fichas) cobrados por apuestas; los giros de cortesía no cobran.</summary>
        public int TotalWagered { get; private set; }
        /// <summary>Resultado para la casa: apostado menos premios (negativo = el jugador va ganando).</summary>
        public int HouseNet => TotalWagered - TotalWon;

        // Fichas
        public int Chips { get; private set; }
        public int ChipsBought { get; private set; }
        public int ChipsCashed { get; private set; }
        public int CashierVisits { get; private set; }
        public int ChipsValue => Chips * ChipValue;
        /// <summary>Pesos más valor de las fichas: no cambia al comprar o canjear fichas.</summary>
        public int NetWorth => balance() + ChipsValue;

        /// <summary>True cuando no hay ni pesos ni fichas para la apuesta mínima: el siguiente giro lo paga la casa.</summary>
        public bool NextSpinIsFree => balance() < MinBet && Chips < 1;

        /// <summary>True si la apuesta actual se pagaría con fichas.</summary>
        public bool NextSpinUsesChips => !NextSpinIsFree && Chips * ChipValue >= CurrentBet;

        public void CycleBet() => BetIndex = (BetIndex + 1) % BetTiers.Length;

        /// <summary>Elige la apuesta por posición (0 a 3); ignora índices fuera de rango.</summary>
        public bool SetBetIndex(int index)
        {
            if (hasPending || index < 0 || index >= BetTiers.Length) return false;
            BetIndex = index;
            return true;
        }

        // ------------------------------------------------------------------ fichas (cajera)
        /// <summary>Cambia pesos por fichas en una sola transacción: o se cobra y se entregan, o no pasa nada.</summary>
        public bool BuyChips(int count)
        {
            if (count <= 0 || hasPending) return false;
            long cost = (long)count * ChipValue;
            if (cost > int.MaxValue || !trySpend((int)cost)) return false;
            Chips += count;
            ChipsBought += count;
            CashierVisits++;
            MovementRecorded?.Invoke(MovementKind.BuyChips, (int)cost, MovementAccount.Wallet, MovementAccount.Chips, 0);
            StateChanged?.Invoke();
            return true;
        }

        /// <summary>Devuelve fichas a pesos al mismo valor. Devuelve los pesos entregados (0 si no se pudo).</summary>
        public int CashOutChips(int count)
        {
            if (count <= 0 || count > Chips || hasPending) return 0;
            long amount = (long)count * ChipValue;
            if (amount > int.MaxValue || balance() > int.MaxValue - amount) return 0;
            int pesos = (int)amount;
            Chips -= count;
            ChipsCashed += count;
            CashierVisits++;
            earn(pesos);
            MovementRecorded?.Invoke(MovementKind.CashOut, pesos, MovementAccount.Chips, MovementAccount.Wallet, 0);
            StateChanged?.Invoke();
            return pesos;
        }

        private bool Affordable(int bet, int pesos) => Chips * ChipValue >= bet || pesos >= bet;

        // ------------------------------------------------------------------ giro
        /// <summary>Cobra la apuesta y decide el resultado. El premio se acredita al llamar a Complete().</summary>
        public SpinStatus Begin(out SlotRound round, SlotPaytable table = null)
        {
            round = default;
            if (hasPending) return SpinStatus.Busy;
            int saldo = balance();
            SpinStatus status = SpinStatus.Started;
            bool withChips = false;
            if (saldo < MinBet && Chips < 1)
            {
                round = SlotMachineLogic.Play(MinBet, true, rng, table);
                status = SpinStatus.FreeSpin;
            }
            else
            {
                int index = BetIndex;
                while (index > 0 && !Affordable(BetTiers[index], saldo)) index--;
                if (index != BetIndex) { BetIndex = index; status = SpinStatus.AdjustedBet; }
                int bet = CurrentBet;
                if (Chips * ChipValue >= bet)
                {
                    Chips -= bet / ChipValue;
                    withChips = true;
                }
                else if (!trySpend(bet)) throw new InvalidOperationException("El saldo cambió durante la apuesta.");
                round = SlotMachineLogic.Play(bet, false, rng, table);
            }
            pending = round;
            hasPending = true;
            pendingWithChips = withChips;
            RoundsStarted++;
            if (round.FreeSpin) FreeSpins++; else TotalWagered += round.Stake;
            MovementRecorded?.Invoke(round.FreeSpin ? MovementKind.Courtesy : MovementKind.Stake,
                round.FreeSpin ? 0 : round.Stake, withChips ? MovementAccount.Chips : MovementAccount.Wallet,
                MovementAccount.Casino, RoundsStarted);
            StateChanged?.Invoke();
            return status;
        }

        /// <summary>Acredita el premio de la ronda pendiente y actualiza las estadísticas. Una segunda llamada falla.</summary>
        public SlotRound Complete()
        {
            if (!hasPending) throw new InvalidOperationException("No hay una ronda pendiente.");
            SlotRound round = pending;
            bool withChips = pendingWithChips;
            hasPending = false;
            pendingWithChips = false;
            Settle(round.Payout, withChips && !round.FreeSpin);
            if (round.Payout > 0) MovementRecorded?.Invoke(MovementKind.Payout, round.Payout, MovementAccount.Casino,
                withChips && !round.FreeSpin ? MovementAccount.Chips : MovementAccount.Wallet, RoundsStarted);
            LastRound = round;
            LastPaidWithChips = withChips && !round.FreeSpin;
            StateChanged?.Invoke();
            return round;
        }

        private void Settle(int payout, bool inChips)
        {
            if (payout > 0)
            {
                if (inChips) Chips += payout / ChipValue;
                else earn(payout);
            }
            TotalSpins++;
            TotalWon += payout;
            if (payout > BestPayout) BestPayout = payout;
        }

        // ------------------------------------------------------------------ persistencia
        public CasinoState Snapshot() => new CasinoState
        {
            BetIndex = BetIndex, Chips = Chips, ChipsBought = ChipsBought, ChipsCashed = ChipsCashed, CashierVisits = CashierVisits,
            Rounds = RoundsStarted, Spins = TotalSpins, Wagered = TotalWagered, Won = TotalWon, FreeSpins = FreeSpins, Best = BestPayout,
            PendingActive = hasPending, PendingStake = hasPending ? pending.Stake : 0, PendingPayout = hasPending ? pending.Payout : 0,
            PendingFree = hasPending && pending.FreeSpin, PendingChips = hasPending && pendingWithChips, PendingRoundId = PendingRoundId
        };

        /// <summary>
        /// Restaura el libro y las fichas. Si el estado trae una ronda aceptada sin liquidar (la app se cerró girando), se liquida
        /// aquí, una sola vez, y devuelve los pesos de premio acreditados (equivalente en fichas si se apostó con fichas).
        /// </summary>
        public int Restore(CasinoState s)
        {
            hasPending = false;
            pendingWithChips = false;
            BetIndex = Math.Max(0, Math.Min(BetTiers.Length - 1, s.BetIndex));
            Chips = Math.Max(0, s.Chips);
            ChipsBought = Math.Max(0, s.ChipsBought);
            ChipsCashed = Math.Max(0, s.ChipsCashed);
            CashierVisits = Math.Max(0, s.CashierVisits);
            RoundsStarted = Math.Max(0, s.Rounds);
            TotalSpins = Math.Max(0, s.Spins);
            TotalWagered = Math.Max(0, s.Wagered);
            TotalWon = Math.Max(0, s.Won);
            FreeSpins = Math.Max(0, s.FreeSpins);
            BestPayout = Math.Max(0, s.Best);
            int recovered = 0;
            if (s.PendingActive)
            {
                recovered = Math.Max(0, s.PendingPayout);
                Settle(recovered, s.PendingChips && !s.PendingFree);
                if (recovered > 0) MovementRecorded?.Invoke(MovementKind.Payout, recovered, MovementAccount.Casino,
                    s.PendingChips && !s.PendingFree ? MovementAccount.Chips : MovementAccount.Wallet, s.PendingRoundId);
                StateChanged?.Invoke();
            }
            return recovered;
        }
    }
}
