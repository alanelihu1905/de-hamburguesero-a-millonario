using System;
using UnityEditor;
using UnityEngine;

namespace Tycoon3D.Editor
{
    internal static class CasaMareaPalette
    {
        private const string Root = "Assets/_Game/Models/CasaMarea/Paleta/";
        private const string MaterialRoot = "Assets/_Game/Materials/CasaMarea/";

        internal static GameObject Model(string name)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Root + name + ".fbx");
            if (model == null) throw new InvalidOperationException("Falta modelo con paleta: " + name);
            return model;
        }

        internal static Material Material(Material source)
        {
            if (source == null) throw new InvalidOperationException("Material FBX vacío.");
            string name = source.name;
            if (!name.Contains("M_Casa_Marea")) return CasaMareaModelIntegrator.UrpMaterial(source);
            bool emission = name.Contains("Emisivo");
            string path = MaterialRoot + (emission ? "Paleta_Emisiva.mat" : "Paleta_Base.mat");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            var texturePath = Root + "Texturas/Palette_CasaMarea_Albedo.png";
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Falta textura de paleta.");
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.sRGBTexture = true;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Falta URP/Lit.");
            material = new Material(shader) { name = emission ? "Paleta_Emisiva" : "Paleta_Base" };
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", texture);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }

    public static class CasaMareaPaletteIntegration
    {
        [MenuItem("Tools/Tycoon3D/Integrar arte completo Casa Marea")]
        public static void IntegrateAll()
        {
            CasaMareaModelIntegrator.Integrate();
            CasaMareaFacadeIntegrator.Integrate();
            CasaMareaNeighborhoodIntegrator.Integrate();
        }

        public static void IntegrateAndCapture()
        {
            IntegrateAll();
            CasaMareaPreviewCapture.Capture();
        }
    }
}
