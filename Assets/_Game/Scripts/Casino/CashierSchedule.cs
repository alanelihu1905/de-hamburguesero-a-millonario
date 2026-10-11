using System.Globalization;

namespace Tycoon3D.Casino
{
    /// <summary>
    /// Horario de la cajera del casino (sin Unity). Trabaja de las 08:00 a las 22:00 del reloj del juego. Fuera de turno no cambia
    /// pesos por fichas, pero el casino sigue abierto: las máquinas se juegan con pesos, y las fichas que ya se tienen se pueden
    /// apostar y canjear en la ventanilla automática. Así el horario nunca bloquea ni deja valor atrapado.
    /// </summary>
    public static class CashierSchedule
    {
        public const int StartHour = 8;
        public const int EndHour = 22;

        public static float Normalize(float hour) => ((hour % 24f) + 24f) % 24f;

        public static bool OnDuty(float hour)
        {
            float h = Normalize(hour);
            return h >= StartHour && h < EndHour;
        }

        public static string Hours => Two(StartHour) + ":00–" + Two(EndHour) + ":00";

        /// <summary>Texto del rótulo del mostrador: «Turno 08:00–22:00».</summary>
        public static string ShiftText => "Turno " + Hours;

        /// <summary>Estado actual: en turno hasta las 22:00, o en descanso hasta las 08:00.</summary>
        public static string StatusText(float hour) => OnDuty(hour) ? "En turno hasta las " + Two(EndHour) + ":00" : "En descanso · vuelve a las " + Two(StartHour) + ":00";

        /// <summary>Horas del juego que faltan para el próximo cambio de turno.</summary>
        public static float HoursUntilChange(float hour)
        {
            float h = Normalize(hour);
            if (h < StartHour) return StartHour - h;
            if (h < EndHour) return EndHour - h;
            return 24f - h + StartHour;
        }

        private static string Two(int n) => n.ToString("00", CultureInfo.InvariantCulture);
    }
}
