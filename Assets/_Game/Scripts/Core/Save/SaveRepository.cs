using System;
using System.IO;
using System.Text;

namespace Tycoon3D.Save
{
    public enum LoadOutcome { Fresh, Loaded, RecoveredFromBackup, ResetCorrupt, FutureVersionReadOnly }

    /// <summary>
    /// Lee y escribe el perfil en un directorio. Escritura atómica (archivo temporal y reemplazo), copia de seguridad del último
    /// guardado válido y recuperación: si el principal está dañado se usa la copia; si ambos lo están se aparta el dañado y se
    /// empieza limpio. Un archivo de versión futura se respeta: se juega en memoria y no se sobrescribe.
    /// </summary>
    public sealed class SaveRepository
    {
        public const string MainFile = "profile.sav";
        public const string BackupFile = "profile.bak";
        public const string TempFile = "profile.tmp";

        private readonly string directory;
        public SaveRepository(string directory) { this.directory = directory ?? throw new ArgumentNullException(nameof(directory)); }

        public string Directory => directory;
        public bool ReadOnly { get; private set; }
        public string LastError { get; private set; }
        public string MainPath => Path.Combine(directory, MainFile);
        public string BackupPath => Path.Combine(directory, BackupFile);
        private string TempPath => Path.Combine(directory, TempFile);

        public LoadOutcome Load(out SaveData data)
        {
            data = new SaveData();
            ReadOnly = false;
            try
            {
                System.IO.Directory.CreateDirectory(directory);
                if (File.Exists(TempPath)) File.Delete(TempPath);              // escritura interrumpida: el temporal nunca es válido
                bool mainExists = File.Exists(MainPath);
                SaveData main = null;
                var status = ParseStatus.Corrupt;
                if (mainExists) status = SaveCodec.TryParse(File.ReadAllText(MainPath, Encoding.UTF8), out main);
                if (status == ParseStatus.Ok) { data = main; return LoadOutcome.Loaded; }
                if (status == ParseStatus.FutureVersion) { ReadOnly = true; return LoadOutcome.FutureVersionReadOnly; }

                if (File.Exists(BackupPath))
                {
                    var bs = SaveCodec.TryParse(File.ReadAllText(BackupPath, Encoding.UTF8), out var backup);
                    if (bs == ParseStatus.Ok)
                    {
                        if (mainExists) QuarantineMain();
                        data = backup;
                        Write(data);                                           // restablece el principal a partir de la copia
                        return LoadOutcome.RecoveredFromBackup;
                    }
                    if (bs == ParseStatus.FutureVersion) { ReadOnly = true; return LoadOutcome.FutureVersionReadOnly; }
                }
                if (mainExists) { QuarantineMain(); return LoadOutcome.ResetCorrupt; }
                return LoadOutcome.Fresh;
            }
            catch (Exception e)
            {
                LastError = e.Message;
                data = new SaveData();
                return LoadOutcome.ResetCorrupt;
            }
        }

        public bool Save(SaveData data)
        {
            if (ReadOnly) { LastError = "Perfil de una versión futura: no se sobrescribe."; return false; }
            return Write(data);
        }

        private bool Write(SaveData data)
        {
            try
            {
                System.IO.Directory.CreateDirectory(directory);
                data.SaveCount++;
                File.WriteAllText(TempPath, SaveCodec.Serialize(data), new UTF8Encoding(false));
                // la copia de seguridad solo se refresca con un principal válido: un principal dañado nunca pisa una copia buena
                if (File.Exists(MainPath) && SaveCodec.TryParse(File.ReadAllText(MainPath, Encoding.UTF8), out _) == ParseStatus.Ok)
                    File.Copy(MainPath, BackupPath, true);
                if (File.Exists(MainPath)) File.Replace(TempPath, MainPath, null);     // reemplazo atómico (Unity usa .NET Standard 2.1: sin Move con overwrite)
                else File.Move(TempPath, MainPath);
                LastError = null;
                return true;
            }
            catch (Exception e)
            {
                LastError = e.Message;
                return false;
            }
        }

        private void QuarantineMain()
        {
            int n = 1;
            string target;
            do { target = Path.Combine(directory, "profile.corrupt-" + n++); } while (File.Exists(target));
            File.Move(MainPath, target);
        }
    }
}
