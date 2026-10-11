using System;
using System.Globalization;

namespace Tycoon3D.Core
{
    public enum DayPeriod { Madrugada, Manana, Tarde, Noche }

    /// <summary>
    /// Reloj del juego (sin Unity, para probarlo con dotnet y, a futuro, para que lo marque el anfitrión). El tiempo avanza solo
    /// con <see cref="Advance"/>; un día del juego dura <see cref="RealSecondsPerDay"/> segundos reales. Empieza el día 1 a las
    /// 10:00. La luz del día (<see cref="Daylight"/>) es una curva suave de la hora, sin saltos, que usan la iluminación y los
    /// turnos de la cajera.
    /// </summary>
    public sealed class GameClock
    {
        public const int MinutesPerDay = 1440;
        public const double StartMinutes = 10 * 60;
        public const float DefaultRealSecondsPerDay = 480f;     // 8 minutos reales = 24 horas del juego
        public const float MinRealSecondsPerDay = 10f;

        private double totalMinutes = StartMinutes;
        private float realSecondsPerDay = DefaultRealSecondsPerDay;

        public double TotalMinutes => totalMinutes;
        public float RealSecondsPerDay
        {
            get => realSecondsPerDay;
            set => realSecondsPerDay = Math.Max(MinRealSecondsPerDay, float.IsNaN(value) || float.IsInfinity(value) ? DefaultRealSecondsPerDay : value);
        }

        public int Day => (int)(totalMinutes / MinutesPerDay) + 1;
        public int MinuteOfDay => (int)(totalMinutes % MinutesPerDay);
        public float Hour => (float)((totalMinutes % MinutesPerDay) / 60.0);
        public DayPeriod Period => PeriodOf(Hour);
        public string Text => "Día " + Day.ToString(CultureInfo.InvariantCulture) + " · " + Format(MinuteOfDay);

        public void Advance(float realSeconds)
        {
            if (realSeconds <= 0f || float.IsNaN(realSeconds) || float.IsInfinity(realSeconds)) return;
            totalMinutes += realSeconds * (MinutesPerDay / (double)realSecondsPerDay);
        }

        /// <summary>Fija el reloj (perfil guardado o pruebas). Un valor negativo se toma como el inicio del juego.</summary>
        public void Set(double minutes) => totalMinutes = minutes < 0 || double.IsNaN(minutes) || double.IsInfinity(minutes) ? StartMinutes : minutes;

        public void SetHour(float hour, int day = 1)
        {
            float h = ((hour % 24f) + 24f) % 24f;
            Set((Math.Max(1, day) - 1) * (double)MinutesPerDay + h * 60.0);
        }

        public static string Format(int minuteOfDay) =>
            (minuteOfDay / 60).ToString("00", CultureInfo.InvariantCulture) + ":" + (minuteOfDay % 60).ToString("00", CultureInfo.InvariantCulture);

        public static DayPeriod PeriodOf(float hour)
        {
            if (hour < 6f) return DayPeriod.Madrugada;
            if (hour < 14f) return DayPeriod.Manana;
            if (hour < 20f) return DayPeriod.Tarde;
            return DayPeriod.Noche;
        }

        /// <summary>Altura del sol de -1 (medianoche) a 1 (mediodía a las 12:00), cruzando el horizonte a las 06:00 y las 18:00.</summary>
        public static float SunHeight(float hour) => (float)Math.Sin((hour - 6f) / 24f * 2.0 * Math.PI);

        /// <summary>Luz natural de 0 (noche cerrada) a 1 (día pleno): sube entre las 05:00 y las 08:00 y baja entre las 17:00 y las 20:00.</summary>
        public static float Daylight(float hour)
        {
            float h = ((hour % 24f) + 24f) % 24f;
            if (h < 12f) return Smooth((h - 5f) / 3f);
            return Smooth((20f - h) / 3f);
        }

        private static float Smooth(float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            return t * t * (3f - 2f * t);
        }
    }
}
