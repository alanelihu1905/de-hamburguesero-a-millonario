using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tycoon3D.Save
{
    public enum ParseStatus { Ok, Corrupt, FutureVersion }

    public delegate void SaveMigration(Dictionary<string, long> values);

    /// <summary>
    /// Formato de texto versionado: cabecera, version=N, pares clave=valor y un checksum FNV-1a al final. Una clave desconocida
    /// se ignora (compatibilidad hacia adelante), una clave ausente toma el valor por defecto, un valor fuera de rango o un
    /// checksum distinto marcan el archivo como dañado. Una versión mayor a la conocida nunca se interpreta ni se sobrescribe.
    /// </summary>
    public static class SaveCodec
    {
        public const int CurrentVersion = 1;
        public const string Header = "TYCOON3D-SAVE";

        private struct Field
        {
            public string Key;
            public Func<SaveData, long> Get;
            public Action<SaveData, long> Set;
            public long Min, Max;
        }

        private const long Big = 1_000_000_000_000L;

        private static readonly Field[] Fields =
        {
            F("saveCount", d => d.SaveCount, (d, v) => d.SaveCount = v, 0, Big),
            F("pesos", d => d.Pesos, (d, v) => d.Pesos = v, -1, Big),
            F("burgerStep", d => d.BurgerStep, (d, v) => d.BurgerStep = v, 0, 2),
            F("burgerSales", d => d.BurgerSales, (d, v) => d.BurgerSales = v, 0, Big),
            F("betIndex", d => d.BetIndex, (d, v) => d.BetIndex = v, 0, 3),
            F("chips", d => d.Chips, (d, v) => d.Chips = v, 0, Big),
            F("chipsBought", d => d.ChipsBought, (d, v) => d.ChipsBought = v, 0, Big),
            F("chipsCashed", d => d.ChipsCashed, (d, v) => d.ChipsCashed = v, 0, Big),
            F("cashierVisits", d => d.CashierVisits, (d, v) => d.CashierVisits = v, 0, Big),
            F("casinoRounds", d => d.CasinoRounds, (d, v) => d.CasinoRounds = v, 0, Big),
            F("casinoSpins", d => d.CasinoSpins, (d, v) => d.CasinoSpins = v, 0, Big),
            F("casinoWagered", d => d.CasinoWagered, (d, v) => d.CasinoWagered = v, 0, Big),
            F("casinoWon", d => d.CasinoWon, (d, v) => d.CasinoWon = v, 0, Big),
            F("casinoFreeSpins", d => d.CasinoFreeSpins, (d, v) => d.CasinoFreeSpins = v, 0, Big),
            F("casinoBest", d => d.CasinoBest, (d, v) => d.CasinoBest = v, 0, Big),
            F("pendingActive", d => d.PendingActive, (d, v) => d.PendingActive = v, 0, 1),
            F("pendingStake", d => d.PendingStake, (d, v) => d.PendingStake = v, 0, Big),
            F("pendingPayout", d => d.PendingPayout, (d, v) => d.PendingPayout = v, 0, Big),
            F("pendingFree", d => d.PendingFree, (d, v) => d.PendingFree = v, 0, 1),
            F("pendingChips", d => d.PendingChips, (d, v) => d.PendingChips = v, 0, 1),
            F("pendingRoundId", d => d.PendingRoundId, (d, v) => d.PendingRoundId = v, 0, Big),
        };

        private static Field F(string key, Func<SaveData, long> get, Action<SaveData, long> set, long min, long max)
            => new Field { Key = key, Get = get, Set = set, Min = min, Max = max };

        /// <summary>Migraciones por versión de origen (de N a N+1). Vacío mientras solo exista la versión 1.</summary>
        public static readonly Dictionary<int, SaveMigration> Migrations = new Dictionary<int, SaveMigration>();

        public static string Serialize(SaveData data)
        {
            var body = new StringBuilder();
            body.Append(Header).Append('\n');
            body.Append("version=").Append(CurrentVersion.ToString(CultureInfo.InvariantCulture)).Append('\n');
            foreach (var f in Fields)
                body.Append(f.Key).Append('=').Append(f.Get(data).ToString(CultureInfo.InvariantCulture)).Append('\n');
            string text = body.ToString();
            return text + "checksum=" + Checksum(text).ToString("x8", CultureInfo.InvariantCulture) + "\n";
        }

        public static ParseStatus TryParse(string text, out SaveData data, IDictionary<int, SaveMigration> migrations = null)
        {
            data = null;
            if (string.IsNullOrEmpty(text)) return ParseStatus.Corrupt;
            text = text.Replace("\r\n", "\n");
            int cut = text.LastIndexOf("checksum=", StringComparison.Ordinal);
            if (cut < 0) return ParseStatus.Corrupt;
            string body = text.Substring(0, cut);
            string tail = text.Substring(cut + "checksum=".Length).TrimEnd('\n', ' ');
            if (!uint.TryParse(tail, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint expected) ||
                expected != Checksum(body)) return ParseStatus.Corrupt;

            var lines = body.Split('\n');
            if (lines.Length == 0 || lines[0] != Header) return ParseStatus.Corrupt;
            var values = new Dictionary<string, long>();
            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].Length == 0) continue;
                int eq = lines[i].IndexOf('=');
                if (eq <= 0) return ParseStatus.Corrupt;
                if (!long.TryParse(lines[i].Substring(eq + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long v))
                    return ParseStatus.Corrupt;
                values[lines[i].Substring(0, eq)] = v;
            }
            if (!values.TryGetValue("version", out long version) || version < 0) return ParseStatus.Corrupt;
            if (version > CurrentVersion) return ParseStatus.FutureVersion;

            var steps = migrations ?? Migrations;
            while (version < CurrentVersion)
            {
                if (!steps.TryGetValue((int)version, out var migrate)) return ParseStatus.Corrupt;
                migrate(values);
                version++;
            }

            var result = new SaveData();
            foreach (var f in Fields)
            {
                if (!values.TryGetValue(f.Key, out long v)) continue;          // ausente: valor por defecto
                if (v < f.Min || v > f.Max) return ParseStatus.Corrupt;
                f.Set(result, v);
            }
            if (result.PendingActive == 1 && result.PendingStake <= 0) return ParseStatus.Corrupt;
            data = result;
            return ParseStatus.Ok;
        }

        private static uint Checksum(string text)
        {
            uint hash = 2166136261;
            foreach (char c in text) { hash ^= c; hash *= 16777619; }
            return hash;
        }
    }
}
