using System.Collections;
using UnityEngine;

namespace Tycoon3D.Casino
{
    /// <summary>
    /// Celebración del jackpot FOX: lluvia de monedas que sale de las seis máquinas, todas las bombillas del casino festejando,
    /// el sonido de monedas además de la fanfarria y un marco dorado en el panel. Dura cinco segundos y respeta el límite de
    /// destellos (<see cref="FlashPolicy"/>): con «luces fijas» activo no parpadea nada. Se crea sola desde el servicio del
    /// casino, sin tocar la escena, y no cambia ninguna regla de pago.
    /// </summary>
    public sealed class JackpotCelebration : MonoBehaviour
    {
        public const float Duration = 5f;

        private CasinoService service;
        private Coroutine running;

        public bool Active => Time.time < activeUntil;
        public int Celebrations { get; private set; }
        public int CoinBursts { get; private set; }
        private float activeUntil = -100f;

        public void Bind(CasinoService casino)
        {
            service = casino;
            service.Revealed += OnRevealed;
        }

        private void OnDestroy()
        {
            if (service != null) service.Revealed -= OnRevealed;
        }

        private void OnRevealed(SlotRound round)
        {
            if (round.Tier != SlotTier.JackpotFox) return;
            Celebrations++;
            activeUntil = Time.time + Duration;
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Celebrate());
        }

        private IEnumerator Celebrate()
        {
            var machines = service.Machines;
            var sounds = service.Sounds;
            var cam = Camera.main;
            if (sounds != null && sounds.jackpotCoins != null)
                sounds.PlayAt(sounds.jackpotCoins, cam != null ? cam.transform.position : transform.position, 1f);
            foreach (var machine in machines) if (machine != null) machine.Celebrate(Duration);
            // seis oleadas de monedas, una por máquina, escalonadas para que parezca una lluvia y no un solo estallido
            for (int wave = 0; wave < 6; wave++)
            {
                foreach (var machine in machines)
                    if (machine != null) { machine.CoinBurst(18 + wave * 4); CoinBursts++; }
                yield return new WaitForSeconds(.7f);
            }
            running = null;
        }
    }
}
