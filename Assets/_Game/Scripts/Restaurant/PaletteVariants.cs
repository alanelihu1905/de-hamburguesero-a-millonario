using System;
using System.Collections.Generic;

namespace Tycoon3D.Restaurant
{
    /// <summary>
    /// Variantes de ropa de los clientes con la paleta única del proyecto (sin Unity, para probarlas con dotnet). Los modelos de
    /// los clientes usan una textura de paleta de 128 × 64 con 26 colores en celdas de 16 px; una variante es esa misma textura
    /// con algunas celdas de ropa cambiadas por OTROS colores de la propia paleta. No toca la piel ni el pelo, no añade colores
    /// nuevos, no cambia la geometría (los triángulos son los mismos) y cuesta una textura pequeña por variante.
    /// La tabla de celdas es la de ArtSource/CasaMarea/Palette/palette.json (origen arriba a la izquierda).
    /// </summary>
    public static class PaletteVariants
    {
        public const int CellPx = 16, Columns = 8, Rows = 4;
        public const int Width = Columns * CellPx, Height = Rows * CellPx;

        public struct Swatch
        {
            public string Name; public int Col, Row; public byte R, G, B;
            public Swatch(string name, int col, int row, string hex)
            {
                Name = name; Col = col; Row = row;
                R = Convert.ToByte(hex.Substring(1, 2), 16); G = Convert.ToByte(hex.Substring(3, 2), 16); B = Convert.ToByte(hex.Substring(5, 2), 16);
            }
        }

        public static readonly Swatch[] Palette =
        {
            new Swatch("M_Acero_Claro", 0, 0, "#8E9AA0"), new Swatch("M_Crema", 1, 0, "#F7EEDD"), new Swatch("M_Jitomate", 2, 0, "#C0261F"),
            new Swatch("M_Brasa", 3, 0, "#5A2A1E"), new Swatch("M_Chile", 4, 0, "#E4572E"), new Swatch("M_Carne", 5, 0, "#5A3524"),
            new Swatch("M_Barro", 6, 0, "#B5623B"), new Swatch("M_Caoba", 7, 0, "#8A5A3C"),
            new Swatch("M_Madera_Clara", 0, 1, "#B88556"), new Swatch("M_Cal_Arena", 1, 1, "#E8C4A0"), new Swatch("M_Pan", 2, 1, "#D99A4E"),
            new Swatch("M_Piedra_Caliza", 3, 1, "#CDBFA4"), new Swatch("M_Queso", 4, 1, "#F2B63A"), new Swatch("M_Mango", 5, 1, "#F4C95D"),
            new Swatch("M_Hoja_Clara", 6, 1, "#7BAF4B"), new Swatch("M_Lechuga", 7, 1, "#6FA83F"),
            new Swatch("M_Vegetacion", 0, 2, "#4F7A3A"), new Swatch("M_Turquesa", 1, 2, "#2BA6A0"), new Swatch("M_Plancha_Superficie", 2, 2, "#2B3438"),
            new Swatch("M_Metal_Plancha", 3, 2, "#3E4A50"), new Swatch("M_Pizarra", 4, 2, "#3B2F4A"), new Swatch("M_Herreria", 5, 2, "#2E2538"),
            new Swatch("M_Cebolla", 6, 2, "#8E4A78"), new Swatch("M_Piel_Oscura", 7, 2, "#6B4530"),
            new Swatch("M_Piel_Media", 0, 3, "#C98B5E"), new Swatch("M_Piel_Clara", 1, 3, "#E3B58D"),
        };

        /// <summary>Piel y pelo: nunca se cambian en una variante.</summary>
        public static readonly string[] Protected = { "M_Piel_Oscura", "M_Piel_Media", "M_Piel_Clara", "M_Herreria" };

        public sealed class Variant
        {
            public string Name;
            public (string From, string To)[] Swaps;
        }

        /// <summary>0 = el aspecto original (sin cambios). Las demás cambian solo ropa y accesorios.</summary>
        public static readonly Variant[] All =
        {
            new Variant { Name = "Original", Swaps = new (string, string)[0] },
            new Variant { Name = "Turquesa y mango", Swaps = new[] { ("M_Chile", "M_Turquesa"), ("M_Crema", "M_Mango"), ("M_Turquesa", "M_Chile"), ("M_Piedra_Caliza", "M_Cal_Arena") } },
            new Variant { Name = "Ciruela y hoja", Swaps = new[] { ("M_Chile", "M_Cebolla"), ("M_Crema", "M_Hoja_Clara"), ("M_Turquesa", "M_Queso"), ("M_Pizarra", "M_Metal_Plancha"), ("M_Piedra_Caliza", "M_Crema") } },
            new Variant { Name = "Barro y turquesa", Swaps = new[] { ("M_Chile", "M_Mango"), ("M_Crema", "M_Turquesa"), ("M_Turquesa", "M_Barro"), ("M_Madera_Clara", "M_Pizarra"), ("M_Piedra_Caliza", "M_Acero_Claro") } },
        };

        public static Swatch Find(string name)
        {
            foreach (var s in Palette) if (s.Name == name) return s;
            throw new ArgumentException("No está en la paleta: " + name);
        }

        public static bool Has(string name)
        {
            foreach (var s in Palette) if (s.Name == name) return true;
            return false;
        }

        /// <summary>Textura RGBA de 128 × 64 (fila 0 abajo, como una Texture2D de Unity) con las celdas de la variante.</summary>
        public static byte[] BuildPixels(int variantIndex)
        {
            if (variantIndex < 0 || variantIndex >= All.Length) throw new ArgumentOutOfRangeException(nameof(variantIndex));
            var swaps = new Dictionary<string, string>();
            foreach (var (from, to) in All[variantIndex].Swaps) swaps[from] = to;
            var pixels = new byte[Width * Height * 4];
            foreach (var swatch in Palette)
            {
                var shown = swaps.TryGetValue(swatch.Name, out var replacement) ? Find(replacement) : swatch;
                int x0 = swatch.Col * CellPx;
                int y0 = (Rows - 1 - swatch.Row) * CellPx;              // la fila 0 de la paleta está arriba
                for (int y = 0; y < CellPx; y++)
                    for (int x = 0; x < CellPx; x++)
                    {
                        int i = ((y0 + y) * Width + x0 + x) * 4;
                        pixels[i] = shown.R; pixels[i + 1] = shown.G; pixels[i + 2] = shown.B; pixels[i + 3] = 255;
                    }
            }
            return pixels;
        }

        /// <summary>Color del centro de una celda de la textura.</summary>
        public static (byte R, byte G, byte B) CellColor(byte[] pixels, int col, int row)
        {
            int x = col * CellPx + CellPx / 2, y = (Rows - 1 - row) * CellPx + CellPx / 2;
            int i = (y * Width + x) * 4;
            return (pixels[i], pixels[i + 1], pixels[i + 2]);
        }
    }
}
