using Tycoon3D.Economy;

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

        // caja del casino: fondo, premios por cobrar y lo repuesto por la casa (CashboxInit = 0: perfil anterior, fondo inicial)
        public long CashboxInit;
        public long CashboxFunds;
        public long CashboxOwed;
        public long CashboxRefilled;

        // ajustes de accesibilidad (por defecto: texto al 100 %, teclas E, Q y F)
        public long SettingsScalePercent = 100;
        public long SettingsKeyInteract = 101;
        public long SettingsKeySecondary = 113;
        public long SettingsKeyLights = 102;

        // reloj del juego en minutos desde el día 1 a las 00:00 (-1 = sin guardar: el reloj arranca a las 08:00)
        public long ClockMinutes = -1;

        // libro común: últimos movimientos y saldos anteriores al historial recortado
        public long LedgerSequence;
        public long LedgerOpeningPesos;
        public long LedgerOpeningChips;
        public Movement[] LedgerEntries = new Movement[0];

        public bool HasProfile => Pesos >= 0;

        public SaveData Clone()
        {
            var copy = (SaveData)MemberwiseClone();
            copy.LedgerEntries = (Movement[])LedgerEntries.Clone();
            return copy;
        }
    }
}
