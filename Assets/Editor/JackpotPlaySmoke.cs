using System;
using System.IO;
using System.Linq;
using Tycoon3D.Casino;
using Tycoon3D.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tycoon3D.Editor
{
    /// <summary>
    /// Prueba en Play Mode de la celebración del jackpot: un jackpot dispara la lluvia de monedas desde las seis máquinas, hace
    /// festejar todas las bombillas y suena la lluvia de monedas; un premio común no celebra; con «luces fijas» las bombillas no
    /// parpadean. Marcador: TYCOON_JACKPOT_OK (no lanzar con -quit).
    /// </summary>
    public static class JackpotPlaySmoke
    {
        private const string ScenePath = "Assets/_Game/Scenes/BarrioResidencial.unity";
        private static bool prevOptionsEnabled;
        private static EnterPlayModeOptions prevOptions;
        private static int frames;
        private static float startedAt;
        private static CasinoService casino;
        private static SlotMachineStation[] machines;

        private sealed class Scripted : IRandomSource
        {
            private readonly System.Collections.Generic.Queue<int> values;
            private readonly System.Random fallback = new System.Random(5);
            public Scripted(params int[] v) { values = new System.Collections.Generic.Queue<int>(v); }
            public int Next(int max) => values.Count > 0 ? Math.Min(values.Dequeue(), max - 1) : fallback.Next(max);
        }

        public static void Run()
        {
            try
            {
                GameProfile.DirectoryOverride = Path.Combine(Path.GetTempPath(), "tycoon-smoke-jackpot-" + Guid.NewGuid().ToString("N"));
                EditorSceneManager.OpenScene(ScenePath);
                prevOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
                prevOptions = EditorSettings.enterPlayModeOptions;
                EditorSettings.enterPlayModeOptionsEnabled = true;
                EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
                EditorApplication.playModeStateChanged += OnPlayMode;
                EditorApplication.EnterPlaymode();
            }
            catch (Exception e) { Fail(e); }
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            frames = 0;
            EditorApplication.update += WaitForStart;
        }

        private static void WaitForStart()
        {
            if (++frames < 4) return;
            EditorApplication.update -= WaitForStart;
            try
            {
                casino = UnityEngine.Object.FindFirstObjectByType<CasinoService>();
                machines = UnityEngine.Object.FindObjectsByType<SlotMachineStation>(FindObjectsSortMode.None);
                Require(casino != null && machines.Length == 6, "Faltan el casino o sus seis máquinas.");
                Require(casino.Celebration != null, "El casino no creó la celebración del jackpot.");
                Require(casino.Sounds != null && casino.Sounds.jackpotCoins != null, "Falta el sonido de la lluvia de monedas en el banco de sonidos.");
                Require(casino.Sounds.jackpotCoins.length > 3f, "La lluvia de monedas debe durar más de 3 s.");
                Require(!machines.Any(m => m.Flashing), "Ninguna máquina debe estar festejando al empezar.");

                // un premio común (trío de chiles) no celebra
                casino.UseRandom(new Scripted(68));
                Require(casino.TrySpin(machines[0]), "No se pudo girar.");
                casino.RevealNow();
                Require(casino.Celebration.Celebrations == 0 && !casino.Celebration.Active, "Un trío de chiles no debe disparar la celebración del jackpot.");

                // el jackpot sí: monedas desde las seis máquinas, todas las bombillas festejan y suena la lluvia de monedas
                int sonidosAntes = casino.Sounds.PlayedCount;
                casino.UseRandom(new Scripted(99));
                Require(casino.TrySpin(machines[1]), "No se pudo girar el jackpot.");
                casino.RevealNow();
                Require(casino.LastRound.HasValue && casino.LastRound.Value.Tier == SlotTier.JackpotFox, "La tirada 99 debía ser el jackpot FOX.");
                Require(casino.Celebration.Celebrations == 1 && casino.Celebration.Active, "El jackpot debía iniciar la celebración.");
                Require(machines.All(m => m.Flashing), "Las seis máquinas debían festejar.");
                Require(casino.Sounds.PlayedCount >= sonidosAntes + 3, "Debían sonar la fanfarria, las monedas y la lluvia de monedas del jackpot.");
                startedAt = Time.realtimeSinceStartup;
                frames = 0;
                EditorApplication.update += WaitForCelebration;
            }
            catch (Exception e) { Fail(e); }
        }

        private static void WaitForCelebration()
        {
            try
            {
                if (Time.realtimeSinceStartup - startedAt < JackpotCelebration.Duration + .8f) return;
                EditorApplication.update -= WaitForCelebration;
                Require(casino.Celebration.CoinBursts >= 6 * 6, "Debían lanzarse 6 oleadas desde las 6 máquinas; hubo " + casino.Celebration.CoinBursts);
                Require(!casino.Celebration.Active, "La celebración debía terminar a los 5 segundos.");
                Require(!machines.Any(m => m.Flashing), "Las máquinas debían dejar de festejar al terminar.");

                // luces fijas: ni la celebración ni las bombillas parpadean
                casino.ReduceFlashing = true;
                bool fijas = true;
                for (float t = 0f; t < 4f; t += .05f) fijas &= FlashPolicy.Lit(t, FlashPolicy.CelebrationHz, casino.ReduceFlashing);
                Require(fijas, "Con luces fijas la política no debe apagar ninguna luz.");
                casino.ReduceFlashing = false;
                Debug.Log("TYCOON_JACKPOT_OK: el jackpot lanza la lluvia de monedas desde las seis máquinas, hace festejar las bombillas y suena la lluvia de monedas durante 5 s; un premio común no celebra y con luces fijas nada parpadea.");
                Finish(0);
            }
            catch (Exception e) { Fail(e); }
        }

        private static void Require(bool ok, string message)
        {
            if (!ok) throw new InvalidOperationException(message);
        }

        private static void Fail(Exception e)
        {
            Debug.LogException(e);
            Finish(1);
        }

        private static void Finish(int code)
        {
            EditorApplication.update -= WaitForStart;
            EditorApplication.update -= WaitForCelebration;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            GameProfile.DirectoryOverride = null;
            EditorSettings.enterPlayModeOptions = prevOptions;
            EditorSettings.enterPlayModeOptionsEnabled = prevOptionsEnabled;
            if (code == 0) EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(code);
        }
    }
}
