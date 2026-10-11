using System;
using System.IO;
using System.Linq;
using Tycoon3D.Core;
using Tycoon3D.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tycoon3D.Editor
{
    /// <summary>
    /// Prueba en Play Mode del ciclo de día y noche: la luz cambia con la hora sin saltos, la noche conserva un ambiente legible
    /// (el casino y la cocina no quedan a oscuras), las luces propias de los interiores no se tocan, la hora se guarda en el
    /// perfil y sobrevive a recargar la escena (R). Marcador: TYCOON_DAYNIGHT_OK (no lanzar con -quit).
    /// </summary>
    public static class DayNightPlaySmoke
    {
        private const string ScenePath = "Assets/_Game/Scenes/BarrioResidencial.unity";
        private static bool prevOptionsEnabled;
        private static EnterPlayModeOptions prevOptions;
        private static int reloadFrames;
        private static string saveDir;

        public static void Run()
        {
            try
            {
                saveDir = Path.Combine(Path.GetTempPath(), "tycoon-smoke-daynight-" + Guid.NewGuid().ToString("N"));
                GameProfile.DirectoryOverride = saveDir;
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

        private static float Luma(Color c) => .2126f * c.r + .7152f * c.g + .0722f * c.b;

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            try
            {
                var cycle = DayNightCycle.Current;
                Require(cycle != null, "No existe el ciclo de día y noche.");
                Require(cycle.Sun != null, "La escena no tiene luz direccional (sol).");
                cycle.Frozen = true;

                var interiors = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.type != LightType.Directional).ToArray();
                Require(interiors.Length >= 5, "Se esperaban las luces del casino y de la cocina; hay " + interiors.Length);
                float[] interiorBefore = interiors.Select(l => l.intensity).ToArray();

                cycle.SetHour(12f);
                float sunNoon = cycle.Sun.intensity, ambientNoon = Luma(RenderSettings.ambientSkyColor + RenderSettings.ambientEquatorColor);
                float fogNoon = Luma(RenderSettings.fogColor);
                cycle.SetHour(2f);
                float sunNight = cycle.Sun.intensity, ambientNight = Luma(RenderSettings.ambientSkyColor + RenderSettings.ambientEquatorColor);
                float fogNight = Luma(RenderSettings.fogColor);
                Require(sunNight < sunNoon * .3f, "De noche el sol/luna debe ser mucho más tenue: " + sunNight + " frente a " + sunNoon);
                Require(sunNight > sunNoon * .05f, "De noche debe quedar luz de luna: " + sunNight);
                Require(ambientNight >= ambientNoon * .22f, "El ambiente nocturno es demasiado oscuro: " + ambientNight + " frente a " + ambientNoon);
                Require(ambientNight < ambientNoon, "El ambiente nocturno debe ser más oscuro que el diurno.");
                Require(fogNight < fogNoon, "La niebla debe oscurecerse de noche.");

                // sin saltos a lo largo del día
                float prevSun = -1f, prevAmb = -1f, stepSun = 0f, stepAmb = 0f;
                for (float h = 0f; h <= 24f; h += .25f)
                {
                    cycle.SetHour(h);
                    float s = cycle.Sun.intensity, a = Luma(RenderSettings.ambientSkyColor + RenderSettings.ambientEquatorColor);
                    if (prevSun >= 0f) { stepSun = Mathf.Max(stepSun, Mathf.Abs(s - prevSun)); stepAmb = Mathf.Max(stepAmb, Mathf.Abs(a - prevAmb)); }
                    prevSun = s; prevAmb = a;
                }
                Require(stepSun < sunNoon * .12f, "La luz del sol da un salto de " + stepSun + " en 15 minutos de juego.");
                Require(stepAmb < ambientNoon * .12f, "El ambiente da un salto de " + stepAmb + " en 15 minutos de juego.");

                // los interiores conservan sus luces
                for (int i = 0; i < interiors.Length; i++)
                    Require(Mathf.Approximately(interiors[i].intensity, interiorBefore[i]), "Cambió la intensidad de una luz de interior: " + interiors[i].name);

                // amanecer: el sol sale por el este (azimut menor), más bajo y más tenue que a mediodía
                cycle.SetHour(7f);
                float dawnAzimuth = cycle.Sun.transform.eulerAngles.y, dawnElevation = cycle.Sun.transform.eulerAngles.x, dawnIntensity = cycle.Sun.intensity;
                cycle.SetHour(12f);
                Require(dawnAzimuth < cycle.Sun.transform.eulerAngles.y, "El sol debe avanzar de este a oeste.");
                Require(dawnElevation < cycle.Sun.transform.eulerAngles.x, "El sol debe estar más bajo al amanecer que a mediodía.");
                Require(dawnIntensity < cycle.Sun.intensity, "El sol debe ser más tenue al amanecer que a mediodía.");

                // la hora se guarda en el perfil y vuelve tras R
                cycle.SetHour(21.5f);
                GameProfile.Current.Flush();
                var probe = new Tycoon3D.Save.SaveRepository(saveDir);
                // el candado lo tiene el juego; se lee el archivo directamente
                string text = File.ReadAllText(probe.MainPath);
                Require(Tycoon3D.Save.SaveCodec.TryParse(text, out var saved) == Tycoon3D.Save.ParseStatus.Ok && saved.ClockMinutes == (long)cycle.Clock.TotalMinutes,
                    "La hora no quedó en el perfil guardado.");
                cycle.Clock.SetHour(9f, 1);
                GameProfile.Current.ReloadFromDisk(false);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);   // equivale a pulsar R
                reloadFrames = 0;
                EditorApplication.update += WaitForReload;
            }
            catch (Exception e) { Fail(e); }
        }

        private static void WaitForReload()
        {
            if (++reloadFrames < 6) return;
            EditorApplication.update -= WaitForReload;
            try
            {
                var cycle = DayNightCycle.Current;
                Require(cycle != null && cycle.Sun != null, "Tras recargar la escena falta el ciclo o el sol.");
                Require(Mathf.Abs(cycle.Clock.Hour - 21.5f) < .1f, "Tras recargar la hora debía seguir en 21:30 y es " + cycle.Clock.Hour);
                float night = cycle.Sun.intensity;
                cycle.SetHour(12f);
                Require(cycle.Sun.intensity > night * 2f, "Tras recargar el ciclo debe seguir moviendo la luz.");
                Debug.Log("TYCOON_DAYNIGHT_OK: la luz cambia con la hora sin saltos, la noche conserva ambiente, los interiores no se tocan y la hora sobrevive a guardar y recargar.");
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
            EditorApplication.update -= WaitForReload;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            GameProfile.DirectoryOverride = null;
            EditorSettings.enterPlayModeOptions = prevOptions;
            EditorSettings.enterPlayModeOptionsEnabled = prevOptionsEnabled;
            if (code == 0) EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(code);
        }
    }
}
