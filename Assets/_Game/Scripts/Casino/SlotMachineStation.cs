using Tycoon3D.Interaction;
using UnityEngine;

namespace Tycoon3D.Casino
{
    public enum SlotMachineKind { Clasica, MangoRapido, GranJackpot }

    /// <summary>
    /// Una tragamonedas del Casino FOX. Tres tipos distintos (tabla de premios, ritmo y aspecto). E gira, Q cambia la apuesta.
    /// Los rodillos giran de verdad y frenan uno tras otro; las luces, el sonido y las monedas siguen el resultado.
    /// </summary>
    public sealed class SlotMachineStation : MonoBehaviour, IInteractable, ISecondaryInteractable
    {
        [SerializeField] private CasinoService service;
        [SerializeField] private SlotMachineKind kind;
        [SerializeField] private ReelDrum[] drums = new ReelDrum[3];
        [SerializeField] private Renderer[] bulbs = new Renderer[0];
        [SerializeField] private Renderer marquee;
        [SerializeField] private Light winLight;
        [SerializeField] private ParticleSystem coins;
        [SerializeField] private AudioSource loopSource;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock block;
        private float flashUntil;
        private bool spinningNow;
        private int lastChaseStep = -1;
        private float baseLightIntensity = 1.2f;

        public SlotMachineKind Kind => kind;
        public SlotPaytable Paytable => SlotPaytables.All[(int)kind];
        public ReelDrum[] Drums => drums;
        public bool Flashing => Time.time < flashUntil;

        public string DisplayName => Paytable.Name;

        /// <summary>Duración total del giro: la máquina Mango Rápido es ágil; el Gran Jackpot hace esperar un poco más.</summary>
        public float SpinSeconds
        {
            get
            {
                switch (kind)
                {
                    case SlotMachineKind.MangoRapido: return 1.1f;
                    case SlotMachineKind.GranJackpot: return 2.3f;
                    default: return 1.6f;
                }
            }
        }

        public void Configure(CasinoService casino, SlotMachineKind machineKind, ReelDrum[] reelDrums, Renderer[] bulbRenderers,
            Renderer marqueeRenderer, Light light, ParticleSystem coinBurst, AudioSource spinSource)
        {
            service = casino;
            kind = machineKind;
            drums = reelDrums;
            bulbs = bulbRenderers;
            marquee = marqueeRenderer;
            winLight = light;
            coins = coinBurst;
            loopSource = spinSource;
        }

        public string Prompt
        {
            get
            {
                if (service == null) return string.Empty;
                if (service.Spinning) return "Girando…";
                var session = service.Session;
                return session.NextSpinIsFree
                    ? "E - Giro de cortesía (gratis) · " + DisplayName
                    : "E - Girar ($" + session.CurrentBet + ")" + (session.NextSpinUsesChips ? (session.CurrentBet == CasinoSession.ChipValue ? " con 1 ficha" : " con " + session.CurrentBet / CasinoSession.ChipValue + " fichas") : "")
                      + " · " + DisplayName + " · 1-4 cambian la apuesta";
            }
        }

        public void Interact()
        {
            if (service != null) service.TrySpin(this);
        }

        public void InteractSecondary()
        {
            if (service != null) service.CycleBet();
        }

        private void Start()
        {
            // reposo con símbolos distintos para que se vea el rodillo desde el inicio
            for (int i = 0; i < drums.Length; i++)
                if (drums[i] != null) drums[i].SnapTo((SlotSymbol)(((int)kind * 2 + i * 2) % ReelDrum.Faces));
            if (winLight != null) baseLightIntensity = winLight.intensity;
        }

        /// <summary>El servicio ya cobró la apuesta y conoce el resultado; aquí solo se anima.</summary>
        public void OnSpinStarted(SlotRound round, float seconds)
        {
            spinningNow = true;
            float[] stops = { .55f, .78f, 1f };
            for (int i = 0; i < drums.Length && i < round.Reels.Length; i++)
                if (drums[i] != null) drums[i].StartSpin(round.Reels[i], seconds * stops[i], 2 + i);
            if (service != null && service.Sounds != null)
            {
                service.Sounds.PlayAt(service.Sounds.spinStart, transform.position);
                if (loopSource != null && service.Sounds.spinLoop != null)
                {
                    loopSource.clip = service.Sounds.spinLoop;
                    loopSource.loop = true;
                    loopSource.volume = .5f * service.Sounds.volume;
                    loopSource.Play();
                }
                for (int i = 0; i < 3; i++)
                    StartCoroutine(DelayedClip(service.Sounds.reelStop, seconds * stops[i]));
                ReportAudioStatus();
                StartCoroutine(LogAudioStatus());
            }
        }

        private System.Collections.IEnumerator LogAudioStatus()
        {
            yield return new WaitForSeconds(.2f);
            ReportAudioStatus();
        }

        private void ReportAudioStatus()
        {
            var sounds = service != null ? service.Sounds : null;
            if (sounds == null) return;
            int music = 0, effects = 0, ambience = 0;
            foreach (var source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (!source.isPlaying) continue;
                if (source.outputAudioMixerGroup == sounds.MusicGroup) music++;
                if (source.outputAudioMixerGroup == sounds.EffectsGroup) effects++;
                if (source.outputAudioMixerGroup == sounds.AmbienceGroup) ambience++;
            }
            Debug.Log($"CASINO_AUDIO giro={spinningNow} musica={music} efectos={effects} ambiente={ambience} bucle={loopSource != null && loopSource.isPlaying}");
        }

        private System.Collections.IEnumerator DelayedClip(AudioClip clip, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (service != null && service.Sounds != null) service.Sounds.PlayAt(clip, transform.position, .7f);
        }

        public void OnRevealed(SlotRound round)
        {
            spinningNow = false;
            if (loopSource != null) loopSource.Stop();
            for (int i = 0; i < drums.Length && i < round.Reels.Length; i++)
                if (drums[i] != null) drums[i].SnapTo(round.Reels[i]);          // al revelar, el resultado queda fijo siempre
            bool win = round.Payout > round.Stake || (round.FreeSpin && round.Payout > 0);
            if (service != null && service.Sounds != null)
            {
                service.Sounds.PlayAt(service.Sounds.ForRound(round), transform.position);
                if (win) service.Sounds.PlayAt(service.Sounds.coin, transform.position, .8f);
            }
            if (win)
            {
                flashUntil = Time.time + (round.Tier == SlotTier.JackpotFox ? 4f : 2.2f);
                if (coins != null)
                {
                    int count = round.Tier == SlotTier.JackpotFox ? 70 : round.Tier == SlotTier.Par ? 10 : 28;
                    coins.Emit(count);
                }
            }
        }

        private void Update()
        {
            if (service == null) return;
            bool reduce = service.ReduceFlashing;
            PaintBulbs(reduce);
            if (winLight != null)
            {
                if (Flashing && !reduce) winLight.intensity = baseLightIntensity * (1.6f + .9f * Mathf.Sin(Time.time * 2.5f * 2f * Mathf.PI));
                else winLight.intensity = Mathf.Lerp(winLight.intensity, baseLightIntensity * (Flashing ? 2f : 1f), .2f);
            }
        }

        private void PaintBulbs(bool reduce)
        {
            if (bulbs == null || bulbs.Length == 0) return;
            block ??= new MaterialPropertyBlock();
            // persecución de luces: nunca más de ~3 cambios por segundo en una misma bombilla
            float stepsPerSecond = spinningNow ? 8f : 1.5f;
            int step = Mathf.FloorToInt(Time.time * stepsPerSecond);
            if (step == lastChaseStep && !Flashing) return;
            lastChaseStep = step;
            for (int i = 0; i < bulbs.Length; i++)
            {
                if (bulbs[i] == null) continue;
                bool lit;
                if (reduce) lit = true;                                                // modo sin parpadeos: encendidas fijas
                else if (Flashing) lit = ((int)(Time.time * 5f) % 2 == 0);              // 2.5 Hz
                else lit = ((i + step) % 3) != 0;
                Color c = lit ? new Color32(244, 201, 93, 255) : new Color32(120, 84, 52, 255);
                bulbs[i].GetPropertyBlock(block);
                block.SetColor(BaseColor, c);
                block.SetColor(EmissionColor, lit ? c * 2.4f : Color.black);
                bulbs[i].SetPropertyBlock(block);
            }
        }
    }
}
