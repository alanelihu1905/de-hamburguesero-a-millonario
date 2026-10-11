using System;

namespace Tycoon3D.City
{
    /// <summary>Almacén clave-valor para los ajustes. En Unity lo cubre PlayerPrefs; en las pruebas, un diccionario.</summary>
    public interface IAjustesStore
    {
        bool TryGetFloat(string key, out float value);
        void SetFloat(string key, float value);
    }

    /// <summary>
    /// Ajustes de vista y texto de Puerto Alba. Son preferencias del equipo, no estado de partida: no tocan el guardado
    /// de la economía. Valores fuera de rango se limitan; un valor ilegible (NaN, infinito) vuelve al valor por defecto.
    /// </summary>
    public sealed class CityAjustes
    {
        public const float SensibilidadMin = .5f, SensibilidadMax = 5f, SensibilidadPorDefecto = 2f;
        public const float FovMin = 50f, FovMax = 90f, FovPorDefecto = 60f;
        public const float TextoMin = .85f, TextoMax = 1.5f, TextoPorDefecto = 1f;

        public const string KeySensibilidad = "ciudad.sensibilidad";
        public const string KeyFov = "ciudad.fov";
        public const string KeyTexto = "ciudad.escala_texto";

        public float Sensibilidad { get; private set; } = SensibilidadPorDefecto;
        public float Fov { get; private set; } = FovPorDefecto;
        public float EscalaTexto { get; private set; } = TextoPorDefecto;

        public void Set(float sensibilidad, float fov, float escalaTexto)
        {
            Sensibilidad = ClampSensibilidad(sensibilidad);
            Fov = ClampFov(fov);
            EscalaTexto = ClampTextScale(escalaTexto);
        }

        public void Load(IAjustesStore store)
        {
            if (store == null) return;
            Set(Read(store, KeySensibilidad, SensibilidadPorDefecto),
                Read(store, KeyFov, FovPorDefecto),
                Read(store, KeyTexto, TextoPorDefecto));
        }

        public void Save(IAjustesStore store)
        {
            if (store == null) return;
            store.SetFloat(KeySensibilidad, Sensibilidad);
            store.SetFloat(KeyFov, Fov);
            store.SetFloat(KeyTexto, EscalaTexto);
        }

        public static float ClampSensibilidad(float v) => Clamp(v, SensibilidadMin, SensibilidadMax, SensibilidadPorDefecto);
        public static float ClampFov(float v) => Clamp(v, FovMin, FovMax, FovPorDefecto);
        public static float ClampTextScale(float v) => Clamp(v, TextoMin, TextoMax, TextoPorDefecto);

        private static float Read(IAjustesStore store, string key, float fallback)
        {
            return store.TryGetFloat(key, out float v) ? Clamp(v, float.MinValue, float.MaxValue, fallback) : fallback;
        }

        private static float Clamp(float v, float min, float max, float fallback)
        {
            if (float.IsNaN(v) || float.IsInfinity(v)) return fallback;
            return Math.Max(min, Math.Min(max, v));
        }
    }
}
