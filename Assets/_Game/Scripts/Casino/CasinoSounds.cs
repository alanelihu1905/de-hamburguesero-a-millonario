using UnityEngine;

namespace Tycoon3D.Casino
{
    /// <summary>Sonidos del casino (originales, generados por ArtSource/CasaMarea/casino_assets/generar_assets.py).</summary>
    public sealed class CasinoSounds : MonoBehaviour
    {
        public AudioClip spinLoop, spinStart, reelStop, betChange, lose, winSmall, winBig, jackpot, coin;
        [Range(0f, 1f)] public float volume = .8f;

        public int PlayedCount { get; private set; }
        public string LastPlayed { get; private set; } = string.Empty;

        public bool Complete =>
            spinLoop != null && spinStart != null && reelStop != null && betChange != null && lose != null &&
            winSmall != null && winBig != null && jackpot != null && coin != null;

        public void PlayAt(AudioClip clip, Vector3 position, float gain = 1f)
        {
            if (clip == null) return;
            PlayedCount++;
            LastPlayed = clip.name;
            AudioSource.PlayClipAtPoint(clip, position, Mathf.Clamp01(volume * gain));
        }

        public AudioClip ForRound(SlotRound round)
        {
            if (round.Tier == SlotTier.JackpotFox) return jackpot;
            if (round.Tier == SlotTier.TrioHamburguesa || round.Tier == SlotTier.TrioMango) return winBig;
            return round.Payout > 0 ? winSmall : lose;
        }
    }
}
