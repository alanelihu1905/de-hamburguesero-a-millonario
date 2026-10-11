using System;
using System.IO;
using Tycoon3D.Casino;
using Tycoon3D.Economy;
using Tycoon3D.Restaurant;
using Tycoon3D.Save;
using Tycoon3D.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tycoon3D.Core
{
    /// <summary>
    /// Perfil persistente. Vive entre escenas: al arrancar y en cada carga (también R) busca el monedero, las hamburguesas y el
    /// casino de la escena y les devuelve lo guardado. Guarda el estado vivo de todos a la vez, una vez por fotograma como máximo,
    /// para que dinero, fichas y ronda pendiente queden siempre coherentes; también al pausar o cerrar la app.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameProfile : MonoBehaviour
    {
        public static GameProfile Current { get; private set; }

        /// <summary>Directorio de guardado forzado (pruebas). Tiene prioridad sobre -save-dir y TYCOON_SAVE_DIR.</summary>
        public static string DirectoryOverride;

        private SaveRepository repository;
        private Wallet wallet;
        private BurgerLoop burger;
        private CasinoService casino;
        private readonly MovementLedger ledger = new MovementLedger();
        private bool dirty;
        private bool warnedReadOnly;
        private DayNightCycle cycle;
        private float nextClockSave;

        public SaveData Data { get; private set; } = new SaveData();
        /// <summary>Ajustes de accesibilidad del jugador (tamaño del texto y teclas); viajan en el perfil.</summary>
        public GameSettings Settings { get; } = new GameSettings();
        public MovementLedger Ledger => ledger;
        public LoadOutcome Outcome { get; private set; }
        public int Writes { get; private set; }
        public string Directory => repository != null ? repository.Directory : null;
        public bool ReadOnly => repository != null && repository.ReadOnly;
        /// <summary>True si otra copia del juego tiene abierto este perfil: aquí se juega sin guardar.</summary>
        public bool LockedByOtherInstance => repository != null && repository.LockedByOtherInstance;
        public string LastError => repository?.LastError;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Current == null)
            {
                var go = new GameObject("Perfil y guardado");
                DontDestroyOnLoad(go);
                go.AddComponent<GameProfile>();
            }
            Current.Bind();
        }

        private static string ResolveDirectory()
        {
            if (!string.IsNullOrEmpty(DirectoryOverride)) return DirectoryOverride;
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-save-dir");
            if (i >= 0 && i + 1 < args.Length) return args[i + 1];
            int shots = Array.IndexOf(args, "-casino-shots");
            if (shots >= 0 && shots + 1 < args.Length) return Path.Combine(args[shots + 1], "save");   // las capturas no tocan el perfil real
            string env = Environment.GetEnvironmentVariable("TYCOON_SAVE_DIR");
            if (!string.IsNullOrEmpty(env)) return env;
            return Path.Combine(Application.persistentDataPath, "Tycoon3D");
        }

        private void Awake()
        {
            if (Current != null && Current != this) { Destroy(gameObject); return; }
            Current = this;
            cycle = gameObject.AddComponent<DayNightCycle>();
            gameObject.AddComponent<Tycoon3D.UI.SettingsMenu>();
            LoadFromDisk();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            repository?.Dispose();
            if (Current == this) Current = null;
        }

        /// <summary>Vuelve a leer el disco (equivale a cerrar y reabrir la app). Guarda antes lo pendiente.</summary>
        public void ReloadFromDisk(bool flushFirst = true)
        {
            if (flushFirst) Flush();
            LoadFromDisk();
        }

        private void LoadFromDisk()
        {
            repository?.Dispose();                                             // suelta el candado antes de volver a tomarlo
            repository = new SaveRepository(ResolveDirectory());
            warnedReadOnly = false;
            Outcome = repository.Load(out var loaded);
            Data = loaded;
            Settings.Restore(Data);
            Debug.Log("SAVE_LOAD " + Outcome + " dir=" + repository.Directory + " pesos=" + Data.Pesos + " fichas=" + Data.Chips +
                      " ventas=" + Data.BurgerSales + " rondas=" + Data.CasinoRounds + " pendiente=" + Data.PendingActive +
                      (repository.LastError != null ? " error=" + repository.LastError : ""));
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Bind();

        private void Bind()
        {
            Unsubscribe();
            wallet = FindFirstObjectByType<Wallet>();
            burger = FindFirstObjectByType<BurgerLoop>();
            casino = FindFirstObjectByType<CasinoService>();
            int recovered = 0;
            RestoreClock();
            var prices = EconomyConfigSource.Current;
            if (burger != null) burger.ApplyConfig(prices);
            if (casino != null) casino.ApplyConfig(prices);
            if (wallet != null && Data.HasProfile) wallet.Restore((int)Math.Min(int.MaxValue, Data.Pesos));
            if (burger != null && Data.HasProfile) burger.Restore((int)Data.BurgerStep, (int)Data.BurgerSales);
            ledger.Restore(Data, wallet != null ? wallet.Pesos : 0, (int)Data.Chips);
            if (burger != null) burger.MovementRecorded += OnBurgerMovement;
            if (casino != null) casino.MovementRecorded += OnCasinoMovement;
            if (casino != null) recovered = casino.AttachProfile(this);
            if (wallet != null) wallet.Changed += OnWalletChanged;
            if (burger != null) burger.Changed += MarkDirty;
            if (recovered > 0)
                Debug.Log("SAVE_PENDING_SETTLED premio=" + recovered);
            MarkDirty();                                                       // un perfil nuevo se escribe en el primer fotograma
        }

        // el reloj sigue donde se dejó; con -clock-hour manda el argumento (capturas y pruebas)
        private void RestoreClock()
        {
            if (cycle == null || !Data.HasProfile || Data.ClockMinutes < 0) return;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-clock-hour") >= 0) return;
            cycle.Clock.Set(Data.ClockMinutes);
            cycle.Apply();
        }

        private void Unsubscribe()
        {
            if (wallet != null) wallet.Changed -= OnWalletChanged;
            if (burger != null) burger.Changed -= MarkDirty;
            if (burger != null) burger.MovementRecorded -= OnBurgerMovement;
            if (casino != null) casino.MovementRecorded -= OnCasinoMovement;
        }

        private void OnBurgerMovement(MovementKind kind, int amount, MovementAccount from, MovementAccount to)
        {
            ledger.Record(kind, amount, from, to);
            MarkDirty();
        }

        private void OnCasinoMovement(MovementKind kind, int amount, MovementAccount from, MovementAccount to, int roundId)
        {
            ledger.Record(kind, amount, from, to, roundId);
            MarkDirty();
        }

        private void OnWalletChanged(int _) => MarkDirty();

        public void MarkDirty() => dirty = true;

        private void LateUpdate()
        {
            if (cycle != null && Time.unscaledTime >= nextClockSave) { nextClockSave = Time.unscaledTime + 15f; MarkDirty(); }   // la hora se guarda cada 15 s
            if (dirty) Flush();
        }

        private void OnApplicationPause(bool paused) { if (paused) Flush(); }
        private void OnApplicationQuit() => Flush();

        /// <summary>Escribe ya el estado vivo (si hay algo que guardar).</summary>
        public void Flush()
        {
            dirty = false;
            if (repository == null || (wallet == null && burger == null && casino == null)) return;
            Capture(Data);
            if (repository.Save(Data)) Writes++;
            else if (repository.LastError != null && !warnedReadOnly)
            {
                Debug.LogWarning("SAVE_FAIL " + repository.LastError);
                warnedReadOnly = repository.ReadOnly;                            // en solo lectura se avisa una vez, no en cada cambio
            }
        }

        private void Capture(SaveData d)
        {
            Settings.Write(d);
            if (cycle != null) d.ClockMinutes = (long)cycle.Clock.TotalMinutes;
            if (wallet != null) d.Pesos = wallet.Pesos;
            if (burger != null) { d.BurgerStep = (int)burger.Step; d.BurgerSales = burger.Sales; }
            if (casino != null && casino.Session != null) CasinoProfileMapping.Write(d, casino.Session.Snapshot());
            ledger.Write(d);
            if (wallet != null && casino != null && !ledger.Matches(wallet.Pesos, casino.Session.Chips))
                Debug.LogWarning("LEDGER_MISMATCH pesos=" + wallet.Pesos + " fichas=" + casino.Session.Chips);
        }
    }
}
