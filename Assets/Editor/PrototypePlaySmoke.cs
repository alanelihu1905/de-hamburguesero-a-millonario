using System;
using System.Linq;
using Tycoon3D.Economy;
using Tycoon3D.Player;
using Tycoon3D.Restaurant;
using Tycoon3D.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tycoon3D.Editor
{
    public static class PrototypePlaySmoke
    {
        private const string ScenePath = "Assets/_Game/Scenes/BarrioResidencial.unity";
        private static bool previousEnterPlayModeOptionsEnabled;
        private static EnterPlayModeOptions previousEnterPlayModeOptions;
        private static bool hudOnly;

        [MenuItem("Tools/Tycoon3D/Run Prototype Play Smoke")]
        public static void Run() => Start(false);

        [MenuItem("Tools/Tycoon3D/Run HUD Play Smoke")]
        public static void RunHud() => Start(true);

        private static void Start(bool onlyHud)
        {
            try
            {
                hudOnly = onlyHud;
                Tycoon3D.Core.GameProfile.DirectoryOverride = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tycoon-smoke-burger-" + System.Guid.NewGuid().ToString("N"));   // perfil limpio, aparte del real
                EditorSceneManager.OpenScene(ScenePath);
                previousEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
                previousEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
                EditorSettings.enterPlayModeOptionsEnabled = true;
                EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            try
            {
                var wallet = UnityEngine.Object.FindFirstObjectByType<Wallet>();
                var loop = UnityEngine.Object.FindFirstObjectByType<BurgerLoop>();
                var stations = UnityEngine.Object.FindObjectsByType<BurgerStation>(FindObjectsSortMode.None);
                if (wallet == null || loop == null || stations.Length != 3)
                    throw new InvalidOperationException("Faltan componentes de economía o estaciones en la escena.");
                var playerController = UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();
                var playerHud = UnityEngine.Object.FindFirstObjectByType<PrototypeHud>();
                if (Camera.main == null || UnityEngine.Object.FindFirstObjectByType<CharacterController>() == null ||
                    playerController == null || playerController.GetComponent<PlayerStatus>() == null || playerHud == null)
                    throw new InvalidOperationException("Faltan cámara, estado de jugador, controlador o HUD.");

                if (hudOnly)
                {
                    var status = playerController.GetComponent<PlayerStatus>();
                    status.TakeDamage(25);
                    if (status.Health != 75) throw new InvalidOperationException("El daño no actualizó la barra de salud.");
                    status.Heal(10);
                    if (status.Health != 85) throw new InvalidOperationException("La curación no actualizó la barra de salud.");
                    status.AddContraband(2, 40);
                    if (status.ContrabandPackages != 2 || status.Heat != 40)
                        throw new InvalidOperationException("El inventario del teléfono no refleja la carga y atención.");
                    status.RemoveContraband(1);
                    status.SetHeat(120);
                    if (status.ContrabandPackages != 1 || status.Heat != 100)
                        throw new InvalidOperationException("Los límites de carga/atención fallaron.");
                    Debug.Log("TYCOON_HUD_SMOKE_OK: HUD, teléfono, salud, carga y atención.");
                    EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                    RestoreEditorSettings();
                    EditorApplication.ExitPlaymode();
                    if (Application.isBatchMode)
                        EditorApplication.delayCall += () => EditorApplication.Exit(0);
                    return;
                }

                var ingredients = stations.Single(s => s.name == "Caja de ingredientes");
                var grill = stations.Single(s => s.name == "Parrilla");
                var counter = stations.Single(s => s.name == "Punto de venta");
                var art = GameObject.Find("Arte Casa Marea - estaciones");
                if (art == null || art.transform.childCount != 3)
                    throw new InvalidOperationException("Faltan los tres modelos Casa Marea.");
                float[] expectedHeights = { .5f, .75f, .8f };
                for (int i = 0; i < expectedHeights.Length; i++)
                {
                    var renderer = art.transform.GetChild(i).GetComponentInChildren<MeshRenderer>();
                    if (renderer == null || Mathf.Abs(renderer.bounds.size.y - expectedHeights[i]) > .02f)
                        throw new InvalidOperationException("Modelo Casa Marea mal orientado o escalado.");
                    if (renderer.sharedMaterial == null ||
                        renderer.sharedMaterial.shader.name != "Universal Render Pipeline/Lit")
                        throw new InvalidOperationException("Modelo Casa Marea sin material URP.");
                }
                foreach (BurgerStation station in stations)
                {
                    Camera.main.transform.position = station.transform.position + new Vector3(0, .7f, -1.5f);
                    Camera.main.transform.LookAt(station.transform.position);
                    Physics.SyncTransforms();
                    if (!Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward,
                            out RaycastHit hit, 3f) || hit.collider.GetComponentInParent<BurgerStation>() != station)
                        throw new InvalidOperationException("El raycast no alcanza la estación: " + station.name);
                }
                if (wallet.Pesos != 100 || loop.Sales != 0)
                    throw new InvalidOperationException("El estado inicial no es el esperado.");
                if (!ingredients.Prompt.StartsWith("E -") || grill.Prompt.StartsWith("E -") ||
                    counter.Prompt.StartsWith("E -"))
                    throw new InvalidOperationException("Prompts iniciales incorrectos.");
                ingredients.Interact();
                if (ingredients.Prompt.StartsWith("E -") || !grill.Prompt.StartsWith("E -"))
                    throw new InvalidOperationException("Prompts tras compra incorrectos.");
                grill.Interact();
                if (!counter.Prompt.StartsWith("E -"))
                    throw new InvalidOperationException("Prompt de venta incorrecto.");
                counter.Interact();
                if (wallet.Pesos != 135 || loop.Sales != 1 || loop.Step != BurgerStep.NeedIngredients)
                    throw new InvalidOperationException("La compra, cocción y venta no completaron el ciclo.");
                if (!ingredients.Prompt.StartsWith("E -"))
                    throw new InvalidOperationException("Prompt tras venta incorrecto.");

                Debug.Log("TYCOON_SMOKE_OK: Play Mode, tres modelos URP, raycast y prompts, venta y saldo $135.");
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                RestoreEditorSettings();
                EditorApplication.ExitPlaymode();
                if (Application.isBatchMode)
                    EditorApplication.delayCall += () => EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private static void Fail(Exception exception)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            RestoreEditorSettings();
            Debug.LogException(exception);
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
        }

        private static void RestoreEditorSettings()
        {
            EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = previousEnterPlayModeOptionsEnabled;
        }
    }
}
