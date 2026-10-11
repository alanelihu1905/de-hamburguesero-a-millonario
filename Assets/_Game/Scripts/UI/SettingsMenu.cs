using Tycoon3D.Core;
using UnityEngine;

namespace Tycoon3D.UI
{
    /// <summary>
    /// Menú de ajustes de accesibilidad. Se abre con Esc (cuando el cursor queda libre) y se cierra con otro Esc o con un clic
    /// fuera del recuadro. Permite cambiar el tamaño del texto de las ayudas en pantalla y reasignar las teclas de interactuar,
    /// de la acción secundaria (siguiente apuesta, canjear fichas) y de las luces fijas. Todo se guarda en el perfil.
    /// Para reasignar: clic en la tecla y pulsar la nueva; Esc cancela. Se crea solo desde el perfil, sin tocar la escena.
    /// </summary>
    public sealed class SettingsMenu : MonoBehaviour
    {
        public static SettingsMenu Current { get; private set; }

        /// <summary>True mientras se espera la nueva tecla de una acción (Esc debe cancelar, no volver al juego).</summary>
        public static bool Listening => Current != null && Current.listening.HasValue;
        public static bool IsOpen => Current != null && Current.open;

        private bool open;
        private GameAction? listening;
        private string message = string.Empty;
        private Rect lastRect;
        private GUIStyle label, button, title;

        private void Awake() => Current = this;

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        /// <summary>True si el puntero (coordenadas de Input.mousePosition) está dentro del recuadro abierto.</summary>
        public static bool PointerInside(Vector2 mouse)
        {
            if (Current == null || !Current.open) return false;
            return Current.lastRect.Contains(new Vector2(mouse.x, Screen.height - mouse.y));
        }

        private void Update()
        {
            bool free = Cursor.lockState != CursorLockMode.Locked;
            if (open && !free) { open = false; listening = null; message = string.Empty; }
            else if (!open && free && Time.frameCount > 2) open = true;
        }

        private GameSettings Settings => GameProfile.Current != null ? GameProfile.Current.Settings : null;

        private void Styles(float s)
        {
            label = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(20 * s), alignment = TextAnchor.MiddleLeft, wordWrap = true };
            label.normal.textColor = Color.white;
            title = new GUIStyle(label) { fontSize = Mathf.RoundToInt(26 * s), fontStyle = FontStyle.Bold };
            button = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(20 * s), fontStyle = FontStyle.Bold };
        }

        private void OnGUI()
        {
            var settings = Settings;
            if (!open || settings == null) return;

            // el recuadro se ajusta a la pantalla: nunca más ancho que el 92 % ni más alto que el 92 %
            float s = Mathf.Min(Mathf.Clamp(Screen.height / 900f, .8f, 2f) * Mathf.Clamp(settings.HudScale, .8f, 1.6f),
                                Screen.width * .92f / 600f, Screen.height * .92f / 460f);
            Styles(s);
            float w = 600f * s, h = 460f * s;
            var box = new Rect(Screen.width * .5f - w * .5f, Screen.height * .5f - h * .5f, w, h);
            lastRect = box;
            GUI.Box(box, string.Empty);

            if (listening.HasValue && Event.current.type == EventType.KeyDown && Event.current.keyCode != KeyCode.None)
            {
                var action = listening.Value;
                if (Event.current.keyCode == KeyCode.Escape) message = "Cambio cancelado.";
                else if (settings.TryRebind(action, (int)Event.current.keyCode, out string reason))
                {
                    message = GameSettings.NameOf(action) + ": ahora es " + GameSettings.LabelOf((int)Event.current.keyCode) + ".";
                    GameProfile.Current.MarkDirty();
                }
                else message = reason;
                listening = null;
                Event.current.Use();
                return;
            }

            float x = box.x + 24f * s, y = box.y + 14f * s, rowH = 44f * s, width = w - 48f * s;
            GUI.Label(new Rect(x, y, width, 40f * s), "Ajustes", title);
            y += 44f * s;
            GUI.Label(new Rect(x, y, width, rowH), "Esc o un clic fuera del recuadro vuelven al juego.", label);
            y += rowH + 6f * s;

            GUI.Label(new Rect(x, y, width * .5f, rowH), "Tamaño del texto", label);
            if (GUI.Button(new Rect(x + width * .5f, y, 56f * s, rowH - 4f * s), "−", button)) { settings.ChangeScale(-1); GameProfile.Current.MarkDirty(); }
            GUI.Label(new Rect(x + width * .5f + 64f * s, y, 90f * s, rowH), settings.HudScalePercent + " %", label);
            if (GUI.Button(new Rect(x + width * .5f + 150f * s, y, 56f * s, rowH - 4f * s), "+", button)) { settings.ChangeScale(1); GameProfile.Current.MarkDirty(); }
            y += rowH + 10f * s;

            foreach (GameAction action in System.Enum.GetValues(typeof(GameAction)))
            {
                GUI.Label(new Rect(x, y, width * .56f, rowH), GameSettings.NameOf(action), label);
                string text = listening == action ? "Pulsa una tecla…" : settings.LabelOf(action);
                if (GUI.Button(new Rect(x + width * .58f, y, width * .42f, rowH - 4f * s), text, button))
                {
                    listening = action;
                    message = "Pulsa la nueva tecla para «" + GameSettings.NameOf(action) + "». Esc cancela.";
                }
                y += rowH;
            }
            y += 6f * s;
            GUI.Label(new Rect(x, y, width, rowH * 1.6f), message, label);
            y += rowH * 1.7f;
            if (GUI.Button(new Rect(x, y, width * .5f, rowH - 4f * s), "Restablecer", button))
            {
                settings.ResetDefaults();
                GameProfile.Current.MarkDirty();
                message = "Se restablecieron las teclas y el tamaño del texto.";
            }
        }
    }
}
