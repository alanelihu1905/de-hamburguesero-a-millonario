using System;
using System.IO;
using Tycoon3D.Casino;
using Tycoon3D.Economy;
using Tycoon3D.Restaurant;
using Tycoon3D.Save;
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
        private bool dirty;

        public SaveData Data { get; private set; } = new SaveData();
        public LoadOutcome Outcome { get; private set; }
        public int Writes { get; private set; }
        public string Directory => repository != null ? repository.Directory : null;
        public bool ReadOnly => repository != null && repository.ReadOnly;
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
            LoadFromDisk();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
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
            repository = new SaveRepository(ResolveDirectory());
            Outcome = repository.Load(out var loaded);
            Data = loaded;
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
            if (wallet != null && Data.HasProfile) wallet.Restore((int)Math.Min(int.MaxValue, Data.Pesos));
            if (burger != null && Data.HasProfile) burger.Restore((int)Data.BurgerStep, (int)Data.BurgerSales);
            if (casino != null) recovered = casino.AttachProfile(this);
            if (wallet != null) wallet.Changed += OnWalletChanged;
            if (burger != null) burger.Changed += MarkDirty;
            if (recovered > 0)
                Debug.Log("SAVE_PENDING_SETTLED premio=" + recovered);
            MarkDirty();                                                       // un perfil nuevo se escribe en el primer fotograma
        }

        private void Unsubscribe()
        {
            if (wallet != null) wallet.Changed -= OnWalletChanged;
            if (burger != null) burger.Changed -= MarkDirty;
        }

        private void OnWalletChanged(int _) => MarkDirty();

        public void MarkDirty() => dirty = true;

        private void LateUpdate()
        {
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
            else if (repository.LastError != null) Debug.LogWarning("SAVE_FAIL " + repository.LastError);
        }

        private void Capture(SaveData d)
        {
            if (wallet != null) d.Pesos = wallet.Pesos;
            if (burger != null) { d.BurgerStep = (int)burger.Step; d.BurgerSales = burger.Sales; }
            if (casino != null && casino.Session != null) CasinoProfileMapping.Write(d, casino.Session.Snapshot());
        }
    }
}
