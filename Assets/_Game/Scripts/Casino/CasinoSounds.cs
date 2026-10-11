using UnityEngine;
using UnityEngine.Audio;

namespace Tycoon3D.Casino
{
    /// <summary>Sonidos del casino (originales, generados por ArtSource/CasaMarea/casino_assets/generar_assets.py).</summary>
    public sealed class CasinoSounds : MonoBehaviour
    {
        public AudioClip spinLoop, spinStart, reelStop, betChange, lose, winSmall, winBig, jackpot, coin;
        [Range(0f, 1f)] public float volume = .8f;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup effectsGroup;
        [SerializeField] private AudioMixerGroup ambienceGroup;

        public int PlayedCount { get; private set; }
        public string LastPlayed { get; private set; } = string.Empty;
        public AudioMixerGroup MusicGroup => musicGroup;
        public AudioMixerGroup EffectsGroup => effectsGroup;
        public AudioMixerGroup AmbienceGroup => ambienceGroup;

        public void ConfigureMixer(AudioMixerGroup music, AudioMixerGroup effects, AudioMixerGroup ambience)
        {
            musicGroup = music;
            effectsGroup = effects;
            ambienceGroup = ambience;
        }

        public bool Complete =>
            spinLoop != null && spinStart != null && reelStop != null && betChange != null && lose != null &&
            winSmall != null && winBig != null && jackpot != null && coin != null;

        public void PlayAt(AudioClip clip, Vector3 position, float gain = 1f)
        {
            if (clip == null) return;
            PlayedCount++;
            LastPlayed = clip.name;
            var emitter = new GameObject("Casino SFX " + clip.name);
            emitter.transform.position = position;
            var source = emitter.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = effectsGroup;
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume * gain);
            source.spatialBlend = 1f;
            source.minDistance = 2f;
            source.maxDistance = 20f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.Play();
            Destroy(emitter, clip.length + .1f);
        }

        public AudioClip ForRound(SlotRound round)
        {
            if (round.Tier == SlotTier.JackpotFox) return jackpot;
            if (round.Tier == SlotTier.TrioHamburguesa || round.Tier == SlotTier.TrioMango) return winBig;
            return round.Payout > 0 ? winSmall : lose;
        }
    }
}
