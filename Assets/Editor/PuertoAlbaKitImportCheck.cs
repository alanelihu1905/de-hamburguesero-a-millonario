using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Tycoon3D.Editor
{
    /// <summary>
    /// Importa los FBX del kit de Puerto Alba (Docs/Handoffs/Claude/09_KIT_VISUAL_CIUDAD.md) y deja un informe:
    /// escala del importador, límites reales en Unity, base en y = 0 y lado del frente.
    /// Uso en batch: -executeMethod Tycoon3D.Editor.PuertoAlbaKitImportCheck.Run
    /// Informe: ArtSource/PuertoAlba/unity_import_check.txt
    /// </summary>
    public static class PuertoAlbaKitImportCheck
    {
        private const string ModelDir = "Assets/_Game/Models/PuertoAlba";
        private const string ReportPath = "ArtSource/PuertoAlba/unity_import_check.txt";

        // piezas cuyo frente (toldo, visera o pistola) sobresale hacia la cara de frente de Blender (-Y)
        private static readonly string[] FrontProtruding =
        {
            "SM_Tienda_Barrio", "SM_Puerta_Residencial", "SM_Gasolinera_Surtidor", "SM_Casa_Dos_Plantas", "SM_Hito_Casino", "SM_Boulevard_Terraza", "SM_Parque_Quiosco", "SM_Centro_Torre"
        };

        public static void Run()
        {
            var sb = new StringBuilder();
            int errores = 0;
            AssetDatabase.Refresh();

            foreach (var file in Directory.GetFiles(ModelDir, "SM_*.fbx"))
            {
                string path = file.Replace('\\', '/');
                try
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (importer == null || go == null)
                        throw new InvalidOperationException("No se pudo cargar el FBX.");

                    Bounds b = MeshBounds(go, out int tris);
                    string name = Path.GetFileNameWithoutExtension(path);
                    // lado del frente: si la pieza sobresale hacia -Y de Blender, en Unity eso debe verse en -Z
                    // (Unity usa +Z como frente). Un rango más largo hacia +Z significa que está girada.
                    string frente = "no aplica";
                    if (Array.IndexOf(FrontProtruding, name) >= 0)
                    {
                        frente = b.min.z < -b.max.z ? "frente en -Z (girar 180° en Y)" : "frente en +Z (correcto)";
                        if (b.min.z < -b.max.z) errores++;
                    }
                    bool baseEnCero = Mathf.Abs(b.min.y) < 0.001f;   // Unity: Y arriba
                    if (!baseEnCero) errores++;

                    sb.AppendLine(string.Format(
                        "{0}: escala_importador={1} unidades_archivo={2} tris={3} tamaño_XYZ={4:0.000}x{5:0.000}x{6:0.000} base_y={7:0.000} rango_z=[{8:0.000},{9:0.000}] {10}",
                        name, importer.globalScale, importer.useFileUnits, tris,
                        b.size.x, b.size.y, b.size.z, b.min.y, b.min.z, b.max.z, frente));
                }
                catch (Exception e)
                {
                    errores++;
                    sb.AppendLine(Path.GetFileName(path) + ": ERROR " + e.Message);
                }
            }

            int materiales = CrearMaterialesZonas(sb, ref errores);

            File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
            Debug.Log(sb.ToString());
            if (errores == 0) Debug.Log("TYCOON_KIT_IMPORT_OK: " + Directory.GetFiles(ModelDir, "SM_*.fbx").Length + " FBX importados y " + materiales + " materiales de zona creados; revisar el frente en el informe.");
            else Debug.LogError("TYCOON_KIT_IMPORT_FALLO: " + errores + " problema(s); ver " + ReportPath);
            EditorApplication.Exit(errores == 0 ? 0 : 1);
        }

        private static readonly string[] ZoneIds =
        {
            "CasaMarea", "MercadoDelSol", "MuelleFarol", "CentroDelFaro", "TalleresBrisa", "PaseoArena", "ColinaCobalto", "EstacionSur"
        };

        private static readonly string[] ZoneFields = { "muro", "zocalo", "acento", "tejado", "toldo", "vidrio" };

        /// <summary>
        /// Crea un material por zona y campo a partir de ArtSource/PuertoAlba/zonas_variacion.json (sin dependencias de JSON:
        /// busca el bloque de cada zona y sus colores hexadecimales). Los materiales quedan en Models/PuertoAlba/Zonas.
        /// </summary>
        private static int CrearMaterialesZonas(StringBuilder sb, ref int errores)
        {
            const string json = "ArtSource/PuertoAlba/zonas_variacion.json";
            const string dir = ModelDir + "/Zonas";
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(ModelDir, "Zonas");
            string texto = File.ReadAllText(json);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) { errores++; sb.AppendLine("ERROR: falta el shader URP/Lit."); return 0; }

            int creados = 0;
            foreach (var zona in ZoneIds)
            {
                int ini = texto.IndexOf("\"" + zona + "\": {", StringComparison.Ordinal);
                int fin = ini < 0 ? -1 : texto.IndexOf("}", ini, StringComparison.Ordinal);
                if (ini < 0 || fin < 0) { errores++; sb.AppendLine("ERROR: zona " + zona + " no está en el JSON."); continue; }
                string bloque = texto.Substring(ini, fin - ini);
                foreach (var campo in ZoneFields)
                {
                    var m = System.Text.RegularExpressions.Regex.Match(bloque, "\"" + campo + "\"\\s*:\\s*\"(#[0-9A-Fa-f]{6})\"");
                    if (!m.Success) { errores++; sb.AppendLine("ERROR: " + zona + "." + campo + " sin color válido."); continue; }
                    Color color;
                    if (!ColorUtility.TryParseHtmlString(m.Groups[1].Value, out color)) { errores++; continue; }
                    string path = dir + "/Zona_" + zona + "_" + campo + ".mat";
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null)
                    {
                        mat = new Material(shader) { name = "Zona_" + zona + "_" + campo };
                        AssetDatabase.CreateAsset(mat, path);
                    }
                    mat.SetColor("_BaseColor", color);
                    EditorUtility.SetDirty(mat);
                    creados++;
                }
            }
            AssetDatabase.SaveAssets();
            sb.AppendLine("MATERIALES_ZONA: " + creados + " materiales en " + dir);
            return creados;
        }

        private static Bounds MeshBounds(GameObject go, out int tris)
        {
            tris = 0;
            var filters = go.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length == 0) throw new InvalidOperationException("Sin MeshFilter en el FBX.");
            Bounds total = new Bounds();
            bool first = true;
            foreach (var f in filters)
            {
                Mesh m = f.sharedMesh;
                tris += m.triangles.Length / 3;
                // vértices en orientación de mundo, relativos al origen del prefab. Incluye la rotación de la raíz
                // (Unity suele importar los FBX de Blender con -90° en X), que es lo que se ve en la escena.
                Matrix4x4 toWorld = f.transform.localToWorldMatrix;
                foreach (var v in m.vertices)
                {
                    Vector3 p = toWorld.MultiplyPoint3x4(v) - go.transform.position;
                    if (first) { total = new Bounds(p, Vector3.zero); first = false; }
                    else total.Encapsulate(p);
                }
            }
            return total;
        }
    }
}
