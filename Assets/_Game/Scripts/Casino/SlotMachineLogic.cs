using System;

namespace Tycoon3D.Casino
{
    /// <summary>Fuente de azar inyectable: permite pruebas deterministas y, más adelante, validar la ronda en el anfitrión.</summary>
    public interface IRandomSource
    {
        /// <summary>Entero uniforme en [0, maxExclusive).</summary>
        int Next(int maxExclusive);
    }

    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random random;
        public SystemRandomSource() => random = new Random();
        public SystemRandomSource(int seed) => random = new Random(seed);
        public int Next(int maxExclusive) => random.Next(maxExclusive);
    }

    public enum SlotSymbol { Chile, Mango, Concha, Hamburguesa, Fox }

    public enum SlotTier { Perdida, Par, TrioChile, TrioMango, TrioHamburguesa, JackpotFox }

    public readonly struct SlotRound
    {
        public readonly int Stake;          // pesos apostados (en un giro de cortesía, la apuesta que cubre la casa)
        public readonly bool FreeSpin;      // true: la casa pagó la apuesta
        public readonly SlotSymbol[] Reels; // tres rodillos
        public readonly SlotTier Tier;
        public readonly int Multiplier;
        public readonly int Payout;         // pesos que se acreditan al revelar (Stake * Multiplier)

        public SlotRound(int stake, bool freeSpin, SlotSymbol[] reels, SlotTier tier, int multiplier)
        {
            Stake = stake;
            FreeSpin = freeSpin;
            Reels = reels;
            Tier = tier;
            Multiplier = multiplier;
            Payout = checked(stake * multiplier);
        }

        /// <summary>Cambio neto del saldo del jugador con esta ronda.</summary>
        public int Net => FreeSpin ? Payout : Payout - Stake;
    }

    /// <summary>
    /// Mecánica del casino: una tragamonedas de tres rodillos con una tabla de premios fija sobre 100 resultados.
    /// Es pura (sin Unity) y determinista dado el generador de azar, de modo que pueda validarse en el anfitrión.
    /// Retorno esperado: 247 / 100 = 2.47 veces la apuesta (diseñado para acelerar el progreso con pesos virtuales).
    /// </summary>
    public static class SlotMachineLogic
    {
        public const int TableSize = 100;

        // Peso de cada nivel (suman 100) y su multiplicador.
        public static readonly SlotTier[] Tiers =
            { SlotTier.Perdida, SlotTier.Par, SlotTier.TrioChile, SlotTier.TrioMango, SlotTier.TrioHamburguesa, SlotTier.JackpotFox };
        public static readonly int[] Weights = { 38, 30, 18, 9, 4, 1 };
        public static readonly int[] Multipliers = { 0, 2, 3, 5, 12, 40 };

        public static int MultiplierOf(SlotTier tier) => Multipliers[Array.IndexOf(Tiers, tier)];

        public static SlotTier TierForRoll(int roll)
        {
            if (roll < 0 || roll >= TableSize) throw new ArgumentOutOfRangeException(nameof(roll));
            int acc = 0;
            for (int i = 0; i < Tiers.Length; i++)
            {
                acc += Weights[i];
                if (roll < acc) return Tiers[i];
            }
            throw new InvalidOperationException("La tabla de premios no suma " + TableSize);
        }

        /// <summary>Retorno esperado en pesos por cada 100 pesos apostados.</summary>
        public static int ExpectedReturnPer100()
        {
            int total = 0;
            for (int i = 0; i < Tiers.Length; i++) total += Weights[i] * Multipliers[i];
            return total;
        }

        public static SlotRound Play(int stake, bool freeSpin, IRandomSource rng, SlotPaytable table = null)
        {
            if (stake <= 0) throw new ArgumentOutOfRangeException(nameof(stake));
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            table = table ?? SlotPaytables.Clasica;
            SlotTier tier = table.TierForRoll(rng.Next(TableSize));
            return new SlotRound(stake, freeSpin, ReelsFor(tier, rng), tier, table.MultiplierOf(tier));
        }

        /// <summary>Reproduce una ronda con una tirada concreta (para pruebas y para el anfitrión).</summary>
        public static SlotRound PlayWithRoll(int stake, bool freeSpin, int roll, IRandomSource rng, SlotPaytable table = null)
        {
            table = table ?? SlotPaytables.Clasica;
            SlotTier tier = table.TierForRoll(roll);
            return new SlotRound(stake, freeSpin, ReelsFor(tier, rng), tier, table.MultiplierOf(tier));
        }

        private static SlotSymbol[] ReelsFor(SlotTier tier, IRandomSource rng)
        {
            switch (tier)
            {
                case SlotTier.TrioChile: return new[] { SlotSymbol.Chile, SlotSymbol.Chile, SlotSymbol.Chile };
                case SlotTier.TrioMango: return new[] { SlotSymbol.Mango, SlotSymbol.Mango, SlotSymbol.Mango };
                case SlotTier.TrioHamburguesa:
                    return new[] { SlotSymbol.Hamburguesa, SlotSymbol.Hamburguesa, SlotSymbol.Hamburguesa };
                case SlotTier.JackpotFox: return new[] { SlotSymbol.Fox, SlotSymbol.Fox, SlotSymbol.Fox };
                case SlotTier.Par:
                {
                    // dos iguales y uno distinto, en posiciones al azar
                    var all = (SlotSymbol[])Enum.GetValues(typeof(SlotSymbol));
                    SlotSymbol pair = all[rng.Next(all.Length)];
                    // sin bucle abierto: un RNG degenerado (siempre el mismo valor) no puede colgar el sorteo
                    SlotSymbol other = pair;
                    for (int attempt = 0; attempt < 8 && other == pair; attempt++) other = all[rng.Next(all.Length)];
                    if (other == pair) other = all[(Array.IndexOf(all, pair) + 1) % all.Length];
                    int odd = rng.Next(3);
                    var reels = new[] { pair, pair, pair };
                    reels[odd] = other;
                    return reels;
                }
                default:
                {
                    // tres distintos
                    var pool = new System.Collections.Generic.List<SlotSymbol>((SlotSymbol[])Enum.GetValues(typeof(SlotSymbol)));
                    var reels = new SlotSymbol[3];
                    for (int i = 0; i < 3; i++)
                    {
                        int pick = rng.Next(pool.Count);
                        reels[i] = pool[pick];
                        pool.RemoveAt(pick);
                    }
                    return reels;
                }
            }
        }
    }
}
