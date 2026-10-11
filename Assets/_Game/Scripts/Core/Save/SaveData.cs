namespace Tycoon3D.Save
{
    /// <summary>
    /// Perfil del jugador que sobrevive al cierre de la app y a la recarga de escena (R). Solo números enteros, sin Unity,
    /// para poder validarlo con dotnet y, a futuro, en el anfitrión. Pesos = -1 significa «sin perfil todavía».
    /// </summary>
    public sealed class SaveData
    {
        public long SaveCount;
        public long Pesos = -1;

        // hamburguesas
        public long BurgerStep;
        public long BurgerSales;

        // casino (libro y fichas)
        public long BetIndex;
        public long Chips;
        public long ChipsBought;
        public long ChipsCashed;
        public long CashierVisits;
        public long CasinoRounds;
        public long CasinoSpins;
        public long CasinoWagered;
        public long CasinoWon;
        public long CasinoFreeSpins;
        public long CasinoBest;

        // ronda aceptada y aún sin liquidar (si la app se cierra durante el giro, se liquida una sola vez al volver)
        public long PendingActive;
        public long PendingStake;
        public long PendingPayout;
        public long PendingFree;
        public long PendingChips;
        public long PendingRoundId;

        public bool HasProfile => Pesos >= 0;

        public SaveData Clone() => (SaveData)MemberwiseClone();
    }
}
