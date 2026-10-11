using System;
using System.Globalization;

namespace Tycoon3D.Dev
{
    /// <summary>Lee los argumentos de línea de comandos del juego (<c>-bandera</c> o <c>-clave valor</c>).</summary>
    public static class LaunchFlags
    {
        public static bool Has(string flag) => Array.IndexOf(Environment.GetCommandLineArgs(), flag) >= 0;

        public static string Value(string key)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, key);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        public static int IntValue(string key, int fallback)
        {
            string v = Value(key);
            return v != null && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : fallback;
        }
    }
}
