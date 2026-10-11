using Tycoon3D.Core;
using UnityEngine;

namespace Tycoon3D.Interaction
{
    /// <summary>
    /// Las teclas de acción y la escala del texto que eligió el jugador (ajustes del perfil), con las de siempre como respaldo.
    /// Los textos de ayuda en pantalla se arman con las etiquetas de aquí, así que reflejan lo que está asignado.
    /// </summary>
    public static class InputBindings
    {
        private static readonly GameSettings Fallback = new GameSettings();

        public static GameSettings Settings => GameProfile.Current != null ? GameProfile.Current.Settings : Fallback;

        public static KeyCode Interact => (KeyCode)Settings.InteractKey;
        public static KeyCode Secondary => (KeyCode)Settings.SecondaryKey;
        public static KeyCode Lights => (KeyCode)Settings.LightsKey;

        public static string InteractLabel => GameSettings.LabelOf(Settings.InteractKey);
        public static string SecondaryLabel => GameSettings.LabelOf(Settings.SecondaryKey);
        public static string LightsLabel => GameSettings.LabelOf(Settings.LightsKey);

        /// <summary>Multiplicador del texto y de los avisos (1 = 100 %).</summary>
        public static float HudScale => Settings.HudScale;
    }
}
