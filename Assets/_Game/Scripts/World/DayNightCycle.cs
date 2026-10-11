using System;
using System.Globalization;
using Tycoon3D.Core;
using Tycoon3D.Dev;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Tycoon3D.World
{
    /// <summary>
    /// Ciclo de día y noche. Avanza el <see cref="GameClock"/> y mueve el sol (y de noche la luna), el color de la luz, el
    /// ambiente, la niebla y el cielo a partir de lo que trae la escena (al cargar cada escena se vuelven a leer sus valores, así
    /// que R no deja la luz a medias). La noche nunca baja del ~35 % del ambiente del día y las luces propias del casino y de la
    /// cocina no se tocan: se leen las rutas, los rótulos y las máquinas. Vive en el objeto del perfil, no necesita cambios en la
    /// escena. Argumentos: <c>-clock-hour &lt;h&gt;</c> fija la hora de arranque, <c>-clock-speed &lt;segundos reales por día&gt;</c>
    /// cambia la velocidad y <c>-clock-freeze</c> detiene el reloj (capturas y pruebas).
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class DayNightCycle : MonoBehaviour
    {
        public static DayNightCycle Current { get; private set; }

        private static readonly Color NightSky = new Color(.15f, .19f, .35f);
        private static readonly Color NightEquator = new Color(.16f, .15f, .29f);
        private static readonly Color NightGround = new Color(.08f, .07f, .14f);
        private static readonly Color NightFog = new Color(.06f, .08f, .16f);
        private static readonly Color MoonColor = new Color(.52f, .62f, .95f);
        private static readonly Color DawnColor = new Color(1f, .62f, .36f);
        private const float MoonIntensity = .18f;
        private const float ApplyInterval = .1f;

        public GameClock Clock { get; } = new GameClock();
        public bool Frozen { get; set; }
        public Light Sun => sun;
        public float Daylight => GameClock.Daylight(Clock.Hour);
        public int AppliedCount { get; private set; }

        private Light sun;
        private float sunIntensity;
        private Color sunColor;
        private AmbientMode ambientMode;
        private Color ambientSky, ambientEquator, ambientGround, ambientFlat, fogColor;
        private float ambientIntensity;
        private Material skyInstance;
        private float skyExposure = 1f;
        private float nextApply;
        private bool captured;
        private bool disabledByFlag;

        private void Awake()
        {
            Current = this;
            ReadArguments();
            if (LaunchFlags.Has("-no-daynight")) { Frozen = true; disabledByFlag = true; }       // para medir el rendimiento sin el ciclo
            SceneManager.sceneLoaded += OnSceneLoaded;
            Capture();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (skyInstance != null) Destroy(skyInstance);
            if (Current == this) Current = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Capture();

        private void ReadArguments()
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-clock-hour");
            if (i >= 0 && i + 1 < args.Length && float.TryParse(args[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out float h))
                Clock.SetHour(h);
            i = Array.IndexOf(args, "-clock-speed");
            if (i >= 0 && i + 1 < args.Length && float.TryParse(args[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out float s))
                Clock.RealSecondsPerDay = s;
            if (Array.IndexOf(args, "-clock-freeze") >= 0) Frozen = true;
        }

        /// <summary>Lee la luz y el ambiente que trae la escena recién cargada: son el «día pleno» al que se vuelve cada mañana.</summary>
        public void Capture()
        {
            sun = null;
            float best = -1f;
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional || light.intensity <= best) continue;
                best = light.intensity;
                sun = light;
            }
            if (sun != null) { sunIntensity = sun.intensity; sunColor = sun.color; }
            ambientMode = RenderSettings.ambientMode;
            ambientSky = RenderSettings.ambientSkyColor;
            ambientEquator = RenderSettings.ambientEquatorColor;
            ambientGround = RenderSettings.ambientGroundColor;
            ambientFlat = RenderSettings.ambientLight;
            ambientIntensity = RenderSettings.ambientIntensity;
            fogColor = RenderSettings.fogColor;
            if (skyInstance != null) { Destroy(skyInstance); skyInstance = null; }
            var sky = RenderSettings.skybox;
            if (sky != null && sky.HasProperty("_Exposure"))
            {
                skyExposure = sky.GetFloat("_Exposure");
                skyInstance = new Material(sky) { name = sky.name + " (ciclo)" };
                RenderSettings.skybox = skyInstance;
            }
            captured = true;
            Apply();
        }

        private void Update()
        {
            if (!Frozen) Clock.Advance(Time.deltaTime);
            if (Time.unscaledTime < nextApply) return;
            nextApply = Time.unscaledTime + ApplyInterval;
            Apply();
        }

        /// <summary>Fija la hora y aplica la luz de inmediato (pruebas y capturas).</summary>
        public void SetHour(float hour)
        {
            Clock.SetHour(hour, Clock.Day);
            Apply();
        }

        public void Apply()
        {
            if (!captured || disabledByFlag) return;
            AppliedCount++;
            float hour = Clock.Hour;
            float d = GameClock.Daylight(hour);

            if (sun != null)
            {
                float height = GameClock.SunHeight(hour);
                float elevation = Mathf.Max(7f, height * 68f);
                float azimuth = Mathf.Lerp(95f, 265f, Mathf.Clamp01((hour - 6f) / 12f));
                var sunRotation = Quaternion.Euler(elevation, azimuth, 0f);
                var moonRotation = Quaternion.Euler(42f, 215f, 0f);
                sun.transform.rotation = Quaternion.Slerp(moonRotation, sunRotation, d);
                float warm = Mathf.SmoothStep(0f, 1f, elevation / 38f);
                Color day = Color.Lerp(DawnColor, sunColor, warm);
                sun.color = Color.Lerp(MoonColor, day, d);
                sun.intensity = Mathf.Lerp(sunIntensity * MoonIntensity, sunIntensity, d);
            }

            switch (ambientMode)
            {
                case AmbientMode.Trilight:
                    RenderSettings.ambientSkyColor = Color.Lerp(NightSky, ambientSky, d);
                    RenderSettings.ambientEquatorColor = Color.Lerp(NightEquator, ambientEquator, d);
                    RenderSettings.ambientGroundColor = Color.Lerp(NightGround, ambientGround, d);
                    break;
                case AmbientMode.Flat:
                    RenderSettings.ambientLight = Color.Lerp(NightEquator, ambientFlat, d);
                    break;
                default:
                    RenderSettings.ambientIntensity = Mathf.Lerp(ambientIntensity * .35f, ambientIntensity, d);
                    break;
            }
            RenderSettings.fogColor = Color.Lerp(NightFog, fogColor, d);
            if (skyInstance != null) skyInstance.SetFloat("_Exposure", Mathf.Lerp(skyExposure * .12f, skyExposure, d));
        }
    }
}
