using System;
using Tycoon3D.Core;
using Tycoon3D.Economy;
using Tycoon3D.Interaction;
using UnityEngine;

namespace Tycoon3D.Casino
{
    /// <summary>
    /// Casino FOX: siempre abierto, sin desbloqueo. Une la lógica pura (CasinoSession) con el Wallet de la escena y con la
    /// presentación (rodillos que giran, luces, sonido). Un giro a la vez para todo el casino.
    /// Atajos cerca de una máquina: 1-4 eligen la apuesta, Q y la rueda del mouse la cambian, F reduce las luces parpadeantes.
    /// </summary>
    public sealed class CasinoService : MonoBehaviour
    {
        public const string ReduceFlashKey = "casino.reduceFlashing";
        public const float HotkeyDistance = 3.6f;

        [SerializeField] private Wallet wallet;
        [SerializeField] private CasinoSounds sounds;
        [SerializeField] private Texture2D symbolAtlas;

        private CasinoSession session;
        private IRandomSource random = new SystemRandomSource();
        private float revealAt;
        private float nextFlicker;
        private SlotMachineStation active;
        private SlotMachineStation[] machines = new SlotMachineStation[0];
        private readonly System.Random visualRandom = new System.Random();
        private bool reduceLoaded, reduceFlashing;
        private GameProfile profile;
        private CasinoState? carry;

        public event Action Started;
        public event Action<SlotRound> Revealed;
        public event Action<MovementKind, int, MovementAccount, MovementAccount, int> MovementRecorded;
        /// <summary>La cajera cambió o canjeó fichas: número de fichas y si fue una compra (true) o un canje (false).</summary>
        public event Action<int, bool> ChipsExchanged;

        public string Message { get; private set; } = "Casino FOX abierto siempre.";
        public SlotSymbol[] DisplayReels { get; } = { SlotSymbol.Chile, SlotSymbol.Mango, SlotSymbol.Concha };
        public int DisplayVersion { get; private set; }
        public float LastActivityTime { get; private set; } = -100f;
        public float LastRevealTime { get; private set; } = -100f;
        public SlotRound? LastRound { get; private set; }
        public SlotMachineStation LastMachine { get; private set; }
        public SlotMachineStation NearestMachine { get; private set; }
        public float NearestDistance { get; private set; } = 999f;
        public CasinoSession Session { get { Ensure(); return session; } }
        public CasinoSounds Sounds => sounds;
        public Texture2D SymbolAtlas => symbolAtlas;
        public bool Spinning => session != null && session.Busy;
        public int PendingStake { get; private set; }
        public SlotMachineStation[] Machines => machines;

        /// <summary>Aviso breve (cajera, liquidación de una ronda pendiente). El HUD lo muestra unos segundos.</summary>
        public string ToastText { get; private set; } = string.Empty;
        public float ToastUntil { get; private set; } = -100f;

        public void Toast(string text, float seconds = 4.5f)
        {
            ToastText = text;
            ToastUntil = Time.time + seconds;
            LastActivityTime = Time.time;
        }

        /// <summary>Conecta con el perfil guardado: restaura libro, fichas y ronda pendiente. Devuelve el premio liquidado, si lo había.</summary>
        public int AttachProfile(GameProfile p)
        {
            profile = p;
            Ensure();
            int recovered = 0;
            if (p.Data.HasProfile) recovered = session.Restore(CasinoProfileMapping.Read(p.Data));
            session.StateChanged -= p.MarkDirty;
            session.StateChanged += p.MarkDirty;
            if (recovered > 0) Toast("Se liquidó tu giro pendiente: +$" + recovered, 6f);
            return recovered;
        }

        // ------------------------------------------------------------------ cajera: fichas
        public const int DefaultChipLot = 20;

        /// <summary>Fichas por canje de la cajera (de los datos editables; 20 por defecto).</summary>
        public int ChipLot { get; private set; } = DefaultChipLot;

        public void ApplyConfig(EconomyConfig config)
        {
            if (config != null) ChipLot = config.ChipLot;
        }

        /// <summary>Fichas que compraría el siguiente canje: un lote de 20 (o lo que alcance, mínimo una).</summary>
        public int ChipsToBuy { get { Ensure(); return Mathf.Min(ChipLot, wallet.Pesos / CasinoSession.ChipValue); } }

        /// <summary>Hora del reloj del juego (12 si no hay ciclo de día y noche, como en las pruebas sin escena).</summary>
        public float HourOfDay { get { var cycle = Tycoon3D.World.DayNightCycle.Current; return cycle != null ? cycle.Clock.Hour : 12f; } }

        /// <summary>La cajera cambia pesos por fichas solo en su turno; las máquinas y el canje de fichas no dependen de ella.</summary>
        public bool CashierOnDuty => CashierSchedule.OnDuty(HourOfDay);

        public bool BuyChips()
        {
            Ensure();
            if (session.Busy) { Toast("Espera: los rodillos siguen girando."); return false; }
            if (!CashierOnDuty)
            {
                Toast("La cajera está en descanso (" + CashierSchedule.ShiftText + "). Las máquinas siguen abiertas: juega con pesos.", 6f);
                return false;
            }
            int n = ChipsToBuy;
            if (n <= 0) { Toast("Una ficha cuesta $" + CasinoSession.ChipValue + ". Con menos, las máquinas te dan giros de cortesía."); return false; }
            int cost = n * CasinoSession.ChipValue;
            if (!session.BuyChips(n)) { Toast("No se pudo completar el cambio."); return false; }
            Toast("Cajera: cambiaste $" + cost + " por " + n + (n == 1 ? " ficha" : " fichas") + ". 1 ficha = $" + CasinoSession.ChipValue + ".");
            PlayCoin();
            ChipsExchanged?.Invoke(n, true);
            return true;
        }

        public bool CashOutChips()
        {
            Ensure();
            if (session.Busy) { Toast("Espera: los rodillos siguen girando."); return false; }
            int n = session.Chips;
            if (n <= 0) { Toast("No tienes fichas que canjear."); return false; }
            int pesos = session.CashOutChips(n);
            if (pesos <= 0) { Toast("No se pudo completar el canje."); return false; }
            Toast((CashierOnDuty ? "Cajera: canjeaste " : "Ventanilla automática: canjeaste ") + n + (n == 1 ? " ficha" : " fichas") + " por $" + pesos + ".");
            PlayCoin();
            ChipsExchanged?.Invoke(n, false);
            return true;
        }

        private void PlayCoin()
        {
            if (sounds == null) return;
            var cam = Camera.main;
            sounds.PlayAt(sounds.coin, cam != null ? cam.transform.position : transform.position, .9f);
        }

        public bool ReduceFlashing
        {
            get
            {
                if (!reduceLoaded) { reduceFlashing = PlayerPrefs.GetInt(ReduceFlashKey, 0) == 1; reduceLoaded = true; }
                return reduceFlashing;
            }
            set
            {
                reduceFlashing = value;
                reduceLoaded = true;
                PlayerPrefs.SetInt(ReduceFlashKey, value ? 1 : 0);
            }
        }

        public void Configure(Wallet money, CasinoSounds soundBank, Texture2D atlas)
        {
            wallet = money;
            sounds = soundBank;
            symbolAtlas = atlas;
            session = null;
        }

        /// <summary>Sustituye el generador de azar (pruebas deterministas y, a futuro, el anfitrión).</summary>
        public void UseRandom(IRandomSource source)
        {
            random = source ?? throw new ArgumentNullException(nameof(source));
            if (session != null) carry = session.Snapshot();            // el libro y las fichas no se pierden al cambiar el azar
            session = null;
        }

        private void Awake() => Ensure();

        public JackpotCelebration Celebration { get; private set; }

        private void Start()
        {
            machines = FindObjectsByType<SlotMachineStation>(FindObjectsSortMode.None);
            Celebration = GetComponent<JackpotCelebration>();
            if (Celebration == null) Celebration = gameObject.AddComponent<JackpotCelebration>();
            Celebration.Bind(this);
        }

        private void Ensure()
        {
            if (session != null) return;
            if (wallet == null) wallet = FindFirstObjectByType<Wallet>();
            if (wallet == null) throw new InvalidOperationException("Casino sin Wallet en la escena.");
            session = new CasinoSession(random, () => wallet.Pesos, wallet.TrySpend, wallet.Earn);
            session.MovementRecorded += OnSessionMovement;
            if (carry.HasValue) { session.Restore(carry.Value); carry = null; }
            if (profile != null) session.StateChanged += profile.MarkDirty;
        }

        private void OnSessionMovement(MovementKind kind, int amount, MovementAccount from, MovementAccount to, int roundId)
            => MovementRecorded?.Invoke(kind, amount, from, to, roundId);

        /// <summary>Gira en una máquina concreta (con su tabla y su ritmo). Sin máquina se usa la tabla clásica.</summary>
        public bool TrySpin(SlotMachineStation machine = null)
        {
            Ensure();
            if (session.Busy) { Message = "Los rodillos siguen girando…"; return false; }
            var table = machine != null ? machine.Paytable : SlotPaytables.Clasica;
            SpinStatus status = session.Begin(out SlotRound round, table);
            PendingStake = round.Stake;
            float seconds = machine != null ? machine.SpinSeconds : 1.4f;
            revealAt = Time.time + seconds;
            LastActivityTime = Time.time;
            active = machine;
            LastMachine = machine;
            switch (status)
            {
                case SpinStatus.FreeSpin: Message = "Giro de cortesía: la casa invita $" + round.Stake + "."; break;
                case SpinStatus.AdjustedBet: Message = "Apuesta ajustada a $" + session.CurrentBet + " por tu saldo. Girando…"; break;
                default: Message = "Girando… apuesta $" + round.Stake; break;
            }
            if (machine != null) machine.OnSpinStarted(round, seconds);
            Started?.Invoke();
            return true;
        }

        public void CycleBet()
        {
            Ensure();
            if (session.Busy) return;
            session.CycleBet();
            AfterBetChange();
        }

        /// <summary>Elige la apuesta por posición (0 a 3).</summary>
        public bool SetBet(int index)
        {
            Ensure();
            if (!session.SetBetIndex(index)) return false;
            AfterBetChange();
            return true;
        }

        private void AfterBetChange()
        {
            LastActivityTime = Time.time;
            Message = session.NextSpinIsFree ? "Sin saldo: el siguiente giro es de cortesía."
                                             : "Apuesta: $" + session.CurrentBet;
            if (sounds != null)
            {
                var cam = Camera.main;
                sounds.PlayAt(sounds.betChange, cam != null ? cam.transform.position : transform.position, .8f);
            }
        }

        /// <summary>Termina el giro actual ya (pruebas y botón de omitir).</summary>
        public void RevealNow()
        {
            if (session != null && session.Busy) Reveal();
        }

        private void Update()
        {
            HandleInput();
            if (session == null || !session.Busy) return;
            if (Time.time >= revealAt) { Reveal(); return; }
            if (Time.time >= nextFlicker)
            {
                nextFlicker = Time.time + .09f;
                var all = (SlotSymbol[])Enum.GetValues(typeof(SlotSymbol));
                for (int i = 0; i < DisplayReels.Length; i++) DisplayReels[i] = all[visualRandom.Next(all.Length)];
                DisplayVersion++;
            }
        }

        private void HandleInput()
        {
            var cam = Camera.main;
            if (cam == null || machines == null) return;
            NearestDistance = 999f;
            NearestMachine = null;
            Vector3 p = cam.transform.position;
            foreach (var m in machines)
            {
                if (m == null) continue;
                float d = Vector3.Distance(p, m.transform.position);
                if (d < NearestDistance) { NearestDistance = d; NearestMachine = m; }
            }
            if (NearestMachine == null || NearestDistance > HotkeyDistance || Cursor.lockState != CursorLockMode.Locked) return;
            for (int i = 0; i < CasinoSession.BetTiers.Length; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) SetBet(i);
            float wheel = Input.mouseScrollDelta.y;
            if (wheel > .1f) SetBet(Mathf.Min(CasinoSession.BetTiers.Length - 1, session != null ? session.BetIndex + 1 : 0));
            else if (wheel < -.1f) SetBet(Mathf.Max(0, session != null ? session.BetIndex - 1 : 0));
            if (Input.GetKeyDown(InputBindings.Lights))
            {
                ReduceFlashing = !ReduceFlashing;
                Message = ReduceFlashing ? "Luces fijas activadas (sin parpadeos)." : "Luces animadas activadas.";
                LastActivityTime = Time.time;
            }
        }

        private void Reveal()
        {
            SlotRound round = session.Complete();
            for (int i = 0; i < DisplayReels.Length; i++) DisplayReels[i] = round.Reels[i];
            DisplayVersion++;
            LastActivityTime = Time.time;
            LastRevealTime = Time.time;
            LastRound = round;
            Message = Describe(round);
            if (active != null) active.OnRevealed(round);
            Revealed?.Invoke(round);
        }

        public static string Describe(SlotRound r)
        {
            string premio = " +$" + r.Payout;
            switch (r.Tier)
            {
                case SlotTier.JackpotFox: return "¡JACKPOT FOX x" + r.Multiplier + "!" + premio;
                case SlotTier.TrioHamburguesa: return "¡Tres hamburguesas x" + r.Multiplier + "!" + premio;
                case SlotTier.TrioMango: return "¡Tres mangos x" + r.Multiplier + "!" + premio;
                case SlotTier.TrioChile: return "¡Tres chiles x" + r.Multiplier + "!" + premio;
                case SlotTier.Par: return "¡Par! x" + r.Multiplier + premio;
                default: return r.FreeSpin ? "Sin premio, pero la casa invitó. ¡Otra vez!" : "Sin premio. ¡Otra vez!";
            }
        }
    }
}
