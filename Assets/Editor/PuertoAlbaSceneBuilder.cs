using System;
using Tycoon3D.City;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tycoon3D.Editor
{
    /// <summary>
    /// Crea Assets/_Game/Scenes/PuertoAlba.unity: fase 1 de Puerto Alba (Casa Marea, calle y Mercado del Sol como suelo
    /// provisional). No cambia BarrioResidencial ni la lista de escenas de compilación.
    /// </summary>
    public static class PuertoAlbaSceneBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/PuertoAlba.unity";

        [MenuItem("Tools/Tycoon3D/Crear escena Puerto Alba (fase 1)")]
        public static void Build()
        {
            var problems = CityLayout.Validate();
            if (problems.Count > 0) throw new InvalidOperationException("Geometría inválida: " + string.Join(" | ", problems));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var ground = Mat("Puerto Alba suelo", new Color32(184, 167, 140, 255));
            var streetMat = Mat("Puerto Alba calle", new Color32(120, 110, 100, 255));
            var casaMat = Mat("Puerto Alba Casa Marea", new Color32(232, 196, 160, 255));
            var marketMat = Mat("Puerto Alba Mercado", new Color32(43, 166, 160, 255));

            var root = new GameObject("Puerto Alba - fase 1 (provisional)").transform;
            Slab("Suelo general", new Vector3(0, -.1f, 0), new Vector3(80, .2f, 40), ground, root);
            foreach (var box in CityLayout.Built)
            {
                var mat = box.Zone == ZoneId.CasaMarea ? casaMat : marketMat;
                Slab("Zona " + CityMap.Get(box.Zone).Name, new Vector3(box.CenterX, .01f, box.CenterZ),
                    new Vector3(box.MaxX - box.MinX, .02f, box.MaxZ - box.MinZ), mat, root);
            }
            var street = CityLayout.Street;
            Slab("Calle", new Vector3(street.CenterX, .02f, street.CenterZ), new Vector3(street.MaxX - street.MinX, .03f, street.MaxZ - street.MinZ), streetMat, root);

            var player = new GameObject("Jugador");
            player.transform.position = new Vector3(0, 1f, 0);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = .35f;
            controller.center = Vector3.zero;
            player.AddComponent<Tycoon3D.Player.FirstPersonController>();
            var tracker = player.AddComponent<CityZoneTracker>();
            tracker.Configure(player.transform);

            var hud = new GameObject("Puerto Alba HUD").AddComponent<CityHud>();
            hud.Configure(tracker, player.GetComponent<Tycoon3D.Player.FirstPersonController>());

            var cam = new GameObject("Camara primera persona");
            cam.tag = "MainCamera";
            cam.transform.SetParent(player.transform, false);
            cam.transform.localPosition = new Vector3(0, .65f, 0);
            cam.AddComponent<Camera>();
            cam.AddComponent<AudioListener>();

            var sun = new GameObject("Sol");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            sun.transform.rotation = Quaternion.Euler(30, 40, 0);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("No se pudo guardar " + ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("PUERTO_ALBA_SCENE_OK: " + ScenePath);
            // En batch, cierra Unity al terminar; sin esto el proceso se queda abierto tras guardar.
            if (Application.isBatchMode) EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }

        private static void Slab(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Material Mat(string name, Color32 color)
        {
            const string folder = "Assets/_Game/Materials/PuertoAlba";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/_Game/Materials", "PuertoAlba");
            string path = folder + "/" + name.Replace(' ', '_') + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("Falta URP/Lit.");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
