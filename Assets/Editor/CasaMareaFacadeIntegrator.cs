using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tycoon3D.Editor
{
    public static class CasaMareaFacadeIntegrator
    {
        private const string ScenePath = "Assets/_Game/Scenes/BarrioResidencial.unity";
        private const string ModelPath = "Assets/_Game/Models/CasaMarea/";
        private const string GroupName = "Arte Casa Marea - fachada";

        [MenuItem("Tools/Tycoon3D/Integrar fachada Casa Marea")]
        public static void Integrate()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var old = GameObject.Find(GroupName);
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            var group = new GameObject(GroupName).transform;
            AddModel("SM_Fachada_CasaMarea.fbx", group);
            AddModel("SM_Despacho_Este.fbx", group);

            HidePlaceholder("Frente izquierda");
            HidePlaceholder("Frente derecha");
            HidePlaceholder("Dintel puerta");
            HidePlaceholder("Techo");
            HidePlaceholder("Letrero provisional");
            HidePlaceholder("Pared derecha");
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("No se pudo guardar la fachada.");
            AssetDatabase.SaveAssets();
            Debug.Log("CASAMAREA_FACADE_OK: fachada y despacho FBX integrados.");
        }

        private static void AddModel(string fileName, Transform parent)
        {
            var prefab = CasaMareaPalette.Model(fileName.Replace(".fbx", ""));
            if (prefab == null) throw new InvalidOperationException("Falta FBX: " + fileName);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            model.name = fileName.Replace(".fbx", "");
            model.transform.SetParent(parent, false);
            model.transform.localPosition = Vector3.zero;
            // This FBX imports Y up and mirrors X/Z; turn it toward the street, restoring east at +X.
            model.transform.localScale = Vector3.one;
            model.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var renderers = model.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("FBX sin mallas: " + fileName);
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
                var sources = renderer.sharedMaterials;
                var replacements = new Material[sources.Length];
                for (int i = 0; i < sources.Length; i++) replacements[i] = CasaMareaPalette.Material(sources[i]);
                renderer.sharedMaterials = replacements;
            }
            Debug.Log(model.name + ": tamaño Unity " + bounds.size + ", centro " + bounds.center);
        }

        private static void HidePlaceholder(string name)
        {
            var obj = GameObject.Find(name);
            if (obj == null) throw new InvalidOperationException("Falta provisional: " + name);
            var renderer = obj.GetComponent<MeshRenderer>();
            if (renderer == null) throw new InvalidOperationException("Falta renderer: " + name);
            renderer.enabled = false;
        }
    }
}
