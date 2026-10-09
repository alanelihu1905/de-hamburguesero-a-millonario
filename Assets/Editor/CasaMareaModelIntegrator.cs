using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tycoon3D.Editor
{
    public static class CasaMareaModelIntegrator
    {
        private const string ScenePath = "Assets/_Game/Scenes/BarrioResidencial.unity";
        private const string ModelPath = "Assets/_Game/Models/CasaMarea/";
        private const string MaterialPath = "Assets/_Game/Materials/CasaMarea";
        private const string ArtGroupName = "Arte Casa Marea - estaciones";

        [MenuItem("Tools/Tycoon3D/Integrar estaciones Casa Marea")]
        public static void Integrate()
        {
            if (!System.IO.File.Exists(ScenePath)) throw new InvalidOperationException("Falta la escena Casa Marea.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var existingArt = GameObject.Find(ArtGroupName);
            if (existingArt != null) UnityEngine.Object.DestroyImmediate(existingArt);
            var artGroup = new GameObject(ArtGroupName).transform;
            Install("Caja de ingredientes", "SM_Huacal_Ingredientes.fbx", artGroup);
            Install("Parrilla", "SM_Plancha_Tubo.fbx", artGroup);
            Install("Punto de venta", "SM_PuntoVenta_Feston.fbx", artGroup);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("No se pudo guardar la escena.");
            AssetDatabase.SaveAssets();
            Debug.Log("CASAMAREA_MODELS_OK: tres estaciones FBX integradas en la escena.");
        }

        private static void Install(string stationName, string fileName, Transform artGroup)
        {
            var station = GameObject.Find(stationName);
            if (station == null) throw new InvalidOperationException("Falta estación: " + stationName);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath + fileName);
            if (prefab == null) throw new InvalidOperationException("Falta FBX importado: " + fileName);

            var old = station.transform.Find("Modelo Casa Marea");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            model.name = "Modelo Casa Marea - " + stationName;
            model.transform.SetParent(artGroup, false);
            model.transform.position = station.transform.position;
            // Blender exports Z up and front toward -Y; Unity needs Y up and front toward -Z.
            model.transform.rotation = Quaternion.Euler(0, 180, 0) * Quaternion.Euler(-90, 0, 0);
            model.transform.localScale = Vector3.one;

            var renderers = model.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("FBX sin mallas: " + fileName);
            foreach (var renderer in renderers)
            {
                var sources = renderer.sharedMaterials;
                var replacements = new Material[sources.Length];
                for (int i = 0; i < sources.Length; i++) replacements[i] = UrpMaterial(sources[i]);
                renderer.sharedMaterials = replacements;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            Vector3 target = new Vector3(station.transform.position.x, 0.855f, station.transform.position.z);
            model.transform.position += new Vector3(target.x - bounds.center.x, target.y - bounds.min.y, target.z - bounds.center.z);
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            var collider = station.GetComponent<BoxCollider>();
            if (collider == null) throw new InvalidOperationException("Estación sin collider: " + stationName);
            collider.center = station.transform.InverseTransformPoint(bounds.center);
            Vector3 scale = station.transform.lossyScale;
            collider.size = new Vector3(bounds.size.x / scale.x, bounds.size.y / scale.y, bounds.size.z / scale.z);
            var placeholder = station.GetComponent<MeshRenderer>();
            if (placeholder != null) placeholder.enabled = false;
            Debug.Log(stationName + ": malla " + bounds.size + ", collider alineado.");
        }

        internal static Material UrpMaterial(Material source)
        {
            if (source == null) throw new InvalidOperationException("Material FBX vacío.");
            if (!AssetDatabase.IsValidFolder(MaterialPath)) AssetDatabase.CreateFolder("Assets/_Game/Materials", "CasaMarea");
            string cleanName = source.name.Replace(" (Instance)", "").Replace(':', '_');
            string path = MaterialPath + "/" + cleanName + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Falta shader URP/Lit.");
            material = new Material(shader) { name = cleanName, color = source.color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
