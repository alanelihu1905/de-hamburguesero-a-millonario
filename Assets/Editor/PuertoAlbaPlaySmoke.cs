using System;
using Tycoon3D.City;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tycoon3D.Editor
{
    /// <summary>
    /// Smoke de Play Mode de Puerto Alba (fase 1): el jugador se apoya sobre el suelo y el seguidor de zona cambia al caminar
    /// de Casa Marea a la calle y de la calle al mercado. Termina con TYCOON_PUERTO_ALBA_SMOKE_OK o con error.
    /// </summary>
    public static class PuertoAlbaPlaySmoke
    {
        private static bool prevOptionsEnabled;
        private static EnterPlayModeOptions prevOptions;
        private static GameObject player;
        private static CityZoneTracker tracker;
        private static CityHud hud;
        private static readonly MemoriaSmoke memoria = new MemoriaSmoke();
        private static int step, frames;
        private static float stepStart, realStart;

        private static readonly (Vector3 at, ZoneId? expect, string label)[] Path =
        {
            (new Vector3(0, 3, 0), ZoneId.CasaMarea, "Casa Marea"),   // se suelta desde 3 m: debe caer y apoyarse
            (new Vector3(16, 1, 0), null, "calle"),
            (new Vector3(30, 1, 0), ZoneId.MercadoDelSol, "Mercado del Sol"),
            (new Vector3(0, 1, 0), ZoneId.CasaMarea, "vuelta a Casa Marea"),
        };

        public static void Run()
        {
            try
            {
                EditorSceneManager.OpenScene(PuertoAlbaSceneBuilder.ScenePath);
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
            try
            {
                player = GameObject.Find("Jugador");
                tracker = player != null ? player.GetComponent<CityZoneTracker>() : null;
                Require(player != null && tracker != null, "Falta el jugador o el seguidor de zona.");
                hud = GameObject.Find("Puerto Alba HUD") != null ? GameObject.Find("Puerto Alba HUD").GetComponent<CityHud>() : null;
                Require(hud != null, "Falta el HUD de Puerto Alba.");
                hud.UseStore(memoria);   // los ajustes del smoke no escriben en PlayerPrefs del usuario
                step = 0; frames = 0;
                EditorApplication.update += Step;
            }
            catch (Exception e) { Fail(e); }
        }

        private static void Step()
        {
            try
            {
                frames++;
                if (step == 0 && frames == 1) { Place(0); stepStart = Time.time; realStart = Time.realtimeSinceStartup; }
                // Espera por tiempo de juego (no por fotogramas): en batch los fotogramas son casi instantáneos.
                if (Time.time - stepStart < .5f) { if (Time.realtimeSinceStartup - realStart < 90f) return; throw new InvalidOperationException("El reloj del juego no avanza: t=" + Time.time); }

                if (step == 0)
                {
                    // Asentamiento: en batch la gravedad del controlador se integra con pasos mínimos y no es fiable
                    // para medir la caída. Aquí se empuja hacia abajo con CharacterController.Move hasta que se apoye,
                    // para comprobar que el jugador queda sobre el suelo sin atravesarlo ni flotar.
                    var cc = player.GetComponent<CharacterController>();
                    for (int i = 0; i < 4000 && !(cc.collisionFlags.HasFlag(CollisionFlags.Below)); i++)
                        cc.Move(Vector3.down * .05f);
                    float y = player.transform.position.y;
                    Require(cc.collisionFlags.HasFlag(CollisionFlags.Below), "El jugador no encontró suelo bajo sus pies.");
                    Require(y > .95f && y < 1.05f, "El jugador quedó en y=" + y.ToString("F3") + " (esperado ~1.00 sobre el suelo de 0,02 m).");
                    Require(Physics.Raycast(player.transform.position + Vector3.up * .2f, Vector3.down, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Ignore),
                        "No hay suelo bajo el jugador.");
                    float gap = cc.bounds.min.y - hit.point.y;
                    Require(gap <= .12f, "El jugador flota: hueco de " + gap.ToString("F3") + " m sobre " + hit.collider.name);
                    Debug.Log("PUERTO_ALBA_APOYO y=" + y.ToString("F3") + " hueco=" + gap.ToString("F3") + " sobre " + hit.collider.name);
                }
                var expect = Path[step];
                Require(tracker.Known && tracker.CurrentZone == expect.expect,
                    "Tras caminar a " + expect.label + " la zona debía ser " + (expect.expect.HasValue ? expect.expect.Value.ToString() : "calle") +
                    " y es " + (tracker.CurrentZone.HasValue ? tracker.CurrentZone.Value.ToString() : "calle"));
                string textoEsperado = expect.expect.HasValue ? CityMap.Get(expect.expect.Value).Name : "Calle";
                Require(hud.ZoneText == textoEsperado, "El banner dice «" + hud.ZoneText + "» y debía decir «" + textoEsperado + "».");
                Debug.Log("PUERTO_ALBA_PASO " + expect.label + " OK");
                step++;
                if (step >= Path.Length)
                {
                    // ajustes: se limitan al rango y se aplican al jugador y a la cámara
                    hud.ApplyAjustes(99f, 500f, 9f);
                    var fpc = player.GetComponent<Tycoon3D.Player.FirstPersonController>();
                    Require(hud.Ajustes.Sensibilidad == CityAjustes.SensibilidadMax && fpc.MouseSensitivity == CityAjustes.SensibilidadMax,
                        "La sensibilidad no se limitó o no llegó al jugador.");
                    Require(Mathf.Approximately(Camera.main.fieldOfView, CityAjustes.FovMax), "El FOV no llegó a la cámara.");
                    Require(hud.Ajustes.EscalaTexto == CityAjustes.TextoMax, "La escala de texto no se limitó.");
                    hud.ApplyAjustes(CityAjustes.SensibilidadPorDefecto, CityAjustes.FovPorDefecto, CityAjustes.TextoPorDefecto);
                    Require(Mathf.Approximately(Camera.main.fieldOfView, CityAjustes.FovPorDefecto), "El FOV no volvió al valor por defecto.");
                    Debug.Log("PUERTO_ALBA_HUD banner=" + hud.ZoneText + " ajustes=limitados");
                    Debug.Log("TYCOON_PUERTO_ALBA_SMOKE_OK: apoyo sobre el suelo y cambio de zona en Casa Marea, calle y mercado.");
                    Finish(0);
                    return;
                }
                Place(step);
                stepStart = Time.time; realStart = Time.realtimeSinceStartup;
            }
            catch (Exception e) { Fail(e); }
        }

        private static void Place(int index)
        {
            var c = player.GetComponent<CharacterController>();
            c.enabled = false;
            player.transform.position = Path[index].at;
            c.enabled = true;
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
            EditorApplication.update -= Step;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorSettings.enterPlayModeOptions = prevOptions;
            EditorSettings.enterPlayModeOptionsEnabled = prevOptionsEnabled;
            if (code == 0) EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(code);
        }
    }

    /// <summary>Almacén en memoria para el smoke: no escribe en PlayerPrefs.</summary>
    internal sealed class MemoriaSmoke : Tycoon3D.City.IAjustesStore
    {
        private readonly System.Collections.Generic.Dictionary<string, float> datos = new System.Collections.Generic.Dictionary<string, float>();
        public bool TryGetFloat(string key, out float value) => datos.TryGetValue(key, out value);
        public void SetFloat(string key, float value) => datos[key] = value;
    }
}
