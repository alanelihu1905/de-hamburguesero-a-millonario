using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tycoon3D.Editor
{
    public static class CasaMareaNeighborhoodIntegrator
    {
        private const string ScenePath = "Assets/_Game/Scenes/BarrioResidencial.unity";
        private const string ModelPath = "Assets/_Game/Models/CasaMarea/";
        private const string GroupName = "Arte Casa Marea - barrio";

        [MenuItem("Tools/Tycoon3D/Integrar barrio Casa Marea")]
        public static void Integrate()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var previous = GameObject.Find(GroupName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous);
            var group = new GameObject(GroupName).transform;

            Add("SM_Casa_Vecina_A", new Vector3(-12, 0, 1), 0, group);
            Add("SM_Casa_Vecina_B", new Vector3(12, 0, 1), 0, group);
            Hide("Casa vecina izquierda");
            Hide("Casa vecina derecha");

            Add("SM_Cliente_Pescador", new Vector3(6.1f, 0, 1.75f), -90, group);
            Add("SM_Cliente_Vecina", new Vector3(6.1f, 0, .4f), -90, group);
            Add("SM_Cliente_Joven", new Vector3(6.1f, 0, -.9f), -90, group);

            Add("SM_Banco_Calle", new Vector3(-6.5f, 0, -5.4f), 0, group);
            Add("SM_Maceta_Tropical", new Vector3(-4.6f, 0, -2.7f), 0, group);
            Add("SM_Farol_Calle", new Vector3(-8.1f, 0, -3.3f), 0, group);
            Add("SM_Cartel_Precios", new Vector3(4.6f, 0, -3.1f), 0, group);
            Add("SM_Banco_Callejon", new Vector3(7.8f, 0, -2.4f), -90, group);
            Add("SM_Caja_Pescado", new Vector3(8.1f, 0, 2.6f), 0, group);
            Add("SM_Cajas_Pescado_Pila", new Vector3(-7.5f, 0, -1.9f), 0, group);
            Add("SM_Bolardo", new Vector3(8.6f, 0, -4.4f), 0, group);
            Add("SM_Barril", new Vector3(-8.3f, 0, -.6f), 0, group);
            Add("SM_Maceta_Patio", new Vector3(4.3f, 0, -1.25f), 0, group);
            Add("SM_Cubeta", new Vector3(-3.7f, 0, 2.8f), 0, group);

            Add("SM_Refrigerador", new Vector3(-4.05f, 0, 3.35f), 0, group);
            Add("SM_Tabla_Corte", new Vector3(-2.1f, .86f, 2.15f), 0, group);
            Add("SM_Hamburguesa_Piezas", new Vector3(-1.1f, .86f, 2.15f), 0, group);
            Add("SM_Hamburguesa_Completa", new Vector3(2.1f, .86f, 2.15f), 0, group);
            Add("SM_Repisa_Frascos", new Vector3(-3.3f, 1.85f, 3.75f), 0, group);
            for (int i = 0; i < 8; i++)
            Add("SM_Mostrador_Modulo", new Vector3(-3.5f + i, 0, 2.2f), 0, group);
            Hide("Mostrador cocina");

            var sun = GameObject.Find("Sol");
            if (sun != null) sun.GetComponent<Light>().intensity = 1.0f;

            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("No se pudo guardar el barrio.");
            AssetDatabase.SaveAssets();
            Debug.Log("CASAMAREA_NEIGHBORHOOD_OK: casas, clientes, cocina y props integrados.");
        }

        private static void Add(string name, Vector3 position, float facing, Transform parent)
        {
            var prefab = CasaMareaPalette.Model(name);
            if (prefab == null) throw new InvalidOperationException("Falta FBX: " + name);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            model.name = name;
            model.transform.SetParent(parent, false);
            model.transform.position = position;
            // These batches export Blender Z-up coordinates, as the station batch does.
            model.transform.rotation = Quaternion.Euler(0, 180 + facing, 0) * Quaternion.Euler(-90, 0, 0);
            var renderers = model.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("FBX sin mallas: " + name);
            foreach (var renderer in renderers)
            {
                var sources = renderer.sharedMaterials;
                var replacements = new Material[sources.Length];
                for (int i = 0; i < sources.Length; i++) replacements[i] = CasaMareaPalette.Material(sources[i]);
                renderer.sharedMaterials = replacements;
            }
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            model.transform.position += Vector3.up * (position.y - bounds.min.y);
            Debug.Log(name + " tamaño " + bounds.size);
        }

        private static void Hide(string name)
        {
            var placeholder = GameObject.Find(name);
            if (placeholder == null) throw new InvalidOperationException("Falta provisional: " + name);
            var renderer = placeholder.GetComponent<MeshRenderer>();
            if (renderer == null) throw new InvalidOperationException("Sin renderer: " + name);
            renderer.enabled = false;
        }
    }
}
