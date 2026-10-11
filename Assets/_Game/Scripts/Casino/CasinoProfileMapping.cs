using Tycoon3D.Save;

namespace Tycoon3D.Casino
{
    /// <summary>Convierte entre el estado del casino y el perfil guardado (sin Unity).</summary>
    public static class CasinoProfileMapping
    {
        public static void Write(SaveData data, CasinoState s)
        {
            data.BetIndex = s.BetIndex; data.Chips = s.Chips; data.ChipsBought = s.ChipsBought; data.ChipsCashed = s.ChipsCashed;
            data.CashierVisits = s.CashierVisits; data.CasinoRounds = s.Rounds; data.CasinoSpins = s.Spins; data.CasinoWagered = s.Wagered;
            data.CasinoWon = s.Won; data.CasinoFreeSpins = s.FreeSpins; data.CasinoBest = s.Best;
            data.PendingActive = s.PendingActive ? 1 : 0; data.PendingStake = s.PendingStake; data.PendingPayout = s.PendingPayout;
            data.PendingFree = s.PendingFree ? 1 : 0; data.PendingChips = s.PendingChips ? 1 : 0; data.PendingRoundId = s.PendingRoundId;
        }

        public static CasinoState Read(SaveData d) => new CasinoState
        {
            BetIndex = (int)d.BetIndex, Chips = (int)d.Chips, ChipsBought = (int)d.ChipsBought, ChipsCashed = (int)d.ChipsCashed,
            CashierVisits = (int)d.CashierVisits, Rounds = (int)d.CasinoRounds, Spins = (int)d.CasinoSpins, Wagered = (int)d.CasinoWagered,
            Won = (int)d.CasinoWon, FreeSpins = (int)d.CasinoFreeSpins, Best = (int)d.CasinoBest,
            PendingActive = d.PendingActive == 1, PendingStake = (int)d.PendingStake, PendingPayout = (int)d.PendingPayout,
            PendingFree = d.PendingFree == 1, PendingChips = d.PendingChips == 1, PendingRoundId = (int)d.PendingRoundId
        };
    }
}
