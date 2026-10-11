using System.IO;
using Tycoon3D.Player;
using Tycoon3D.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tycoon3D.Editor
{
    public static class CityGridSceneBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/PuertoAlba_Grid.unity";

        [MenuItem("Tools/Tycoon3D/Create Puerto Alba 21x21 Grid Scene")]
        public static void CreateScene()
        {
            if (File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Puerto Alba", "La escena ya existe. No se sobrescribió.", "OK");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var city = new GameObject("Puerto Alba - 21 x 21 blocks");
            city.AddComponent<CityGridBlockout>();

            var player = new GameObject("City Grid Player");
            player.transform.position = new Vector3(0, 1, -120);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = .35f;
            player.AddComponent<FirstPersonController>();
            player.AddComponent<Tycoon3D.Interaction.PlayerInteractor>();
            var cameraObject = new GameObject("City Grid Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform);
            cameraObject.transform.localPosition = new Vector3(0, .65f, 0);
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();

            var sun = new GameObject("Puerto Alba Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.15f;
            sun.transform.rotation = Quaternion.Euler(35, -35, 0);
            RenderSettings.ambientLight = new Color(.54f, .57f, .62f);

            Directory.CreateDirectory("Assets/_Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Puerto Alba grid scene saved: 441 blocks, radius 10 blocks, 1,776 m square. Open it and press Play to generate blockout.");
        }
    }
}
