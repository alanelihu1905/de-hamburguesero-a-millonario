using System;
using System.IO;
using UnityEngine;

namespace Tycoon3D.Economy
{
    /// <summary>
    /// De dónde salen los precios: <c>StreamingAssets/economy.json</c> (junto al ejecutable en una build, así que se puede editar sin
    /// recompilar) o la ruta de <c>-economy-config &lt;archivo&gt;</c>. Si el archivo falta o es inválido se usan los valores por
    /// defecto del código y se deja una línea <c>ECONOMY_CONFIG</c> en el log; nunca se bloquea el arranque.
    /// </summary>
    public static class EconomyConfigSource
    {
        public const string FileName = "economy.json";
        private static EconomyConfig current;

        /// <summary>Ruta forzada (pruebas). Tiene prioridad sobre el argumento de línea de comandos.</summary>
        public static string PathOverride;

        public static EconomyConfig Current => current ?? (current = Load());

        public static void Reload() => current = Load();

        public static string ResolvePath()
        {
            if (!string.IsNullOrEmpty(PathOverride)) return PathOverride;
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-economy-config");
            if (i >= 0 && i + 1 < args.Length) return args[i + 1];
            return Path.Combine(Application.streamingAssetsPath, FileName);
        }

        private static EconomyConfig Load()
        {
            string path = ResolvePath();
            try
            {
                if (!File.Exists(path))
                {
                    Debug.Log("ECONOMY_CONFIG sin archivo (" + path + "): valores por defecto");
                    return EconomyConfig.Default;
                }
                if (EconomyConfig.TryParse(File.ReadAllText(path), out var config, out string error))
                {
                    Debug.Log("ECONOMY_CONFIG cargado de " + path + " ingredientes=$" + config.IngredientCost + " venta=$" + config.SalePrice + " lote=" + config.ChipLot);
                    return config;
                }
                Debug.LogWarning("ECONOMY_CONFIG inválido en " + path + ": " + error + ". Se usan los valores por defecto.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("ECONOMY_CONFIG no se pudo leer " + path + ": " + e.Message + ". Se usan los valores por defecto.");
            }
            return EconomyConfig.Default;
        }
    }
}
