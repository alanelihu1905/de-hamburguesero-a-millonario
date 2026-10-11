using System;
using System.Collections.Generic;

namespace Tycoon3D.Dev
{
    /// <summary>
    /// Estadísticas de fotogramas de una medición de rendimiento (sin Unity, para probarlas con dotnet). Entra el tiempo de cada
    /// fotograma en segundos; salen los fotogramas por segundo medios, el peor 1 % (la media de los fotogramas más lentos, que es
    /// lo que se siente como tirones) y el fotograma más largo. Son cifras de UNA máquina y UNA escena: no valen como promesa.
    /// </summary>
    public struct PerfStats
    {
        public int Frames;
        public double Seconds;
        public double AverageFps;
        public double OnePercentLowFps;
        public double MinFps;
        public double MaxFrameMs;
        public double MedianFrameMs;
        public double P99FrameMs;

        public static PerfStats Compute(IReadOnlyList<float> frameSeconds)
        {
            var valid = new List<double>();
            if (frameSeconds != null)
                foreach (float f in frameSeconds)
                    if (f > 0f && !float.IsNaN(f) && !float.IsInfinity(f)) valid.Add(f);
            if (valid.Count == 0) return default;

            double total = 0, max = 0;
            foreach (double f in valid) { total += f; max = Math.Max(max, f); }
            var sorted = new List<double>(valid);
            sorted.Sort();                                           // de menor a mayor tiempo: el final son los fotogramas más lentos

            int worst = Math.Max(1, (int)Math.Ceiling(sorted.Count * 0.01));
            double worstSum = 0;
            for (int i = sorted.Count - worst; i < sorted.Count; i++) worstSum += sorted[i];
            double p99 = sorted[Math.Min(sorted.Count - 1, (int)Math.Ceiling(sorted.Count * 0.99) - 1)];

            return new PerfStats
            {
                Frames = valid.Count,
                Seconds = total,
                AverageFps = valid.Count / total,
                OnePercentLowFps = worst / worstSum,
                MinFps = 1.0 / max,
                MaxFrameMs = max * 1000.0,
                MedianFrameMs = sorted[sorted.Count / 2] * 1000.0,
                P99FrameMs = p99 * 1000.0
            };
        }

        public override string ToString() =>
            "frames=" + Frames + " duracion_s=" + Seconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) +
            " fps_medio=" + AverageFps.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) +
            " fps_peor1pct=" + OnePercentLowFps.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) +
            " fps_min=" + MinFps.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) +
            " ms_mediana=" + MedianFrameMs.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) +
            " ms_p99=" + P99FrameMs.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) +
            " ms_max=" + MaxFrameMs.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
    }
}
