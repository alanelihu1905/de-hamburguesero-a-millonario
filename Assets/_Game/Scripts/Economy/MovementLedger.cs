using System;
using System.Collections.Generic;
using Tycoon3D.Save;

namespace Tycoon3D.Economy
{
    public enum MovementKind { Opening, Ingredients, Sale, BuyChips, CashOut, Stake, Payout, Courtesy, Adjustment }
    public enum MovementAccount { Outside, Wallet, Chips, Casino }

    /// <summary>Movimiento en pesos; Chips representa fichas a su valor de cambio de $25.</summary>
    public struct Movement
    {
        public long Id;
        public MovementKind Kind;
        public int Amount;
        public MovementAccount From;
        public MovementAccount To;
        public int RoundId;
    }

    /// <summary>Últimos 32 movimientos, con saldos iniciales que conservan la auditoría al recortar el historial.</summary>
    public sealed class MovementLedger
    {
        public const int Capacity = 32;
        private readonly List<Movement> entries = new List<Movement>(Capacity);
        private long sequence;
        private long openingPesos;
        private long openingChips;

        public IReadOnlyList<Movement> Entries => entries;
        public long Sequence => sequence;

        public void Restore(SaveData data, int pesos, int chips)
        {
            entries.Clear();
            sequence = data.LedgerSequence;
            openingPesos = data.LedgerOpeningPesos;
            openingChips = data.LedgerOpeningChips;
            if (data.LedgerEntries != null) entries.AddRange(data.LedgerEntries);
            if (entries.Count != 0 || openingPesos != 0 || openingChips != 0) return;
            if (pesos > 0) Record(MovementKind.Opening, pesos, MovementAccount.Outside, MovementAccount.Wallet);
            if (chips > 0) Record(MovementKind.Opening, checked(chips * 25), MovementAccount.Outside, MovementAccount.Chips);
        }

        public void Record(MovementKind kind, int amount, MovementAccount from, MovementAccount to, int roundId = 0)
        {
            if (amount < 0 || from == to || (from == MovementAccount.Chips || to == MovementAccount.Chips) && amount % 25 != 0)
                throw new ArgumentException("Movimiento inválido.");
            var entry = new Movement { Id = ++sequence, Kind = kind, Amount = amount, From = from, To = to, RoundId = roundId };
            if (entries.Count == Capacity)
            {
                Apply(entries[0], ref openingPesos, ref openingChips);
                entries.RemoveAt(0);
            }
            entries.Add(entry);
        }

        public bool Matches(int pesos, int chips)
        {
            long p = openingPesos, c = openingChips;
            foreach (var entry in entries) Apply(entry, ref p, ref c);
            return p == pesos && c == chips;
        }

        public void Write(SaveData data)
        {
            data.LedgerSequence = sequence;
            data.LedgerOpeningPesos = openingPesos;
            data.LedgerOpeningChips = openingChips;
            data.LedgerEntries = entries.ToArray();
        }

        private static void Apply(Movement entry, ref long pesos, ref long chips)
        {
            if (entry.From == MovementAccount.Wallet) pesos -= entry.Amount;
            if (entry.To == MovementAccount.Wallet) pesos += entry.Amount;
            if (entry.From == MovementAccount.Chips) chips -= entry.Amount / 25;
            if (entry.To == MovementAccount.Chips) chips += entry.Amount / 25;
        }
    }
}
