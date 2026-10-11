using System;
using Tycoon3D.Player;
using UnityEngine;

namespace Tycoon3D.City
{
    /// <summary>
    /// HUD de Puerto Alba: nombre de zona, minimapa y ajustes (sensibilidad, FOV y escala de texto).
    /// La disposición viene de <see cref="CityHudLayout"/>. Los ajustes se guardan como preferencias, no como estado de partida.
    /// </summary>
    public sealed class CityHud : MonoBehaviour
    {
        [SerializeField] private CityZoneTracker tracker;
        [SerializeField] private FirstPersonController player;

        private readonly CityAjustes ajustes = new CityAjustes();
        private IAjustesStore store;
        private GUIStyle labelStyle;
        private GUIStyle titleStyle;

        /// <summary>Texto de zona que muestra el banner en este momento.</summary>
        public string ZoneText { get; private set; } = "Calle";
        public CityAjustes Ajustes => ajustes;
        public bool SettingsOpen => Cursor.lockState != CursorLockMode.Locked;

        /// <summary>Enlaza el HUD con el seguidor de zona y el controlador del jugador.</summary>
        public void Configure(CityZoneTracker zoneTracker, FirstPersonController playerController)
        {
            tracker = zoneTracker;
            player = playerController;
        }

        /// <summary>Cambia el almacén de ajustes (las pruebas usan memoria para no escribir en PlayerPrefs).</summary>
        public void UseStore(IAjustesStore newStore)
        {
            store = newStore;
            ajustes.Load(store);
            Apply();
        }

        /// <summary>Aplica y guarda ajustes; cada valor se limita al rango permitido.</summary>
        public void ApplyAjustes(float sensibilidad, float fov, float escalaTexto)
        {
            ajustes.Set(sensibilidad, fov, escalaTexto);
            Apply();
            ajustes.Save(store);
        }

        private void Awake()
        {
            store = new PlayerPrefsStore();
            ajustes.Load(store);
        }

        private void Start() => Apply();

        private void Update()
        {
            ZoneText = tracker != null && tracker.Known && tracker.CurrentZone.HasValue
                ? CityMap.Get(tracker.CurrentZone.Value).Name
                : "Calle";
        }

        private void Apply()
        {
            if (player == null) return;
            player.MouseSensitivity = ajustes.Sensibilidad;
            player.SetFieldOfView(ajustes.Fov);
        }

        private void OnGUI()
        {
            var f = CityHudLayout.Compute(Screen.width, Screen.height, ajustes.EscalaTexto);
            if (labelStyle == null) labelStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
            if (titleStyle == null) titleStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            labelStyle.fontSize = f.FontSize;
            titleStyle.fontSize = (int)Math.Round(f.FontSize * 1.3f);

            DrawBanner(f);
            DrawMinimap(f);
            GUI.Label(Inset(f.Hint, 8f * f.Scale), "Esc: ajustes y ratón libre   ·   WASD: caminar", labelStyle);
            if (SettingsOpen) DrawSettings(f);
        }

        private void DrawBanner(CityHudFrame f)
        {
            GUI.Box(f.Banner.ToRect(), GUIContent.none);
            GUI.Label(Inset(f.Banner, 12f * f.Scale), ZoneText, titleStyle);
        }

        private void DrawMinimap(CityHudFrame f)
        {
            GUI.Box(f.Minimap.ToRect(), GUIContent.none);
            var map = f.MinimapMap;
            Color prev = GUI.color;
            GUI.color = new Color(.62f, .58f, .53f);
            GUI.DrawTexture(CityHudLayout.BoxToMinimap(CityLayout.Street, map).ToRect(), Texture2D.whiteTexture);
            foreach (var box in CityLayout.Built)
            {
                GUI.color = box.Zone == ZoneId.CasaMarea ? new Color(.95f, .85f, .69f) : new Color(.96f, .79f, .36f);
                GUI.DrawTexture(CityHudLayout.BoxToMinimap(box, map).ToRect(), Texture2D.whiteTexture);
            }
            if (player != null)
            {
                Vector3 p = player.transform.position;
                var (px, py) = CityHudLayout.ToMinimap(p.x, p.z, map);
                float d = 6f * f.Scale;
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(px - d / 2f, py - d / 2f, d, d), Texture2D.whiteTexture);
            }
            GUI.color = prev;
        }

        private void DrawSettings(CityHudFrame f)
        {
            var s = f.Settings;
            float k = f.Scale;
            GUI.Box(s.ToRect(), "Ajustes de Puerto Alba", titleStyle);
            float rowH = 64f * k, x = s.X + 24f * k, w = s.W - 48f * k;
            float y = s.Y + 56f * k;

            GUI.Label(new Rect(x, y, w, 24f * k), "Sensibilidad del ratón: " + ajustes.Sensibilidad.ToString("0.0"), labelStyle);
            float sens = GUI.HorizontalSlider(new Rect(x, y + 26f * k, w, 20f * k), ajustes.Sensibilidad,
                CityAjustes.SensibilidadMin, CityAjustes.SensibilidadMax);

            y += rowH;
            GUI.Label(new Rect(x, y, w, 24f * k), "Campo de visión: " + ajustes.Fov.ToString("0") + "°", labelStyle);
            float fov = GUI.HorizontalSlider(new Rect(x, y + 26f * k, w, 20f * k), ajustes.Fov, CityAjustes.FovMin, CityAjustes.FovMax);

            y += rowH;
            GUI.Label(new Rect(x, y, w, 24f * k), "Tamaño del texto: " + ajustes.EscalaTexto.ToString("0.00") + "×", labelStyle);
            float texto = GUI.HorizontalSlider(new Rect(x, y + 26f * k, w, 20f * k), ajustes.EscalaTexto,
                CityAjustes.TextoMin, CityAjustes.TextoMax);

            if (!Mathf.Approximately(sens, ajustes.Sensibilidad) || !Mathf.Approximately(fov, ajustes.Fov) || !Mathf.Approximately(texto, ajustes.EscalaTexto))
                ApplyAjustes(sens, fov, texto);
        }

        private static Rect Inset(HudRect r, float pad) => new Rect(r.X + pad, r.Y + pad, r.W - 2 * pad, r.H - 2 * pad);

        /// <summary>Preferencias en PlayerPrefs. Un fallo de lectura o escritura no detiene el juego: usa el valor por defecto.</summary>
        private sealed class PlayerPrefsStore : IAjustesStore
        {
            public bool TryGetFloat(string key, out float value)
            {
                value = 0f;
                try
                {
                    if (!PlayerPrefs.HasKey(key)) return false;
                    value = PlayerPrefs.GetFloat(key);
                    return true;
                }
                catch (Exception) { return false; }
            }

            public void SetFloat(string key, float value)
            {
                try { PlayerPrefs.SetFloat(key, value); PlayerPrefs.Save(); }
                catch (Exception) { }
            }
        }
    }

    internal static class HudRectExtensions
    {
        public static Rect ToRect(this HudRect r) => new Rect(r.X, r.Y, r.W, r.H);
    }
}
