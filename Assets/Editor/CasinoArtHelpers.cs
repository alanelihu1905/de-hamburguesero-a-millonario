using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tycoon3D.Editor
{
    /// <summary>Ayudantes de arte para el Casino FOX: materiales URP, texturas, modelos externos CC0 y partículas.</summary>
    internal static class CasinoArt
    {
        public const string MaterialRoot = "Assets/_Game/Materials/Casino";
        public const string TextureRoot = "Assets/_Game/Art/Casino/Textures";
        public const string AudioRoot = "Assets/_Game/Art/Casino/Audio";
        public const string ExternalRoot = "Assets/_Game/Models/External";

        public static readonly List<string> ExternalUsed = new List<string>();
        private static readonly Dictionary<string, Material> externalMaterials = new Dictionary<string, Material>();

        public static Texture2D Texture(string file, bool repeat, bool mips, bool uncompressed)
        {
            string path = TextureRoot + "/" + file;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
            }
            if (importer == null) throw new InvalidOperationException("Falta la textura " + path + ". Ejecuta generar_assets.py.");
            importer.sRGBTexture = true;
            importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.mipmapEnabled = mips;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = uncompressed ? TextureImporterCompression.Uncompressed : TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static AudioClip Clip(string file)
        {
            string path = AudioRoot + "/" + file + ".wav";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
            if (clip == null) throw new InvalidOperationException("Falta el sonido " + path + ". Ejecuta generar_assets.py.");
            return clip;
        }

        public static Material Mat(string name, Color32 color, float smoothness, Color32? emission = null, float intensity = 0f,
            Texture2D baseMap = null, Vector2? tiling = null, Texture2D emissionMap = null)
        {
            if (!AssetDatabase.IsValidFolder(MaterialRoot)) AssetDatabase.CreateFolder("Assets/_Game/Materials", "Casino");
            string path = MaterialRoot + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("Falta el shader URP/Lit.");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.color = color;
            material.SetFloat("_Smoothness", smoothness);
            if (baseMap != null)
            {
                material.SetTexture("_BaseMap", baseMap);
                material.mainTexture = baseMap;
                material.SetTextureScale("_BaseMap", tiling ?? Vector2.one);
            }
            if (emission.HasValue || emissionMap != null)
            {
                material.EnableKeyword("_EMISSION");
                Color e = emission.HasValue ? (Color)emission.Value * intensity : Color.white * intensity;
                material.SetColor("_EmissionColor", e);
                if (emissionMap != null) material.SetTexture("_EmissionMap", emissionMap);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        // ------------------------------------------------------------------ modelos externos (Kenney, CC0)
        private static Material ExternalMat(Material source, string kit)
        {
            string key = kit + "/" + (source != null ? source.name : "?");
            if (externalMaterials.TryGetValue(key, out var cached)) return cached;
            string clean = (kit + "_" + (source != null ? source.name : "mat")).Replace(" (Instance)", "").Replace(' ', '_');
            string path = MaterialRoot + "/" + clean + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = clean };
                AssetDatabase.CreateAsset(material, path);
            }
            Color color = source != null ? source.color : Color.white;
            Texture tex = source != null ? source.mainTexture : null;
            if (tex == null && kit == "Kenney_FoodKit")
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ExternalRoot + "/Kenney_FoodKit/Textures/colormap.png");
            material.SetColor("_BaseColor", tex != null ? Color.white : color);
            if (tex != null) material.SetTexture("_BaseMap", tex);
            material.SetFloat("_Smoothness", .25f);
            EditorUtility.SetDirty(material);
            externalMaterials[key] = material;
            return material;
        }

        /// <summary>
        /// Instancia un FBX externo, elige la orientación que lo deja de pie (o plano, si es alfombra/plato), lo escala a la altura
        /// o al ancho deseado, lo apoya en <paramref name="surfaceY"/> y le da materiales URP. Devuelve la cima del modelo.
        /// </summary>
        public static GameObject Place(string kit, string model, Transform parent, Vector3 position, float yawDegrees,
            float targetSize, bool bySize_Width, bool flat, out float top, float surfaceY = 0f, bool decorOnly = false)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExternalRoot + "/" + kit + "/" + model + ".fbx");
            if (prefab == null) throw new InvalidOperationException("Falta el modelo externo " + kit + "/" + model);
            var root = new GameObject(model);
            root.transform.SetParent(parent, false);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.SetParent(root.transform, false);
            foreach (var r in inst.GetComponentsInChildren<Renderer>())
            {
                var src = r.sharedMaterials;
                var dst = new Material[src.Length];
                for (int i = 0; i < src.Length; i++) dst[i] = ExternalMat(src[i], kit);
                r.sharedMaterials = dst;
            }
            // orientación: probar tres giros y quedarse con el que mejor encaja (alto frente a ancho)
            Quaternion[] options = { Quaternion.identity, Quaternion.Euler(-90, 0, 0), Quaternion.Euler(90, 0, 0) };
            float bestScore = float.MinValue;
            Quaternion best = Quaternion.identity;
            foreach (var q in options)
            {
                inst.transform.localRotation = q;
                Bounds b = Measure(inst);
                float tall = b.size.y / Mathf.Max(.0001f, Mathf.Max(b.size.x, b.size.z));
                float score = flat ? -tall : tall;
                if (score > bestScore) { bestScore = score; best = q; }
            }
            inst.transform.localRotation = best;
            Bounds m = Measure(inst);
            float current = bySize_Width ? Mathf.Max(m.size.x, m.size.z) : m.size.y;
            float scale = targetSize / Mathf.Max(.0001f, current);
            inst.transform.localScale = Vector3.one * scale;
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0, yawDegrees, 0);
            Bounds placed = Measure(inst);
            // centrar en XZ sobre la posición y apoyar en la superficie
            Vector3 shift = new Vector3(position.x - placed.center.x, surfaceY - placed.min.y, position.z - placed.center.z);
            root.transform.position += shift;
            top = Measure(inst).max.y;
            foreach (var c in inst.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
            if (!decorOnly)
            {
                var col = root.AddComponent<BoxCollider>();
                Bounds fin = Measure(inst);
                col.center = root.transform.InverseTransformPoint(fin.center);
                col.size = fin.size;
            }
            ExternalUsed.Add(kit + "/" + model);
            return root;
        }

        public static Bounds Measure(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        // ------------------------------------------------------------------ monedas
        public static ParticleSystem CoinBurst(Transform parent, Vector3 position, Material gold)
        {
            var go = new GameObject("Lluvia de monedas");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(-90, 0, 0);       // el cono apunta hacia arriba (+Y)
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 4.2f);
            main.gravityModifier = 1.1f;
            main.startSize3D = true;
            main.startSizeX = .07f; main.startSizeY = .008f; main.startSizeZ = .07f;
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.maxParticles = 160;
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 28f;
            shape.radius = .12f;
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-8f, 8f);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            renderer.mesh = cylinder.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(cylinder);
            renderer.sharedMaterial = gold;
            return ps;
        }
    }
}
