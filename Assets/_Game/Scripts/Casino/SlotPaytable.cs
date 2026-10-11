using System;

namespace Tycoon3D.Casino
{
    /// <summary>
    /// Tabla de premios de una máquina: peso (sobre 100) y multiplicador de cada nivel, en el orden de
    /// <see cref="SlotMachineLogic.Tiers"/>. Pura y sin Unity.
    /// </summary>
    public sealed class SlotPaytable
    {
        public string Id { get; }
        public string Name { get; }
        public int[] Weights { get; }
        public int[] Multipliers { get; }

        public SlotPaytable(string id, string name, int[] weights, int[] multipliers)
        {
            if (weights == null || multipliers == null || weights.Length != SlotMachineLogic.Tiers.Length ||
                multipliers.Length != SlotMachineLogic.Tiers.Length)
                throw new ArgumentException("La tabla debe tener un valor por nivel.");
            int sum = 0;
            foreach (int w in weights)
            {
                if (w < 0) throw new ArgumentException("Pesos negativos.");
                sum += w;
            }
            if (sum != SlotMachineLogic.TableSize) throw new ArgumentException("Los pesos deben sumar " + SlotMachineLogic.TableSize + ".");
            Id = id;
            Name = name;
            Weights = (int[])weights.Clone();
            Multipliers = (int[])multipliers.Clone();
        }

        public int MultiplierOf(SlotTier tier) => Multipliers[Array.IndexOf(SlotMachineLogic.Tiers, tier)];

        public SlotTier TierForRoll(int roll)
        {
            if (roll < 0 || roll >= SlotMachineLogic.TableSize) throw new ArgumentOutOfRangeException(nameof(roll));
            int acc = 0;
            for (int i = 0; i < Weights.Length; i++)
            {
                acc += Weights[i];
                if (roll < acc) return SlotMachineLogic.Tiers[i];
            }
            throw new InvalidOperationException("La tabla no suma " + SlotMachineLogic.TableSize);
        }

        /// <summary>Pesos devueltos por cada 100 apostados (247 = retorno x2.47).</summary>
        public int ExpectedReturnPer100()
        {
            int total = 0;
            for (int i = 0; i < Weights.Length; i++) total += Weights[i] * Multipliers[i];
            return total;
        }

        /// <summary>Porcentaje de giros con premio.</summary>
        public int HitPercent => SlotMachineLogic.TableSize - Weights[0];

        public int TopMultiplier
        {
            get
            {
                int top = 0;
                for (int i = 0; i < Weights.Length; i++) if (Weights[i] > 0 && Multipliers[i] > top) top = Multipliers[i];
                return top;
            }
        }
    }

    /// <summary>Las tres tablas del casino. Todas pagan más de lo que cobran: es un salto de progreso, no una apuesta equilibrada.</summary>
    public static class SlotPaytables
    {
        // orden de niveles: Perdida, Par, TrioChile, TrioMango, TrioHamburguesa, JackpotFox
        public static readonly SlotPaytable Clasica = new SlotPaytable("clasica", "Zorro Clásico",
            new[] { 38, 30, 18, 9, 4, 1 }, new[] { 0, 2, 3, 5, 12, 40 });

        public static readonly SlotPaytable MangoRapido = new SlotPaytable("mango", "Mango Rápido",
            new[] { 30, 36, 22, 9, 3, 0 }, new[] { 0, 2, 3, 5, 12, 40 });

        public static readonly SlotPaytable GranJackpot = new SlotPaytable("jackpot", "Gran Jackpot FOX",
            new[] { 60, 17, 11, 7, 4, 1 }, new[] { 0, 2, 3, 6, 15, 60 });

        public static readonly SlotPaytable[] All = { Clasica, MangoRapido, GranJackpot };
    }
}
