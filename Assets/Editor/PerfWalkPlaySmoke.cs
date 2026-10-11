using System;
using System.IO;
using Tycoon3D.Core;
using Tycoon3D.Dev;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tycoon3D.Editor
{
    /// <summary>
    /// Prueba en Play Mode del recorrido de medición (se lanza con <c>-perf-walk -perf-laps 1 -perf-out &lt;archivo&gt;</c>): el jugador
    /// camina solo de la calle al fondo del casino y de vuelta, mira las máquinas, se guardan los tiempos de fotograma y se escribe
    /// el informe. En batch sin gráficos los fotogramas por segundo no significan nada; aquí solo se comprueba que el recorrido
    /// llega al casino, termina y deja un informe completo. Marcador: TYCOON_PERF_WALK_OK (no lanzar con -quit).
    /// </summary>
    public static class PerfWalkPlaySmoke
    {
        private const string ScenePath = "Assets/_Game/Scenes/BarrioResidencial.unity";
        private static bool prevOptionsEnabled;
        private static EnterPlayModeOptions prevOptions;
        private static float startedAt, maxZ = -999f;
        private static Transform player;

        public static void Run()
        {
            try
            {
                GameProfile.DirectoryOverride = Path.Combine(Path.GetTempPath(), "tycoon-smoke-perf-" + Guid.NewGuid().ToString("N"));
                EditorSceneManager.OpenScene(ScenePath);
                prevOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
                prevOptions = EditorSettings.enterPlayModeOptions;
                EditorSettings.enterPlayModeOptionsEnabled = true;
                EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
                EditorApplication.playModeStateChanged += OnPlayMode;
                EditorApplication.EnterPlaymode();
            }
            catch (Exception e) { Fail(e); }
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            startedAt = Time.realtimeSinceStartup;
            EditorApplication.update += Wait;
        }

        private static void Wait()
        {
            try
            {
                var walk = PerfWalk.Current;
                if (walk == null)
                {
                    if (Time.realtimeSinceStartup - startedAt > 5f) throw new InvalidOperationException("El recorrido no arrancó: ¿se pasó -perf-walk?");
                    return;
                }
                if (player == null)
                {
                    var controller = UnityEngine.Object.FindFirstObjectByType<Tycoon3D.Player.FirstPersonController>();
                    if (controller != null) player = controller.transform;
                }
                if (player != null) maxZ = Mathf.Max(maxZ, player.position.z);
                if (!walk.Finished)
                {
                    if (Time.realtimeSinceStartup - startedAt > 150f) throw new InvalidOperationException("El recorrido no terminó en 150 s.");
                    return;
                }
                EditorApplication.update -= Wait;
                Require(maxZ > 2.2f, "El recorrido no llegó al fondo del casino (z máxima " + maxZ + ").");
                Require(walk.Result.Frames > 100 && walk.Result.AverageFps > 0 && walk.Result.Seconds > 5, "Resultado inválido: " + walk.Result);
                Require(walk.RouteMeters > 40f, "La ruta es demasiado corta: " + walk.RouteMeters + " m.");
                Require(File.Exists(walk.OutputPath), "No se escribió el informe en " + walk.OutputPath);
                string text = File.ReadAllText(walk.OutputPath);
                foreach (var need in new[] { "medicion:", "escena: BarrioResidencial", "unity:", "cpu:", "gpu:", "resolucion:", "calidad:", "ciclo_dia_noche:", "resultado: frames=", "fps_medio=", "fps_peor1pct=", "nota:" })
                    Require(text.Contains(need), "Al informe le falta «" + need + "»:\n" + text);
                Debug.Log("PERF_WALK_SMOKE informe:\n" + text);
                Debug.Log("TYCOON_PERF_WALK_OK: el jugador recorre solo la calle y el casino y vuelve; se guardan los tiempos de fotograma y se escribe el informe con escena, versión y equipo.");
                Finish(0);
            }
            catch (Exception e) { Fail(e); }
        }

        private static void Require(bool ok, string message)
        {
            if (!ok) throw new InvalidOperationException(message);
        }

        private static void Fail(Exception e)
        {
            Debug.LogException(e);
            Finish(1);
        }

        private static void Finish(int code)
        {
            EditorApplication.update -= Wait;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            GameProfile.DirectoryOverride = null;
            EditorSettings.enterPlayModeOptions = prevOptions;
            EditorSettings.enterPlayModeOptionsEnabled = prevOptionsEnabled;
            if (code == 0) EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(code);
        }
    }
}
