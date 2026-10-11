namespace Tycoon3D.Casino
{
    /// <summary>Estado persistible del casino (sin Unity): libro, fichas y la ronda aceptada aún sin liquidar.</summary>
    public struct CasinoState
    {
        public int BetIndex;
        public int Chips;
        public int ChipsBought;
        public int ChipsCashed;
        public int CashierVisits;
        public int Rounds;
        public int Spins;
        public int Wagered;
        public int Won;
        public int FreeSpins;
        public int Best;

        public bool PendingActive;
        public int PendingStake;
        public int PendingPayout;
        public bool PendingFree;
        public bool PendingChips;
        public int PendingRoundId;
    }
}
