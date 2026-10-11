using System;

namespace Tycoon3D.Casino
{
    public struct HudRect
    {
        public float X, Y, W, H;
        public HudRect(float x, float y, float w, float h) { X = x; Y = y; W = w; H = h; }
        public float XMax => X + W;
        public float YMax => Y + H;

        public bool Overlaps(HudRect o) => X < o.XMax && o.X < XMax && Y < o.YMax && o.Y < YMax;
        public bool InsideScreen(float screenW, float screenH) => X >= -0.01f && Y >= -0.01f && XMax <= screenW + 0.01f && YMax <= screenH + 0.01f;
    }

    /// <summary>
    /// Reparto del HUD del casino en pantalla (sin Unity, para probarlo con dotnet en muchas proporciones). Medidas base a escala 1
    /// (pantalla de 1080 px de alto): panel de 780 × 372 anclado abajo, etiqueta «abierto siempre» de 470 × 70 arriba a la derecha
    /// y aviso de la cajera de 760 × 52 arriba al centro. La escala sale de la altura de la pantalla y del ajuste del jugador, y
    /// se recorta para que el panel quepa a lo ancho, no tape la etiqueta ni el aviso y nada se salga. Si el aviso chocaría con
    /// la etiqueta (pantallas casi cuadradas), baja justo debajo de ella.
    /// </summary>
    public static class HudLayout
    {
        public const float PanelW = 780f, PanelH = 372f, PanelBottom = 44f;
        public const float TagW = 470f, TagH = 70f, ToastW = 760f, ToastH = 52f;
        public const float Margin = 16f, Gap = 8f, ToastTop = 20f;
        public const float MinScale = .4f, BaseMin = .7f, BaseMax = 2.2f;

        /// <summary>Alto que necesita la pila arriba (etiqueta y aviso) más el panel de abajo, a escala 1.</summary>
        public const float StackH = Margin + TagH + Gap + ToastH + Gap + PanelH + PanelBottom;

        public struct Result
        {
            public float Scale;
            public HudRect Panel, Tag, Toast;
            public bool ToastBelowTag;
        }

        public static float BaseScale(float screenH) => Clamp(screenH / 1080f, BaseMin, BaseMax);

        /// <summary>Escala que se usa de verdad: la pedida (altura de pantalla por el ajuste del jugador) recortada para que todo quepa.</summary>
        public static float Fit(float screenW, float screenH, float userScale)
        {
            float requested = BaseScale(screenH) * Clamp(userScale, .1f, 4f);
            float byWidth = screenW * .96f / PanelW;
            float byHeight = screenH * .98f / StackH;
            return Math.Max(MinScale, Math.Min(requested, Math.Min(byWidth, byHeight)));
        }

        public static Result Compute(float screenW, float screenH, float userScale = 1f)
        {
            float s = Fit(screenW, screenH, userScale);
            var r = new Result { Scale = s };
            r.Panel = new HudRect(screenW * .5f - PanelW * s * .5f, screenH - (PanelH + PanelBottom) * s, PanelW * s, PanelH * s);
            float tagW = Math.Min(TagW * s, screenW - 2f * Margin * s);
            r.Tag = new HudRect(screenW - tagW - Margin * s, Margin * s, tagW, TagH * s);
            float toastW = Math.Min(ToastW * s, screenW - 2f * Margin * s);
            r.Toast = new HudRect(screenW * .5f - toastW * .5f, ToastTop * s, toastW, ToastH * s);
            if (r.Toast.Overlaps(r.Tag))
            {
                r.ToastBelowTag = true;
                r.Toast = new HudRect(r.Toast.X, r.Tag.YMax + Gap * s, toastW, ToastH * s);
            }
            return r;
        }

        private static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;
    }
}
