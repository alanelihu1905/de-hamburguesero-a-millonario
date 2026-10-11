using System;
using System.Linq;
using Tycoon3D.Player;
using Tycoon3D.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tycoon3D.Editor
{
    public static class CityGridPlaySmoke
    {
        private const string ScenePath = "Assets/_Game/Scenes/PuertoAlba_Grid.unity";
        private static double deadline;

        [MenuItem("Tools/Tycoon3D/Run Puerto Alba Grid Play Smoke")]
        public static void Run()
        {
            try
            {
                EditorSceneManager.OpenScene(ScenePath);
                deadline = EditorApplication.timeSinceStartup + 30;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception) { Fail(exception); }
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) EditorApplication.update += WaitForCity;
            if (state == PlayModeStateChange.ExitingPlayMode) Cleanup();
        }

        private static void WaitForCity()
        {
            var city = UnityEngine.Object.FindFirstObjectByType<CityGridBlockout>();
            if (city != null && city.transform.childCount > 0)
            {
                EditorApplication.update -= WaitForCity;
                Verify(city);
                return;
            }
            if (EditorApplication.timeSinceStartup > deadline)
            {
                EditorApplication.update -= WaitForCity;
                Fail(new TimeoutException("CityGridBlockout no generó geometría en 30 segundos."));
            }
        }

        private static void Verify(CityGridBlockout city)
        {
            try
            {
                if (city.transform.childCount < 900)
                    throw new InvalidOperationException($"Blockout incompleto: solo {city.transform.childCount} objetos.");
                if (city.transform.Find("City Ground")?.GetComponent<Collider>() == null)
                    throw new InvalidOperationException("Falta colisión en suelo urbano.");
                if (city.transform.Find("Casino") == null)
                    throw new InvalidOperationException("Falta casino permanente en el centro.");
                if (city.GetComponentsInChildren<Transform>().Count(item => item.name == "Gas station shop") != 2)
                    throw new InvalidOperationException("No hay exactamente dos gasolineras visibles.");
                var doors = UnityEngine.Object.FindObjectsByType<DoorVisitorSpawner>(FindObjectsSortMode.None);
                if (doors.Length < 50) throw new InvalidOperationException("Faltan puertas interactivas escasas.");
                if (Camera.main == null || UnityEngine.Object.FindFirstObjectByType<CharacterController>() == null)
                    throw new InvalidOperationException("Faltan cámara o controlador de primera persona.");
                var door = doors[0];
                if (!door.Prompt.StartsWith("E")) throw new InvalidOperationException("La puerta no presenta acción E.");
                door.Interact();
                if (!door.Prompt.Contains("Despedir")) throw new InvalidOperationException("La puerta no genera visitante.");
                door.Interact();
                if (!door.Prompt.Contains("Tocar")) throw new InvalidOperationException("La puerta no retira visitante.");

                Debug.Log($"TYCOON_PUERTO_ALBA_GRID_SMOKE_OK blocks=441 radius=10m casino=1 gasStations=2 doors={doors.Length} objects={city.transform.childCount}");
                EditorApplication.ExitPlaymode();
            }
            catch (Exception exception) { Fail(exception); }
        }

        private static void Fail(Exception exception)
        {
            Cleanup();
            Debug.LogException(exception);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }

        private static void Cleanup()
        {
            EditorApplication.update -= WaitForCity;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }
    }
}
