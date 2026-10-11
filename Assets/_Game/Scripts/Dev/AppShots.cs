using System;
using System.Collections;
using System.IO;
using Tycoon3D.Casino;
using UnityEngine;

namespace Tycoon3D.Dev
{
    /// <summary>
    /// Modo de capturas para revisión visual desde la app compilada. Solo se activa con el argumento
    /// <c>-casino-shots &lt;carpeta&gt;</c>: recorre puntos de vista fijos, juega una ronda de cada tipo y guarda PNG
    /// (incluido el HUD) antes de cerrar la aplicación. Sin ese argumento no hace nada.
    /// </summary>
    public sealed class AppShots : MonoBehaviour
    {
        private sealed class Scripted : IRandomSource
        {
            private readonly int roll;
            public Scripted(int roll) { this.roll = roll; }
            private int calls;
            // la tirada de premio (rango 100) es fija; los demás sorteos (símbolos) varían para no repetir valores constantes
            public int Next(int max) => max == 100 ? Math.Min(roll, 99) : (calls++ * 7 + 3) % max;
        }

        private bool live;
        private string folder;
        private Camera shotCamera;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-casino-live") >= 0)
            {
                Application.runInBackground = true;
                new GameObject("CasinoLive").AddComponent<AppShots>().live = true;
                return;
            }
            int i = Array.IndexOf(args, "-casino-shots");
            if (i < 0 || i + 1 >= args.Length) return;
            Application.runInBackground = true;      // sin foco la app se pausaba y no avanzaba ningún fotograma
            var go = new GameObject("AppShots");
            DontDestroyOnLoad(go);
            go.AddComponent<AppShots>().folder = args[i + 1];
        }

        private IEnumerator Start()
        {
            if (live)
            {
                // modo manual: deja al jugador frente a la primera máquina Clásica, mirando hacia ella
                for (int i = 0; i < 30; i++) yield return null;
                var machine = FindObjectsByType<SlotMachineStation>(FindObjectsSortMode.None);
                SlotMachineStation target = null;
                foreach (var m in machine) if (m.Kind == SlotMachineKind.Clasica && (target == null || m.transform.position.x < target.transform.position.x)) target = m;
                var pl = FindFirstObjectByType<CharacterController>();
                var fp = FindFirstObjectByType<Tycoon3D.Player.FirstPersonController>();
                // el bloqueo del cursor al arrancar mete un giro de ratón: se reorienta dos veces para quedar frente a la máquina
                bool atCashier = Array.IndexOf(Environment.GetCommandLineArgs(), "-casino-at-cashier") >= 0;
                for (int round = 0; round < 2; round++)
                {
                    if (atCashier && pl != null)
                    {
                        Teleport(pl, new Vector3(18.2f, 0.05f, 2.5f));
                        if (fp != null) fp.FaceDirection(90f);
                    }
                    else if (target != null && pl != null)
                    {
                        Teleport(pl, new Vector3(target.transform.position.x, 0.05f, target.transform.position.z - 2.05f));   // fuera del taburete: si no, el CharacterController se desplaza de lado
                        if (fp != null) fp.FaceDirection(0f);
                    }
                    for (int i = 0; i < 90; i++) yield return null;
                }
                var cam = Camera.main;
                if (cam != null && Physics.Raycast(cam.transform.position, cam.transform.forward, out var aim, 3f))
                    Debug.Log("CASINO_LIVE_AIM " + aim.collider.name + " interactuable=" + (aim.collider.GetComponentInParent<SlotMachineStation>() != null));
                string[] a = Environment.GetCommandLineArgs();
                int ri = Array.IndexOf(a, "-casino-roll");
                var casino = FindFirstObjectByType<CasinoService>();
                if (ri >= 0 && ri + 1 < a.Length && int.TryParse(a[ri + 1], out int forced) && casino != null)
                {
                    casino.UseRandom(new Scripted(forced));
                    Debug.Log("CASINO_LIVE_ROLL " + forced);
                }
                Debug.Log("CASINO_LIVE_READY");
                yield break;
            }
            Directory.CreateDirectory(folder);
            for (int i = 0; i < 20; i++) yield return null;
            shotCamera = new GameObject("Camara de capturas").AddComponent<Camera>();
            var main = Camera.main;
            shotCamera.depth = 100;
            shotCamera.nearClipPlane = .08f;
            shotCamera.farClipPlane = 400;
            if (main != null) { shotCamera.clearFlags = main.clearFlags; shotCamera.backgroundColor = main.backgroundColor; }

            var service = FindFirstObjectByType<CasinoService>();
            var machines = FindObjectsByType<SlotMachineStation>(FindObjectsSortMode.None);
            Array.Sort(machines, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            var player = FindFirstObjectByType<CharacterController>();

            yield return Shot("01_inicio", new Vector3(0, 1.65f, -8.5f), new Vector3(12, 2.5f, 0), 72);
            yield return Shot("02_hacia_el_casino", new Vector3(5, 1.65f, -6.5f), new Vector3(16, 2.8f, -2), 78);
            yield return Shot("03_fachada", new Vector3(16, 1.7f, -10), new Vector3(16, 2.6f, -2), 58);
            yield return Shot("04_entrada", new Vector3(16, 1.65f, -4), new Vector3(16, 1.7f, 4), 80);
            yield return Shot("05_sala", new Vector3(11.4f, 1.95f, -1.2f), new Vector3(17.5f, 1.3f, 4.5f), 78);
            yield return Shot("06_sala_otro_angulo", new Vector3(20.4f, 1.95f, -1.4f), new Vector3(13.5f, 1.2f, 4.5f), 78);

            if (service != null && machines.Length > 0)
            {
                // un recorrido por cada tipo de máquina: reposo, giro, premio
                SlotMachineStation[] picks = { First(machines, SlotMachineKind.Clasica), First(machines, SlotMachineKind.MangoRapido),
                                               First(machines, SlotMachineKind.GranJackpot) };
                int[] rolls = { 70, 40, 99 };
                string[] tags = { "clasica", "mango", "jackpot" };
                for (int k = 0; k < picks.Length; k++)
                {
                    var m = picks[k];
                    if (m == null) continue;
                    Vector3 mp = m.transform.position;
                    Teleport(player, new Vector3(mp.x, 0.05f, mp.z - 1.9f));
                    service.UseRandom(new Scripted(rolls[k]));
                    service.SetBet(k == 2 ? 1 : 0);
                    yield return Shot("07_" + tags[k] + "_reposo", mp + new Vector3(0, 1.65f, -2.2f), mp + new Vector3(0, 1.5f, 0), 52);
                    m.Interact();
                    yield return new WaitForSeconds(m.SpinSeconds * .45f);
                    int playing = 0;
                    foreach (var src in FindObjectsByType<AudioSource>(FindObjectsSortMode.None)) if (src.isPlaying) playing++;
                    Debug.Log("CASINO_AUDIO " + tags[k] + " fuentes_sonando=" + playing + " listener=" + (FindFirstObjectByType<AudioListener>() != null) + " pausado=" + AudioListener.pause + " volumen=" + AudioListener.volume);
                    yield return Shot("08_" + tags[k] + "_girando", mp + new Vector3(0, 1.65f, -2.2f), mp + new Vector3(0, 1.5f, 0), 52);
                    yield return new WaitForSeconds(m.SpinSeconds * .7f);
                    yield return Shot("09_" + tags[k] + "_premio", mp + new Vector3(0, 1.65f, -2.2f), mp + new Vector3(0, 1.5f, 0), 52);
                    yield return new WaitForSeconds(1.2f);
                }
                service.SetBet(2);
                yield return Shot("10_apuesta_500", machines[0].transform.position + new Vector3(0, 1.65f, -2.2f),
                    machines[0].transform.position + new Vector3(0, 1.5f, 0), 52);
            }
            yield return Shot("16_cajera", new Vector3(17.6f, 1.7f, 1.2f), new Vector3(20.8f, 1.3f, 2.5f), 55);
            Teleport(player, new Vector3(0, .05f, -5));
            yield return new WaitForSeconds(2.5f);
            yield return Shot("14_clientes_fila", new Vector3(9.4f, 1.7f, -.6f), new Vector3(6.1f, 1.2f, 1.0f), 50);
            var strollers = FindObjectsByType<Tycoon3D.Restaurant.AnimatedCustomer>(FindObjectsSortMode.None);
            foreach (var cu in strollers)
                if (cu.Mode == Tycoon3D.Restaurant.CustomerMode.Stroll)
                {
                    Debug.Log("CLIENTE_PASEANTE caminando=" + cu.IsWalking + " x=" + cu.transform.position.x.ToString("F2"));
                    Vector3 p = cu.transform.position;
                    yield return Shot("15_paseante", p + new Vector3(2.2f, 1.5f, -3.4f), p + new Vector3(0, 1.0f, 0), 50);
                    Debug.Log("CLIENTE_PASEANTE_2 caminando=" + cu.IsWalking + " x=" + cu.transform.position.x.ToString("F2"));
                }
            yield return Shot("11_barrio_calle", new Vector3(-1, 1.7f, -9.5f), new Vector3(-2, 1.8f, 0), 70);
            yield return Shot("12_casa_marea", new Vector3(0, 1.7f, -8f), new Vector3(0, 1.8f, 0), 62);
            yield return Shot("13_cocina", new Vector3(0, 1.65f, -1.3f), new Vector3(0, 1.1f, 2.4f), 70);
            Debug.Log("APPSHOTS_DONE " + folder);
            Application.Quit();
        }

        private static SlotMachineStation First(SlotMachineStation[] all, SlotMachineKind kind)
        {
            foreach (var m in all) if (m.Kind == kind) return m;
            return null;
        }

        private static void Teleport(CharacterController player, Vector3 position)
        {
            if (player == null) return;
            player.enabled = false;
            player.transform.position = position + Vector3.up * 1f;
            player.enabled = true;
        }

        private IEnumerator Shot(string name, Vector3 position, Vector3 look, float fov)
        {
            shotCamera.transform.position = position;
            shotCamera.transform.LookAt(look);
            shotCamera.fieldOfView = fov;
            for (int i = 0; i < 6; i++) yield return null;
            string file = Path.Combine(folder, name + ".png");
            ScreenCapture.CaptureScreenshot(file);          // incluye el HUD (IMGUI)
            for (int i = 0; i < 8 && !File.Exists(file); i++) yield return null;
            yield return null;
            yield return null;
            Debug.Log("APPSHOT " + name + " " + Screen.width + "x" + Screen.height + " hudRepaints=" + Tycoon3D.Casino.CasinoHud.RepaintCount + " panel=" + Tycoon3D.Casino.CasinoHud.LastPanelDrawn);
        }
    }
}
