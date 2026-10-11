using System;

namespace Tycoon3D.Casino
{
    /// <summary>
    /// Límite de destellos del casino (sin Unity, para probarlo con dotnet). Ninguna luz del casino parpadea más de tres veces por
    /// segundo (un destello = encenderse y apagarse), y con «luces fijas» activo no parpadea nada: las luces quedan encendidas.
    /// Lo usan las bombillas de las máquinas y la celebración del jackpot.
    /// </summary>
    public static class FlashPolicy
    {
        public const float MaxFlashesPerSecond = 3f;
        /// <summary>Ritmo de la celebración del jackpot y de los premios: dos destellos y medio por segundo.</summary>
        public const float CelebrationHz = 2.5f;

        /// <summary>Ritmo permitido: lo pedido, recortado al máximo; cero con luces fijas.</summary>
        public static float EffectiveHz(float requestedHz, bool steadyLights)
            => steadyLights ? 0f : Math.Max(0f, Math.Min(requestedHz, MaxFlashesPerSecond));

        /// <summary>True si la luz está encendida en el instante dado. Con luces fijas siempre lo está.</summary>
        public static bool Lit(float seconds, float requestedHz, bool steadyLights)
        {
            float hz = EffectiveHz(requestedHz, steadyLights);
            if (hz <= 0f) return true;
            return (int)Math.Floor(seconds * hz * 2f) % 2 == 0;
        }

        /// <summary>Intensidad de 0 a 1 de un pulso suave; con luces fijas queda a media intensidad, sin cambios.</summary>
        public static float Pulse(float seconds, float requestedHz, bool steadyLights)
        {
            float hz = EffectiveHz(requestedHz, steadyLights);
            if (hz <= 0f) return .6f;
            return .5f + .5f * (float)Math.Sin(2.0 * Math.PI * hz * seconds);
        }
    }
}
