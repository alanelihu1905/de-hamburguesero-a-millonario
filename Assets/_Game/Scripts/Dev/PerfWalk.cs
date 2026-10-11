using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tycoon3D.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tycoon3D.Dev
{
    /// <summary>
    /// Medición de rendimiento reproducible. Con <c>-perf-walk</c> el jugador recorre solo la ruta barrio–casino (la misma de las
    /// pruebas de suelo: calle, puerta, pasillo y máquinas), da una vuelta mirando las máquinas y regresa, dos veces; mientras
    /// tanto se guarda el tiempo de cada fotograma. Al terminar escribe una línea <c>PERF_WALK_RESULT</c> en el log y un archivo
    /// de texto con la escena, la versión, el equipo y las cifras (fotogramas por segundo medios, peor 1 %, fotograma más largo),
    /// y cierra la app. Son cifras de esa máquina y esa escena: no prometen nada más.
    /// Argumentos: <c>-perf-out &lt;archivo&gt;</c>, <c>-perf-laps &lt;n&gt;</c> (2), <c>-perf-label &lt;texto&gt;</c>,
    /// <c>-quality-level &lt;n&gt;</c> (0 = más bajo), <c>-no-daynight</c> y <c>-no-variety</c> para medir sin esas funciones.
    /// El V-Sync se apaga para medir sin tope de fotogramas.
    /// </summary>
    public sealed class PerfWalk : MonoBehaviour
    {
        public const float WarmupSeconds = 3f;
        public const float WalkSpeed = 4.5f;
        public const float LookAroundSeconds = 4f;

        public static bool Requested => LaunchFlags.Has("-perf-walk");
        public static PerfWalk Current { get; private set; }

        public PerfStats Result { get; private set; }
        public bool Finished { get; private set; }
        public string OutputPath { get; private set; }
        public string Report { get; private set; } = string.Empty;
        public float RouteMeters { get; private set; }

        private readonly List<float> frames = new List<float>(8192);
        private readonly List<Vector3> route = new List<Vector3>();
        private Transform player;
        private Transform eye;
        private CharacterController controller;
        private FirstPersonController look;
        private int laps = 2, lap, leg;
        private float elapsed, lookTime;
        private int phase;                       // 0 calentamiento, 1 caminando, 2 mirando las máquinas, 3 terminado
        private Vector3 start;
        private string label = string.Empty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!Requested || Current != null) return;
            var go = new GameObject("Medición de rendimiento");
            DontDestroyOnLoad(go);
            go.AddComponent<PerfWalk>();
        }

        private void Awake()
        {
            Current = this;
            laps = Mathf.Max(1, LaunchFlags.IntValue("-perf-laps", 2));
            label = LaunchFlags.Value("-perf-label") ?? string.Empty;
            int quality = LaunchFlags.IntValue("-quality-level", -1);
            if (quality >= 0 && quality < QualitySettings.names.Length) QualitySettings.SetQualityLevel(quality, true);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            string output = LaunchFlags.Value("-perf-out");
            OutputPath = string.IsNullOrEmpty(output) ? Path.Combine(Application.persistentDataPath, "perf_walk.txt") : output;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private bool Bind()
        {
            if (player != null) return true;
            look = FindFirstObjectByType<FirstPersonController>();
            if (look == null) return false;
            player = look.transform;
            controller = player.GetComponent<CharacterController>();
            var cam = Camera.main;
            eye = cam != null ? cam.transform : player;
            look.enabled = false;                                      // el recorrido no depende del ratón ni del teclado
            if (controller != null) controller.enabled = false;
            start = player.position;
            float y = start.y;
            // la ruta de las pruebas de suelo: calle, esquina, puerta del casino, pasillo hasta las máquinas y de vuelta
            route.Clear();
            route.Add(new Vector3(9.5f, y, -6f));
            route.Add(new Vector3(16f, y, -3.2f));
            route.Add(new Vector3(16f, y, 2.4f));            // fondo del casino: aquí se mira alrededor
            route.Add(new Vector3(16f, y, -3.2f));
            route.Add(new Vector3(9.5f, y, -6f));
            route.Add(start);
            for (int i = 0, n = route.Count; i < n; i++)
                RouteMeters += Vector3.Distance(i == 0 ? start : route[i - 1], route[i]);
            RouteMeters *= laps;
            return true;
        }

        private void Update()
        {
            if (Finished || !Bind()) return;
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;
            if (phase == 0)
            {
                if (elapsed >= WarmupSeconds) { phase = 1; elapsed = 0f; frames.Clear(); }
                return;
            }
            frames.Add(dt);
            if (phase == 2)
            {
                lookTime += dt;
                player.Rotate(0f, 360f / LookAroundSeconds * dt, 0f);
                if (lookTime >= LookAroundSeconds) { phase = 1; lookTime = 0f; leg++; }
                return;
            }
            Vector3 target = route[leg];
            Vector3 delta = target - player.position;
            delta.y = 0f;
            float step = WalkSpeed * dt;
            if (delta.magnitude <= step)
            {
                player.position = target;
                if (leg == 2 && lookTime == 0f) { phase = 2; return; }
                leg++;
                if (leg >= route.Count) { leg = 0; lap++; if (lap >= laps) Finish(); }
                return;
            }
            Vector3 direction = delta.normalized;
            player.position += direction * step;
            player.rotation = Quaternion.RotateTowards(player.rotation, Quaternion.LookRotation(direction), 240f * dt);
        }

        private void Finish()
        {
            Finished = true;
            phase = 3;
            Result = PerfStats.Compute(frames);
            Report = BuildReport();
            Debug.Log("PERF_WALK_RESULT " + Result + " escena=" + SceneManager.GetActiveScene().name + " unity=" + Application.unityVersion +
                      " gpu=\"" + SystemInfo.graphicsDeviceName + "\" resolucion=" + Screen.width + "x" + Screen.height);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(OutputPath)));
                File.WriteAllText(OutputPath, Report, new UTF8Encoding(false));
            }
            catch (Exception e) { Debug.LogWarning("PERF_WALK no pudo escribir " + OutputPath + ": " + e.Message); }
            if (!Application.isEditor) Application.Quit();
        }

        private string BuildReport()
        {
            var sb = new StringBuilder();
            void Line(string k, string v) => sb.Append(k).Append(": ").Append(v).Append('\n');
            Line("medicion", "recorrido barrio-casino-barrio, " + laps + " vueltas, " + RouteMeters.ToString("F0") + " m a " + WalkSpeed + " m/s" + (string.IsNullOrEmpty(label) ? "" : " (" + label + ")"));
            Line("escena", SceneManager.GetActiveScene().name);
            Line("unity", Application.unityVersion);
            Line("plataforma", Application.platform + " / " + SystemInfo.operatingSystem);
            Line("cpu", SystemInfo.processorType + " (" + SystemInfo.processorCount + " hilos)");
            Line("memoria_mb", SystemInfo.systemMemorySize.ToString());
            Line("gpu", SystemInfo.graphicsDeviceName + " (" + SystemInfo.graphicsDeviceType + ", " + SystemInfo.graphicsMemorySize + " MB)");
            Line("resolucion", Screen.width + "x" + Screen.height + (Screen.fullScreen ? " pantalla completa" : " ventana"));
            Line("calidad", QualitySettings.names[QualitySettings.GetQualityLevel()] + " (nivel " + QualitySettings.GetQualityLevel() + ")");
            Line("vsync", QualitySettings.vSyncCount == 0 ? "apagado" : "encendido");
            Line("ciclo_dia_noche", LaunchFlags.Has("-no-daynight") ? "apagado" : "encendido");
            Line("variedad_clientes", LaunchFlags.Has("-no-variety") ? "apagada" : "encendida");
            Line("resultado", Result.ToString());
            Line("nota", "Cifras de esta maquina y de esta escena. No son una promesa para otros equipos.");
            return sb.ToString();
        }
    }
}
