using System;

namespace Tycoon3D.City
{
    /// <summary>Rectángulo en píxeles de pantalla, origen arriba a la izquierda (como IMGUI).</summary>
    public struct HudRect
    {
        public float X, Y, W, H;

        public HudRect(float x, float y, float w, float h) { X = x; Y = y; W = w; H = h; }

        public float Right => X + W;
        public float Bottom => Y + H;

        public bool Inside(HudRect outer) => X >= outer.X && Y >= outer.Y && Right <= outer.Right && Bottom <= outer.Bottom;
        public bool Overlaps(HudRect o) => X < o.Right && o.X < Right && Y < o.Bottom && o.Y < Bottom;
        public bool Contains(float px, float py) => px >= X && px <= Right && py >= Y && py <= Bottom;
    }

    /// <summary>Disposición de todos los elementos del HUD de la ciudad para una resolución dada.</summary>
    public struct CityHudFrame
    {
        public float Scale;        // escala de diseño: 1 en 1280×720, hasta 2 en pantallas grandes
        public int FontSize;       // tamaño de texto base ya escalado
        public HudRect Banner;     // nombre de zona, arriba a la izquierda
        public HudRect Minimap;    // panel del minimapa, abajo a la derecha
        public HudRect MinimapMap;  // área del mapa dentro del panel, con la proporción de Puerto Alba
        public HudRect Hint;       // pista de controles, abajo a la izquierda
        public HudRect Settings;   // panel de ajustes (modal), centrado
    }

    /// <summary>
    /// Matemática del HUD de la ciudad, sin Unity: posiciones, escalas y mapeo del minimapa.
    /// Se prueba con dotnet (Tests/CitySmoke). Las resoluciones objetivo son 1280×720, 1600×900 y 2560×1080.
    /// </summary>
    public static class CityHudLayout
    {
        public const int ReferenceWidth = 1280;
        public const int ReferenceHeight = 720;
        public const float BaseFontSize = 14f;

        /// <summary>Escala del HUD: crece con la resolución (sin reducirse por debajo del diseño de 1280×720) y multiplica el tamaño de texto elegido.</summary>
        public static float Scale(int screenWidth, int screenHeight, float textScale)
        {
            float ratio = Math.Min(screenWidth / (float)ReferenceWidth, screenHeight / (float)ReferenceHeight);
            ratio = Math.Max(1f, Math.Min(2f, ratio));
            return ratio * CityAjustes.ClampTextScale(textScale);
        }

        public static CityHudFrame Compute(int screenWidth, int screenHeight, float textScale)
        {
            float s = Scale(screenWidth, screenHeight, textScale);
            float W = screenWidth, H = screenHeight, m = 16f * s;

            var frame = new CityHudFrame
            {
                Scale = s,
                FontSize = (int)Math.Round(BaseFontSize * s),
                Banner = new HudRect(m, m, 340f * s, 64f * s),
                Hint = new HudRect(m, H - m - 28f * s, 520f * s, 28f * s),
                Settings = new HudRect((W - 520f * s) / 2f, (H - 300f * s) / 2f, 520f * s, 300f * s),
            };
            float side = 200f * s;
            frame.Minimap = new HudRect(W - m - side, H - m - side, side, side);
            frame.MinimapMap = FitWorld(frame.Minimap, 8f * s);
            return frame;
        }

        /// <summary>Límites del mundo de Puerto Alba que se dibujan en el minimapa (zonas construidas y calle).</summary>
        public static void WorldBounds(out float minX, out float minZ, out float maxX, out float maxZ)
        {
            minX = float.MaxValue; minZ = float.MaxValue; maxX = float.MinValue; maxZ = float.MinValue;
            foreach (var b in CityLayout.Built)
            {
                minX = Math.Min(minX, b.MinX); minZ = Math.Min(minZ, b.MinZ);
                maxX = Math.Max(maxX, b.MaxX); maxZ = Math.Max(maxZ, b.MaxZ);
            }
        }

        /// <summary>Área del mapa dentro del panel: mantiene la proporción del mundo y deja margen.</summary>
        public static HudRect FitWorld(HudRect panel, float margin)
        {
            WorldBounds(out float minX, out _, out float maxX, out _);
            WorldBounds(out _, out float minZ, out _, out float maxZ);
            float worldW = maxX - minX, worldH = maxZ - minZ;
            float availW = panel.W - 2 * margin, availH = panel.H - 2 * margin;
            float k = Math.Min(availW / worldW, availH / worldH);
            float w = worldW * k, h = worldH * k;
            return new HudRect(panel.X + (panel.W - w) / 2f, panel.Y + (panel.H - h) / 2f, w, h);
        }

        /// <summary>Punto del mundo (x, z) en píxeles dentro del área del mapa. El norte (z+) queda arriba.</summary>
        public static (float px, float py) ToMinimap(float x, float z, HudRect map)
        {
            WorldBounds(out float minX, out _, out float maxX, out _);
            WorldBounds(out _, out float minZ, out _, out float maxZ);
            float u = (x - minX) / (maxX - minX);
            float v = (maxZ - z) / (maxZ - minZ);
            return (map.X + u * map.W, map.Y + v * map.H);
        }

        /// <summary>Rectángulo en el minimapa de una caja de zona (para dibujarla).</summary>
        public static HudRect BoxToMinimap(ZoneBox box, HudRect map)
        {
            var (x0, y0) = ToMinimap(box.MinX, box.MaxZ, map);
            var (x1, y1) = ToMinimap(box.MaxX, box.MinZ, map);
            return new HudRect(x0, y0, x1 - x0, y1 - y0);
        }
    }
}
