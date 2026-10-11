using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tycoon3D.Economy
{
    /// <summary>
    /// Precios y receta del ciclo de hamburguesas más el lote de fichas de la cajera, como datos editables (sin Unity, para
    /// probarlos con dotnet y validarlos luego en el anfitrión). Se leen de un JSON plano y versionado
    /// (<c>StreamingAssets/economy.json</c>): una clave ausente toma el valor por defecto, una clave desconocida se ignora, y un
    /// archivo ilegible, de una versión futura o con valores fuera de rango se rechaza entero y se usan los valores por defecto.
    /// Los valores por defecto son exactamente los que tenía el código antes de existir este archivo.
    /// </summary>
    public sealed class EconomyConfig
    {
        public const int CurrentVersion = 1;
        public const int MaxPrice = 1_000_000;
        public const int MaxChipLot = 1000;

        /// <summary>Lo que cuestan los ingredientes de una hamburguesa.</summary>
        public int IngredientCost = 15;
        /// <summary>Lo que se cobra por una hamburguesa vendida.</summary>
        public int SalePrice = 50;
        /// <summary>Fichas que la cajera cambia de una vez al pulsar E (o las que alcancen).</summary>
        public int ChipLot = 20;

        public static EconomyConfig Default => new EconomyConfig();

        public bool Validate(out string error)
        {
            error = null;
            if (IngredientCost < 1 || IngredientCost > MaxPrice) error = "ingredientCost debe estar entre 1 y " + MaxPrice;
            else if (SalePrice < 1 || SalePrice > MaxPrice) error = "salePrice debe estar entre 1 y " + MaxPrice;
            else if (ChipLot < 1 || ChipLot > MaxChipLot) error = "chipLot debe estar entre 1 y " + MaxChipLot;
            return error == null;
        }

        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append("  \"version\": ").Append(CurrentVersion.ToString(CultureInfo.InvariantCulture)).Append(",\n");
            sb.Append("  \"ingredientCost\": ").Append(IngredientCost.ToString(CultureInfo.InvariantCulture)).Append(",\n");
            sb.Append("  \"salePrice\": ").Append(SalePrice.ToString(CultureInfo.InvariantCulture)).Append(",\n");
            sb.Append("  \"chipLot\": ").Append(ChipLot.ToString(CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>Lee el JSON. Si algo falla devuelve false y deja los valores por defecto en <paramref name="config"/>.</summary>
        public static bool TryParse(string json, out EconomyConfig config, out string error)
        {
            config = Default;
            if (!TryReadFlat(json, out var values, out error)) return false;
            if (values.TryGetValue("version", out long version))
            {
                if (version < 1) { error = "version inválida"; return false; }
                if (version > CurrentVersion) { error = "archivo de una versión futura (" + version + "): no se interpreta"; return false; }
            }
            var parsed = new EconomyConfig();
            if (!Take(values, "ingredientCost", ref parsed.IngredientCost, out error)) return false;
            if (!Take(values, "salePrice", ref parsed.SalePrice, out error)) return false;
            if (!Take(values, "chipLot", ref parsed.ChipLot, out error)) return false;
            if (!parsed.Validate(out error)) return false;
            config = parsed;
            return true;
        }

        private static bool Take(Dictionary<string, long> values, string key, ref int target, out string error)
        {
            error = null;
            if (!values.TryGetValue(key, out long v)) return true;
            if (v < int.MinValue || v > int.MaxValue) { error = key + " fuera de rango"; return false; }
            target = (int)v;
            return true;
        }

        // ---------------------------------------------------------------- lector mínimo de un objeto JSON plano (claves de texto y enteros)
        private static bool TryReadFlat(string text, out Dictionary<string, long> values, out string error)
        {
            var reader = new FlatReader(text);
            bool ok = reader.Read();
            values = reader.Values;
            error = reader.Error;
            return ok;
        }

        private sealed class FlatReader
        {
            private readonly string text;
            private int i;
            public readonly Dictionary<string, long> Values = new Dictionary<string, long>();
            public string Error;

            public FlatReader(string text) { this.text = text; }

            private void Skip() { while (i < text.Length && char.IsWhiteSpace(text[i])) i++; }

            private bool Fail(string message) { Error = message; return false; }

            private bool Finish()
            {
                Skip();
                return i == text.Length || Fail("texto sobrante después del objeto");
            }

            public bool Read()
            {
                if (string.IsNullOrWhiteSpace(text)) return Fail("archivo vacío");
                Skip();
                if (i >= text.Length || text[i] != '{') return Fail("se esperaba '{'");
                i++;
                Skip();
                if (i < text.Length && text[i] == '}') { i++; return Finish(); }
                while (true)
                {
                    Skip();
                    if (i >= text.Length || text[i] != '"') return Fail("se esperaba el nombre de una clave entre comillas");
                    int start = ++i;
                    while (i < text.Length && text[i] != '"') i++;
                    if (i >= text.Length) return Fail("clave sin cerrar");
                    string key = text.Substring(start, i - start);
                    i++;
                    Skip();
                    if (i >= text.Length || text[i] != ':') return Fail("se esperaba ':' después de \"" + key + "\"");
                    i++;
                    Skip();
                    int numStart = i;
                    if (i < text.Length && (text[i] == '-' || text[i] == '+')) i++;
                    while (i < text.Length && text[i] >= '0' && text[i] <= '9') i++;
                    string number = text.Substring(numStart, i - numStart);
                    if (!long.TryParse(number, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long value))
                        return Fail("\"" + key + "\" debe ser un número entero");
                    if (Values.ContainsKey(key)) return Fail("clave repetida: " + key);
                    Values[key] = value;
                    Skip();
                    if (i < text.Length && text[i] == ',') { i++; continue; }
                    if (i < text.Length && text[i] == '}') { i++; return Finish(); }
                    return Fail("se esperaba ',' o '}'");
                }
            }
        }
    }
}
