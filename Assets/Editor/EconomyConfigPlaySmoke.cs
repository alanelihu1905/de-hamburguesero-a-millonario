using System;
using System.IO;
using System.Linq;
using Tycoon3D.Casino;
using Tycoon3D.Core;
using Tycoon3D.Economy;
using Tycoon3D.Restaurant;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tycoon3D.Editor
{
    /// <summary>
    /// Prueba en Play Mode de los precios como datos: con un economy.json distinto la hamburguesería cobra y vende a otros
    /// precios, los letreros los muestran y la cajera cambia otro lote de fichas; con un archivo roto todo vuelve a los valores
    /// por defecto. Marcador: TYCOON_ECONOMY_CONFIG_OK (no lanzar con -quit).
    /// </summary>
    public static class EconomyConfigPlaySmoke
    {
        private const string ScenePath = "Assets/_Game/Scenes/BarrioResidencial.unity";
        private static bool prevOptionsEnabled;
        private static EnterPlayModeOptions prevOptions;
        private static string tempDir;

        public static void Run()
        {
            try
            {
                tempDir = Path.Combine(Path.GetTempPath(), "tycoon-smoke-economy-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);
                string json = Path.Combine(tempDir, "economy.json");
                File.WriteAllText(json, "{ \"version\": 1, \"ingredientCost\": 20, \"salePrice\": 80, \"chipLot\": 10 }");
                EconomyConfigSource.PathOverride = json;
                EconomyConfigSource.Reload();
                GameProfile.DirectoryOverride = Path.Combine(tempDir, "save");
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
            try
            {
                var wallet = UnityEngine.Object.FindFirstObjectByType<Wallet>();
                var loop = UnityEngine.Object.FindFirstObjectByType<BurgerLoop>();
                var casino = UnityEngine.Object.FindFirstObjectByType<CasinoService>();
                var stations = UnityEngine.Object.FindObjectsByType<BurgerStation>(FindObjectsSortMode.None);
                var desk = UnityEngine.Object.FindFirstObjectByType<CashierDesk>();
                Require(wallet != null && loop != null && casino != null && desk != null && stations.Length == 3, "Faltan componentes en la escena.");

                Require(loop.IngredientCost == 20 && loop.SalePrice == 80, "La hamburguesería no tomó los precios del archivo: " + loop.IngredientCost + "/" + loop.SalePrice);
                Require(casino.ChipLot == 10, "La cajera no tomó el lote del archivo: " + casino.ChipLot);
                var ingredients = stations.Single(s => s.name == "Caja de ingredientes");
                var counter = stations.Single(s => s.name == "Punto de venta");
                Require(ingredients.Prompt.Contains("$20"), "El letrero de ingredientes no muestra $20: " + ingredients.Prompt);

                ingredients.Interact();
                Require(wallet.Pesos == 80, "Los ingredientes debían costar $20; saldo " + wallet.Pesos);
                stations.Single(s => s.name == "Parrilla").Interact();
                Require(counter.Prompt.Contains("$80"), "El letrero de venta no muestra $80: " + counter.Prompt);
                counter.Interact();
                Require(wallet.Pesos == 160 && loop.Sales == 1, "La venta debía pagar $80; saldo " + wallet.Pesos);
                Require(GameProfile.Current.Ledger.Matches(wallet.Pesos, casino.Session.Chips), "El libro no cuadra con los precios nuevos.");

                desk.Interact();
                Require(casino.Session.Chips == 6 && wallet.Pesos == 10, "Con saldo $160 y lote 10 la cajera cambia 6 fichas ($150); fichas " + casino.Session.Chips + ", saldo " + wallet.Pesos);

                // un archivo roto no bloquea el juego: vuelven los valores por defecto
                File.WriteAllText(EconomyConfigSource.PathOverride, "{ esto no es json");
                EconomyConfigSource.Reload();
                Require(EconomyConfigSource.Current.SalePrice == 50 && EconomyConfigSource.Current.IngredientCost == 15 && EconomyConfigSource.Current.ChipLot == 20,
                    "Un archivo roto debía dar los valores por defecto.");
                loop.ApplyConfig(EconomyConfigSource.Current);
                casino.ApplyConfig(EconomyConfigSource.Current);
                Require(loop.SalePrice == 50 && casino.ChipLot == 20, "Los valores por defecto no se aplicaron.");

                Debug.Log("TYCOON_ECONOMY_CONFIG_OK: otros precios en el archivo cambian compra, venta, letreros y lote de la cajera; un archivo roto vuelve a los valores por defecto.");
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
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EconomyConfigSource.PathOverride = null;
            EconomyConfigSource.Reload();
            GameProfile.DirectoryOverride = null;
            EditorSettings.enterPlayModeOptions = prevOptions;
            EditorSettings.enterPlayModeOptionsEnabled = prevOptionsEnabled;
            if (code == 0) EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(code);
        }
    }
}
