using System;
using Tycoon3D.Save;

namespace Tycoon3D.Core
{
    public enum GameAction { Interact, Secondary, Lights }

    /// <summary>
    /// Ajustes de accesibilidad del jugador (sin Unity, para probarlos con dotnet): escala del texto y de los avisos en pantalla
    /// y las teclas de interactuar, de la acción secundaria (siguiente apuesta, canjear fichas) y de las luces fijas. Las teclas
    /// se guardan como el número de su KeyCode de Unity. Se guardan en el perfil; un valor fuera de rango en un perfil se rechaza
    /// como dañado, y aquí se acota para que un valor raro nunca deje el juego ilegible.
    /// </summary>
    public sealed class GameSettings
    {
        public const int MinScalePercent = 70;
        public const int MaxScalePercent = 220;
        public const int ScaleStepPercent = 10;

        public const int KeyE = 101, KeyQ = 113, KeyF = 102;
        private const int KeyTab = 9, KeySpace = 32, KeyA = 97, KeyZ = 122;
        // movimiento (WASD), reiniciar (R), soltar el cursor (Esc) y apuestas (1-4) no se pueden reasignar a una acción
        private static readonly int[] Reserved = { 97 /*a*/, 100 /*d*/, 115 /*s*/, 119 /*w*/, 114 /*r*/, 27 /*esc*/ };

        public int HudScalePercent { get; private set; } = 100;
        public int InteractKey { get; private set; } = KeyE;
        public int SecondaryKey { get; private set; } = KeyQ;
        public int LightsKey { get; private set; } = KeyF;

        public float HudScale => HudScalePercent / 100f;

        public int KeyOf(GameAction action)
        {
            switch (action)
            {
                case GameAction.Interact: return InteractKey;
                case GameAction.Secondary: return SecondaryKey;
                default: return LightsKey;
            }
        }

        public static string NameOf(GameAction action)
        {
            switch (action)
            {
                case GameAction.Interact: return "Interactuar";
                case GameAction.Secondary: return "Siguiente apuesta / canjear";
                default: return "Luces fijas";
            }
        }

        /// <summary>Letras (menos las del movimiento y R), Tab y Espacio.</summary>
        public static bool IsAssignable(int keyCode)
        {
            if (Array.IndexOf(Reserved, keyCode) >= 0) return false;
            return (keyCode >= KeyA && keyCode <= KeyZ) || keyCode == KeyTab || keyCode == KeySpace;
        }

        /// <summary>Texto corto de la tecla para los avisos: «E», «Q», «Espacio».</summary>
        public static string LabelOf(int keyCode)
        {
            if (keyCode == KeySpace) return "Espacio";
            if (keyCode == KeyTab) return "Tab";
            if (keyCode >= KeyA && keyCode <= KeyZ) return ((char)(keyCode - 32)).ToString();
            return "?";
        }

        public string LabelOf(GameAction action) => LabelOf(KeyOf(action));

        public void SetScalePercent(int percent)
        {
            int rounded = (int)Math.Round(percent / (double)ScaleStepPercent) * ScaleStepPercent;
            HudScalePercent = Math.Max(MinScalePercent, Math.Min(MaxScalePercent, rounded));
        }

        public void ChangeScale(int steps) => SetScalePercent(HudScalePercent + steps * ScaleStepPercent);

        /// <summary>Asigna una tecla a una acción. Rechaza teclas reservadas o que ya usa otra acción y dice por qué.</summary>
        public bool TryRebind(GameAction action, int keyCode, out string reason)
        {
            reason = null;
            if (!IsAssignable(keyCode)) { reason = "Esa tecla no se puede usar (se reserva para moverte, reiniciar o soltar el cursor)."; return false; }
            foreach (GameAction other in Enum.GetValues(typeof(GameAction)))
                if (other != action && KeyOf(other) == keyCode) { reason = "La tecla " + LabelOf(keyCode) + " ya es de «" + NameOf(other) + "»."; return false; }
            Assign(action, keyCode);
            return true;
        }

        public void ResetDefaults()
        {
            HudScalePercent = 100;
            InteractKey = KeyE;
            SecondaryKey = KeyQ;
            LightsKey = KeyF;
        }

        private void Assign(GameAction action, int keyCode)
        {
            switch (action)
            {
                case GameAction.Interact: InteractKey = keyCode; break;
                case GameAction.Secondary: SecondaryKey = keyCode; break;
                default: LightsKey = keyCode; break;
            }
        }

        // ---------------------------------------------------------------- perfil
        public void Write(SaveData data)
        {
            data.SettingsScalePercent = HudScalePercent;
            data.SettingsKeyInteract = InteractKey;
            data.SettingsKeySecondary = SecondaryKey;
            data.SettingsKeyLights = LightsKey;
        }

        /// <summary>Lee del perfil; si las teclas guardadas chocan o no se pueden usar, vuelven todas a las de siempre.</summary>
        public void Restore(SaveData data)
        {
            ResetDefaults();
            SetScalePercent((int)data.SettingsScalePercent);
            int[] keys = { (int)data.SettingsKeyInteract, (int)data.SettingsKeySecondary, (int)data.SettingsKeyLights };
            bool ok = true;
            for (int i = 0; i < keys.Length; i++)
            {
                if (!IsAssignable(keys[i])) ok = false;
                for (int j = i + 1; j < keys.Length; j++) if (keys[i] == keys[j]) ok = false;
            }
            if (!ok) return;
            InteractKey = keys[0];
            SecondaryKey = keys[1];
            LightsKey = keys[2];
        }
    }
}
