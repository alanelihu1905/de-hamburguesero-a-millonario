using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tycoon3D.Editor
{
    /// <summary>
    /// Smoke de Play Mode de la sala cerrada del Casino FOX: un CharacterController de 1.80 m × 0.60 m recorre la sala con la
    /// física real. Comprueba que entra por la puerta de ENTRADA, llega al jackpot por el pasillo, sale por la de SALIDA y que
    /// los muros, los taburetes y el mostrador lo detienen. También cuenta los errores de consola.
    /// ARTE, NO MECÁNICA: no prueba apuestas ni pagos.
    /// Uso en batch (sin -quit): -executeMethod Tycoon3D.Editor.PuertoAlbaCasinoSalaPlaySmoke.Run
    /// Informe: ArtSource/PuertoAlba/Casino/unity_sala_play_check.txt
    /// </summary>
    public static class PuertoAlbaCasinoSalaPlaySmoke
    {
        private const string ReportPath = "ArtSource/PuertoAlba/Casino/unity_sala_play_check.txt";
        private static bool prevOptionsEnabled;
        private static EnterPlayModeOptions prevOptions;
        private static int errores, frames, fallos;
        private static readonly StringBuilder informe = new StringBuilder();
        private static bool ejecutado;

        public static void Run()
        {
            try
            {
                EditorSceneManager.OpenScene(PuertoAlbaCasinoSalaBuilder.ScenePath);
                prevOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
                prevOptions = EditorSettings.enterPlayModeOptions;
                EditorSettings.enterPlayModeOptionsEnabled = true;
                EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
                EditorApplication.playModeStateChanged += OnPlayMode;
                EditorApplication.EnterPlaymode();
            }
            catch (Exception e) { Debug.LogException(e); Finish(1); }
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            Application.logMessageReceived += Manejador;
            frames = 0;
            EditorApplication.update += Step;
        }

        private static void Manejador(string cond, string stack, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception) { errores++; informe.AppendLine("  consola: " + tipo + " " + cond); }
        }

        private static void Check(bool ok, string texto)
        {
            informe.AppendLine((ok ? "OK    " : "FALLO ") + texto);
            if (!ok) fallos++;
        }

        private static void Step()
        {
            if (ejecutado) return;
            if (++frames < 3) return;          // deja que la física registre los colisionadores de la escena
            ejecutado = true;
            try { Prueba(); }
            catch (Exception e) { informe.AppendLine("ERROR " + e); fallos++; }
            Finish(fallos == 0 && errores == 0 ? 0 : 1);
        }

        private static void Prueba()
        {
            var referencia = GameObject.Find(PuertoAlbaCasinoSalaBuilder.RefName);
            if (referencia != null) referencia.SetActive(false);   // la cápsula de referencia no es parte del recorrido

            var go = new GameObject("Jugador de prueba");
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = .3f; cc.center = new Vector3(0, .9f, 0);
            cc.stepOffset = .3f; cc.slopeLimit = 45f;
            Teletransportar(cc, new Vector3(-4f, .1f, -14f));
            for (int i = 0; i < 20; i++) cc.Move(Vector3.down * .05f);
            Check(cc.isGrounded, "el jugador de prueba (1.80 m × 0.60 m) se apoya en el suelo");

            // 1. entrada: cruza el hueco de la puerta de ENTRADA (x = −4) y llega a la sala
            Vector3 p = IrA(cc, new Vector3(-4f, .1f, -5f));
            Check(Vector3.Distance(p, new Vector3(-4f, .1f, -5f)) < .3f, "entra por la puerta de ENTRADA y llega a la sala (z = " + p.z.ToString("F2") + ")");

            // 2. pasillo hasta el jackpot
            IrA(cc, new Vector3(0f, .1f, -5f));
            p = IrA(cc, new Vector3(0f, .1f, 7.5f));
            Check(p.z > 7.2f, "recorre el pasillo y llega al jackpot (z = " + p.z.ToString("F2") + ", el letrero está en z = 9)");

            // 3. un muro lo detiene
            Teletransportar(cc, new Vector3(0f, .1f, -5f));
            p = IrA(cc, new Vector3(0f, .1f, -14f));
            Check(p.z > -9.9f, "el muro frontal lo detiene (z final = " + p.z.ToString("F2") + ", el muro está en z = −10)");

            // 4. el jugador sale por la puerta de SALIDA (x = 4)
            Teletransportar(cc, new Vector3(4f, .1f, -5f));
            p = IrA(cc, new Vector3(4f, .1f, -14f));
            Check(p.z < -13.7f, "sale por la puerta de SALIDA (z final = " + p.z.ToString("F2") + ")");

            // 5. tragamonedas y taburete lo detienen
            Teletransportar(cc, new Vector3(-4f, .1f, -3.3f));
            p = IrA(cc, new Vector3(-8f, .1f, -3.3f));
            Check(p.x > -5.6f, "el taburete y la tragamonedas lo detienen (x final = " + p.x.ToString("F2") + ")");

            // 6. el mostrador de cambio lo detiene
            Teletransportar(cc, new Vector3(-6.5f, .1f, -7.6f));
            p = IrA(cc, new Vector3(-10f, .1f, -7.6f));
            Check(p.x > -8.6f, "el mostrador de cambio lo detiene (x final = " + p.x.ToString("F2") + ")");

            // 7. la zona de las dos puertas es transitable en los dos sentidos
            Teletransportar(cc, new Vector3(4f, .1f, -14f));
            p = IrA(cc, new Vector3(4f, .1f, -5f));
            Check(p.z > -5.3f, "se puede entrar también por la puerta de SALIDA (z final = " + p.z.ToString("F2") + ")");

            // 8. consola
            informe.AppendLine("  (errores de consola en Play Mode: " + errores + ")");
            Check(errores == 0, "consola sin errores durante Play Mode (" + errores + ")");
        }

        private static void Teletransportar(CharacterController cc, Vector3 pos)
        {
            cc.enabled = false;
            cc.transform.position = pos;
            cc.enabled = true;
            Physics.SyncTransforms();
        }

        /// <summary>Avanza hacia el destino en pasos de 10 cm con la física real; se detiene si queda bloqueado.</summary>
        private static Vector3 IrA(CharacterController cc, Vector3 destino)
        {
            int sinAvance = 0;
            for (int i = 0; i < 600; i++)
            {
                Vector3 antes = cc.transform.position;
                Vector3 plano = new Vector3(destino.x - antes.x, 0, destino.z - antes.z);
                if (plano.magnitude < .05f) break;
                Vector3 paso = Vector3.ClampMagnitude(plano, .1f);
                cc.Move(paso + Vector3.down * .02f);
                float moved = new Vector2(cc.transform.position.x - antes.x, cc.transform.position.z - antes.z).magnitude;
                sinAvance = moved < .01f ? sinAvance + 1 : 0;
                if (sinAvance >= 4) break;
            }
            return cc.transform.position;
        }

        private static void Finish(int code)
        {
            Application.logMessageReceived -= Manejador;
            EditorApplication.update -= Step;
            informe.AppendLine(code == 0 ? "TYCOON_CASINO_SALA_PLAY_OK" : "TYCOON_CASINO_SALA_PLAY_FALLO (" + fallos + " fallos, " + errores + " errores de consola)");
            File.WriteAllText(ReportPath, informe.ToString(), new UTF8Encoding(false));
            Debug.Log(informe.ToString());
            if (code == 0) Debug.Log("TYCOON_CASINO_SALA_PLAY_OK: la sala se recorre con física real; las puertas pasan y los muros, taburetes y mostrador bloquean; consola limpia.");
            else Debug.LogError("TYCOON_CASINO_SALA_PLAY_FALLO: ver " + ReportPath);
            EditorSettings.enterPlayModeOptions = prevOptions;
            EditorSettings.enterPlayModeOptionsEnabled = prevOptionsEnabled;
            if (code == 0) EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(code);
        }
    }
}
