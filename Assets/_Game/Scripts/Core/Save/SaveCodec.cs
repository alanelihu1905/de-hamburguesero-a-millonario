using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Tycoon3D.Economy;

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
        public const int CurrentVersion = 4;
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
            F("cashboxInit", d => d.CashboxInit, (d, v) => d.CashboxInit = v, 0, 1),
            F("cashboxFunds", d => d.CashboxFunds, (d, v) => d.CashboxFunds = v, 0, int.MaxValue),
            F("cashboxOwed", d => d.CashboxOwed, (d, v) => d.CashboxOwed = v, 0, int.MaxValue),
            F("cashboxRefilled", d => d.CashboxRefilled, (d, v) => d.CashboxRefilled = v, 0, Big),
            F("clockMinutes", d => d.ClockMinutes, (d, v) => d.ClockMinutes = v, -1, Big),
            F("ledgerSequence", d => d.LedgerSequence, (d, v) => d.LedgerSequence = v, 0, Big),
            F("ledgerOpeningPesos", d => d.LedgerOpeningPesos, (d, v) => d.LedgerOpeningPesos = v, -Big, Big),
            F("ledgerOpeningChips", d => d.LedgerOpeningChips, (d, v) => d.LedgerOpeningChips = v, -Big, Big),
        };

        private static Field F(string key, Func<SaveData, long> get, Action<SaveData, long> set, long min, long max)
            => new Field { Key = key, Get = get, Set = set, Min = min, Max = max };

        /// <summary>v1 conserva saldos como punto inicial; el historial anterior no se puede reconstruir.</summary>
        public static readonly Dictionary<int, SaveMigration> Migrations = new Dictionary<int, SaveMigration>
        {
            [1] = values =>
            {
                long pesos = values.TryGetValue("pesos", out long p) ? p : -1;
                long chips = values.TryGetValue("chips", out long c) ? c : 0;
                values["ledgerOpeningPesos"] = Math.Max(0, pesos);
                values["ledgerOpeningChips"] = Math.Max(0, chips);
                values["ledgerCount"] = 0;
            },
            // v2 -> v3: la caja del casino no existía; sin cashboxInit empieza con su fondo inicial y sin deudas
            [2] = values => { values["cashboxInit"] = 0; },
            // v3 -> v4: el reloj del juego no existía; sin clockMinutes arranca a las 08:00 del día 1
            [3] = values => { values["clockMinutes"] = -1; }
        };

        public static string Serialize(SaveData data)
        {
            var body = new StringBuilder();
            body.Append(Header).Append('\n');
            body.Append("version=").Append(CurrentVersion.ToString(CultureInfo.InvariantCulture)).Append('\n');
            foreach (var f in Fields)
                body.Append(f.Key).Append('=').Append(f.Get(data).ToString(CultureInfo.InvariantCulture)).Append('\n');
            var entries = data.LedgerEntries ?? new Movement[0];
            if (entries.Length > MovementLedger.Capacity) throw new ArgumentException("Libro demasiado largo.");
            body.Append("ledgerCount=").Append(entries.Length).Append('\n');
            for (int i = 0; i < entries.Length; i++)
            {
                string key = "ledger" + i.ToString(CultureInfo.InvariantCulture);
                Append(body, key + "Id", entries[i].Id);
                Append(body, key + "Kind", (int)entries[i].Kind);
                Append(body, key + "Amount", entries[i].Amount);
                Append(body, key + "From", (int)entries[i].From);
                Append(body, key + "To", (int)entries[i].To);
                Append(body, key + "Round", entries[i].RoundId);
            }
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
                if (!steps.TryGetValue((int)version, out var migrate) && !Migrations.TryGetValue((int)version, out migrate)) return ParseStatus.Corrupt;
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
            if (!values.TryGetValue("ledgerCount", out long count)) count = 0;
            if (count < 0 || count > MovementLedger.Capacity) return ParseStatus.Corrupt;
            var entries = new Movement[(int)count];
            long previous = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                string key = "ledger" + i.ToString(CultureInfo.InvariantCulture);
                if (!Read(values, key + "Id", 1, Big, out long id) || id <= previous ||
                    !Read(values, key + "Kind", 0, (int)MovementKind.Refill, out long kind) ||
                    !Read(values, key + "Amount", 0, int.MaxValue, out long amount) ||
                    !Read(values, key + "From", 0, (int)MovementAccount.Casino, out long from) ||
                    !Read(values, key + "To", 0, (int)MovementAccount.Casino, out long to) ||
                    !Read(values, key + "Round", 0, int.MaxValue, out long round) || from == to ||
                    ((from == (int)MovementAccount.Chips || to == (int)MovementAccount.Chips) && amount % 25 != 0))
                    return ParseStatus.Corrupt;
                entries[i] = new Movement { Id = id, Kind = (MovementKind)kind, Amount = (int)amount,
                    From = (MovementAccount)from, To = (MovementAccount)to, RoundId = (int)round };
                previous = id;
            }
            if (result.LedgerSequence < previous) return ParseStatus.Corrupt;
            result.LedgerEntries = entries;
            data = result;
            return ParseStatus.Ok;
        }

        private static uint Checksum(string text)
        {
            uint hash = 2166136261;
            foreach (char c in text) { hash ^= c; hash *= 16777619; }
            return hash;
        }

        private static void Append(StringBuilder body, string key, long value)
            => body.Append(key).Append('=').Append(value.ToString(CultureInfo.InvariantCulture)).Append('\n');

        private static bool Read(Dictionary<string, long> values, string key, long min, long max, out long value)
            => values.TryGetValue(key, out value) && value >= min && value <= max;
    }
}
