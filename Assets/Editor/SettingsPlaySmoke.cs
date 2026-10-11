using System;
using System.IO;
using System.Linq;
using Tycoon3D.Casino;
using Tycoon3D.Core;
using Tycoon3D.Interaction;
using Tycoon3D.Restaurant;
using Tycoon3D.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tycoon3D.Editor
{
    /// <summary>
    /// Prueba en Play Mode de los ajustes de accesibilidad: las teclas elegidas llegan a las ayudas en pantalla (letreros de las
    /// estaciones, de las máquinas y de la cajera), una tecla repetida o reservada se rechaza, y las teclas y el tamaño del texto
    /// sobreviven a guardar y recargar la escena (R). Marcador: TYCOON_SETTINGS_OK (no lanzar con -quit).
    /// </summary>
    public static class SettingsPlaySmoke
    {
        private const string ScenePath = "Assets/_Game/Scenes/BarrioResidencial.unity";
        private static bool prevOptionsEnabled;
        private static EnterPlayModeOptions prevOptions;
        private static int reloadFrames;
        private static string saveDir;

        public static void Run()
        {
            try
            {
                saveDir = Path.Combine(Path.GetTempPath(), "tycoon-smoke-settings-" + Guid.NewGuid().ToString("N"));
                GameProfile.DirectoryOverride = saveDir;
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
            try
            {
                var profile = GameProfile.Current;
                Require(profile != null && SettingsMenu.Current != null, "Faltan el perfil o el menú de ajustes.");
                Require(!SettingsMenu.PointerInside(new Vector2(10, 10)) && !SettingsMenu.Listening, "El menú cerrado no debe capturar el puntero.");
                var settings = profile.Settings;
                Require(settings.HudScalePercent == 100 && InputBindings.Interact == KeyCode.E && InputBindings.Secondary == KeyCode.Q && InputBindings.Lights == KeyCode.F,
                    "Los ajustes iniciales deben ser 100 %, E, Q y F.");

                var stations = UnityEngine.Object.FindObjectsByType<BurgerStation>(FindObjectsSortMode.None);
                var ingredients = stations.Single(s => s.name == "Caja de ingredientes");
                var machine = UnityEngine.Object.FindObjectsByType<SlotMachineStation>(FindObjectsSortMode.None).First();
                var desk = UnityEngine.Object.FindFirstObjectByType<CashierDesk>();
                Require(ingredients.Prompt.StartsWith("E - Comprar"), "Letrero inicial de ingredientes: " + ingredients.Prompt);
                Require(machine.Prompt.StartsWith("E - Girar"), "Letrero inicial de la máquina: " + machine.Prompt);
                Require(desk.Prompt.Contains("E - ") && desk.Prompt.Contains("Q - "), "Letrero inicial de la cajera: " + desk.Prompt);

                Require(settings.TryRebind(GameAction.Interact, (int)KeyCode.G, out _), "No se pudo asignar G a interactuar.");
                Require(settings.TryRebind(GameAction.Secondary, (int)KeyCode.Space, out _), "No se pudo asignar Espacio a la acción secundaria.");
                Require(!settings.TryRebind(GameAction.Lights, (int)KeyCode.G, out _), "G repetida debía rechazarse.");
                Require(!settings.TryRebind(GameAction.Lights, (int)KeyCode.W, out _), "W (movimiento) debía rechazarse.");
                settings.SetScalePercent(150);
                Require(InputBindings.Interact == KeyCode.G && InputBindings.Secondary == KeyCode.Space && InputBindings.HudScale > 1.49f && InputBindings.HudScale < 1.51f,
                    "Las teclas elegidas no llegaron a InputBindings.");
                Require(ingredients.Prompt.StartsWith("G - Comprar"), "El letrero de ingredientes no refleja la tecla G: " + ingredients.Prompt);
                Require(machine.Prompt.StartsWith("G - Girar"), "El letrero de la máquina no refleja la tecla G: " + machine.Prompt);
                Require(desk.Prompt.Contains("G - ") && desk.Prompt.Contains("Espacio - "), "El letrero de la cajera no refleja G y Espacio: " + desk.Prompt);

                profile.MarkDirty();
                profile.Flush();
                profile.ReloadFromDisk(false);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);   // equivale a pulsar R
                reloadFrames = 0;
                EditorApplication.update += WaitForReload;
            }
            catch (Exception e) { Fail(e); }
        }

        private static void WaitForReload()
        {
            if (++reloadFrames < 6) return;
            EditorApplication.update -= WaitForReload;
            try
            {
                var settings = GameProfile.Current.Settings;
                Require(settings.InteractKey == (int)KeyCode.G && settings.SecondaryKey == (int)KeyCode.Space && settings.HudScalePercent == 150,
                    "Los ajustes no sobrevivieron a guardar y recargar: " + settings.InteractKey + "/" + settings.SecondaryKey + "/" + settings.HudScalePercent);
                var ingredients = UnityEngine.Object.FindObjectsByType<BurgerStation>(FindObjectsSortMode.None).Single(s => s.name == "Caja de ingredientes");
                Require(ingredients.Prompt.StartsWith("G - Comprar"), "Tras recargar el letrero perdió la tecla elegida: " + ingredients.Prompt);

                settings.ResetDefaults();
                Require(InputBindings.Interact == KeyCode.E && ingredients.Prompt.StartsWith("E - Comprar"), "Restablecer debía devolver E.");
                Debug.Log("TYCOON_SETTINGS_OK: las teclas y el tamaño del texto elegidos llegan a los letreros, se rechazan teclas repetidas o reservadas y sobreviven a guardar y recargar.");
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
            EditorApplication.update -= WaitForReload;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            GameProfile.DirectoryOverride = null;
            EditorSettings.enterPlayModeOptions = prevOptions;
            EditorSettings.enterPlayModeOptionsEnabled = prevOptionsEnabled;
            if (code == 0) EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(code);
        }
    }
}
