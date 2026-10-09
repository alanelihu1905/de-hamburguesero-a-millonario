using System;
using Tycoon3D.Economy;
using Tycoon3D.Interaction;
using Tycoon3D.Player;
using Tycoon3D.Restaurant;
using Tycoon3D.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tycoon3D.Editor
{
    /// <summary>Creates the first playable scene once, then leaves authored scene changes alone.</summary>
    [InitializeOnLoad]
    public static class PrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/BarrioResidencial.unity";
        private const string MaterialsPath = "Assets/_Game/Materials";

        static PrototypeSceneBuilder()
        {
            EditorApplication.delayCall += BuildIfMissing;
        }

        private static void BuildIfMissing()
        {
            if (Application.isBatchMode || System.IO.File.Exists(ScenePath)) return;
            Build();
        }

        [MenuItem("Tools/Tycoon3D/Crear escena prototipo si falta")]
        public static void Build()
        {
            if (System.IO.File.Exists(ScenePath))
            {
                Debug.Log("Tycoon3D: la escena ya existe; no se sobrescribió.");
                return;
            }

            EnsureFolder("Assets/_Game/Scenes");
            EnsureFolder(MaterialsPath);
            ConfigureUrp();
            PlayerSettings.companyName = "Tycoon3D";
            PlayerSettings.productName = "De Hamburguesero a Millonario";
            EditorSettings.serializationMode = SerializationMode.ForceText;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Material ground = MakeMaterial("Suelo", new Color32(184, 167, 140, 255));
            Material wall = MakeMaterial("Pared", new Color32(232, 196, 160, 255));
            Material roof = MakeMaterial("Tejado", new Color32(138, 90, 60, 255));
            Material wood = MakeMaterial("Madera", new Color32(138, 90, 60, 255));
            Material steel = MakeMaterial("Metal", new Color(.26f, .31f, .32f));
            Material accent = MakeMaterial("Acento", new Color32(228, 87, 46, 255));
            Material turquoise = MakeMaterial("Turquesa", new Color32(43, 166, 160, 255));
            Material green = MakeMaterial("Vegetacion", new Color(.27f, .48f, .25f));

            var environment = new GameObject("Barrio provisional - reemplazar tras arte de Claude").transform;
            Cube("Calle peatonal", new Vector3(0, -.22f, -6), new Vector3(18, .3f, 10), ground, environment);
            Cube("Piso casa", new Vector3(0, -.1f, 1), new Vector3(10, .2f, 6), wood, environment);
            Cube("Pared trasera", new Vector3(0, 1.5f, 4), new Vector3(10, 3, .2f), wall, environment);
            Cube("Pared izquierda", new Vector3(-5, 1.5f, 1), new Vector3(.2f, 3, 6), wall, environment);
            Cube("Pared derecha", new Vector3(5, 1.5f, 1), new Vector3(.2f, 3, 6), wall, environment);
            Cube("Frente izquierda", new Vector3(-3, 1.5f, -2), new Vector3(4, 3, .2f), wall, environment);
            Cube("Frente derecha", new Vector3(3, 1.5f, -2), new Vector3(4, 3, .2f), wall, environment);
            Cube("Dintel puerta", new Vector3(0, 2.65f, -2), new Vector3(2, .7f, .2f), wall, environment);
            Cube("Techo", new Vector3(0, 3.18f, 1), new Vector3(10.4f, .25f, 6.4f), roof, environment);
            Cube("Mostrador cocina", new Vector3(0, .43f, 2.15f), new Vector3(8.5f, .85f, .75f), wood, environment);
            Cube("Letrero provisional", new Vector3(0, 2.4f, -2.15f), new Vector3(3.4f, .55f, .08f), turquoise, environment);
            Cube("Casa vecina izquierda", new Vector3(-12, 1.4f, 1), new Vector3(6, 2.8f, 6), wall, environment);
            Cube("Casa vecina derecha", new Vector3(12, 1.4f, 1), new Vector3(6, 2.8f, 6), wall, environment);
            Cube("Arbol tronco", new Vector3(-8, 1, -5), new Vector3(.35f, 2, .35f), wood, environment);
            var leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves.name = "Arbol copa";
            leaves.transform.SetParent(environment);
            leaves.transform.position = new Vector3(-8, 2.4f, -5);
            leaves.transform.localScale = new Vector3(2.3f, 2.1f, 2.3f);
            leaves.GetComponent<Renderer>().sharedMaterial = green;

            GameObject session = new GameObject("Sesion local - economia y ventas");
            Wallet wallet = session.AddComponent<Wallet>();
            BurgerLoop loop = session.AddComponent<BurgerLoop>();

            Station("Caja de ingredientes", new Vector3(-3.3f, 1.025f, 1.75f),
                new Vector3(.9f, .35f, .55f), accent, loop, StationKind.Ingredients);
            Station("Parrilla", new Vector3(0, 1.025f, 1.75f),
                new Vector3(1.1f, .35f, .6f), steel, loop, StationKind.Grill);
            Station("Punto de venta", new Vector3(3.3f, 1.025f, 1.75f),
                new Vector3(.9f, .35f, .55f), turquoise, loop, StationKind.Counter);

            GameObject player = new GameObject("Jugador local");
            player.transform.position = new Vector3(0, 1, -8.5f);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = .35f;
            controller.center = Vector3.zero;
            player.AddComponent<FirstPersonController>();
            PlayerInteractor interactor = player.AddComponent<PlayerInteractor>();
            GameObject cameraObject = new GameObject("Camara primera persona");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform);
            cameraObject.transform.localPosition = new Vector3(0, .65f, 0);
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();

            GameObject hud = new GameObject("HUD provisional");
            hud.AddComponent<PrototypeHud>().Configure(wallet, loop, interactor);

            GameObject sun = new GameObject("Sol");
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.6f;
            light.color = new Color32(255, 196, 126, 255);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = .85f;
            sun.transform.rotation = Quaternion.Euler(30, 40, 0);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color32(143, 199, 206, 255);
            RenderSettings.ambientEquatorColor = new Color32(154, 127, 158, 255);
            RenderSettings.ambientGroundColor = new Color32(59, 47, 74, 255);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color32(242, 214, 184, 255);
            RenderSettings.fogDensity = .006f;
            GameObject workLight = new GameObject("Foco mostrador");
            workLight.transform.position = new Vector3(0, 2.55f, 1.2f);
            Light pointLight = workLight.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = new Color32(244, 201, 93, 255);
            pointLight.intensity = 1.5f;
            pointLight.range = 5.5f;

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("No se pudo guardar " + ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Tycoon3D: escena creada en " + ScenePath + ". Presiona Play para probar.");
        }

        [MenuItem("Tools/Tycoon3D/Validar prototipo")]
        public static void Validate()
        {
            if (!System.IO.File.Exists(ScenePath)) { Debug.LogError("Falta la escena prototipo."); return; }
            if (EditorBuildSettings.scenes.Length != 1 || EditorBuildSettings.scenes[0].path != ScenePath)
                Debug.LogError("La escena no está configurada en Build Settings.");
            else Debug.Log("Tycoon3D: escena registrada. La prueba de juego requiere Play Mode.");
        }

        private static void Station(string name, Vector3 position, Vector3 size, Material material,
            BurgerLoop loop, StationKind kind)
        {
            GameObject station = Cube(name, position, size, material, null);
            station.AddComponent<BurgerStation>().Configure(loop, kind);
        }

        private static GameObject Cube(string name, Vector3 position, Vector3 size, Material material, Transform parent)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            if (parent != null) cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = size;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static Material MakeMaterial(string name, Color color)
        {
            string path = MaterialsPath + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("No hay shader compatible disponible.");
            material = new Material(shader) { name = name, color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void ConfigureUrp()
        {
            const string path = "Assets/_Game/Settings/PrototypeURP.asset";
            if (GraphicsSettings.defaultRenderPipeline != null) return;
            EnsureFolder("Assets/_Game/Settings");
            Type pipelineType = Type.GetType("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset, Unity.RenderPipelines.Universal.Runtime");
            Type rendererType = Type.GetType("UnityEngine.Rendering.Universal.UniversalRendererData, Unity.RenderPipelines.Universal.Runtime");
            if (pipelineType == null || rendererType == null)
            {
                Debug.LogWarning("URP aún no está instalado. Revisa Packages/manifest.json y vuelve a abrir Unity.");
                return;
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(path);
            if (pipeline == null)
            {
                var asset = ScriptableObject.CreateInstance(pipelineType);
                asset.name = "Prototype URP";
                AssetDatabase.CreateAsset(asset, path);
                var renderer = ScriptableObject.CreateInstance(rendererType);
                renderer.name = "Prototype Universal Renderer";
                AssetDatabase.AddObjectToAsset(renderer, asset);
                var serialized = new SerializedObject(asset);
                SerializedProperty renderers = serialized.FindProperty("m_RendererDataList");
                if (renderers == null) throw new InvalidOperationException("No se encontró m_RendererDataList en URP.");
                renderers.arraySize = 1;
                renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                pipeline = asset as RenderPipelineAsset;
                AssetDatabase.SaveAssets();
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = null;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
