using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tycoon3D.Editor
{
    /// <summary>
    /// Kit interior del casino de Puerto Alba: prefabs con colisión simple, variantes rojo y dorado, y una escena de
    /// vista previa separada (Assets/_Game/Scenes/PuertoAlba_CasinoInteriorPreview.unity). ARTE: no contiene ni implementa
    /// apuestas, pagos ni RNG; eso vive en Assets/_Game/Scripts/Casino y no se toca aquí.
    /// Uso en batch:
    ///   -executeMethod Tycoon3D.Editor.PuertoAlbaCasinoPreviewBuilder.Build    (variantes, prefabs y escena)
    ///   -executeMethod Tycoon3D.Editor.PuertoAlbaCasinoPreviewBuilder.Verify   (importación, medidas, escena y captura)
    /// Informe: ArtSource/PuertoAlba/Casino/unity_casino_check.txt
    /// </summary>
    public static class PuertoAlbaCasinoPreviewBuilder
    {
        public const string ModelDir = "Assets/_Game/Models/PuertoAlba/Casino";
        public const string PrefabDir = ModelDir + "/Prefabs";
        public const string VarianteDir = ModelDir + "/Variantes";
        public const string ScenePath = "Assets/_Game/Scenes/PuertoAlba_CasinoInteriorPreview.unity";
        private const string ProjectDir = "ArtSource/PuertoAlba/Casino";
        private const string PreviewDir = "ArtSource/PuertoAlba/Preview/Unity";
        private const string ReportPath = ProjectDir + "/unity_casino_check.txt";

        private static readonly string[] Variantes = { "rojo", "dorado" };
        private static readonly string[] Roles = { "cuerpo", "acento", "fieltro", "madera" };
        private static readonly Dictionary<string, string> MaterialARol = new Dictionary<string, string>
        {
            { "M_Casino_Rojo", "cuerpo" }, { "M_Casino_Oro", "acento" }, { "M_Fieltro", "fieltro" }, { "M_Madera", "madera" },
        };

        // tamaño esperado en Unity (X, Y alto, Z fondo) = (X, Z de Blender, Y de Blender)
        private static readonly Dictionary<string, Vector3> ExpectedSize = new Dictionary<string, Vector3>
        {
            { "SM_Slot_Tres_Rodillos", new Vector3(0.948f, 1.8f, 0.735f) },
            { "SM_Mesa_Ruleta", new Vector3(2.24f, 0.89f, 1.44f) },
            { "SM_Mesa_Blackjack", new Vector3(1.84f, 0.86f, 0.92f) },
        };

        // ------------------------------------------------------------------ construcción

        public static void Build()
        {
            try
            {
                EnsureFolder("Assets/_Game/Models/PuertoAlba", "Casino");
                EnsureFolder(ModelDir, "Prefabs");
                EnsureFolder(ModelDir, "Variantes");
                CrearVariantes();

                // colisiones simples en coordenadas de Unity: (x, z de Blender, −y de Blender)
                MakePrefab("PF_Slot_Tres_Rodillos", "SM_Slot_Tres_Rodillos",
                    new[] { Box(new Vector3(0, .8f, 0), new Vector3(.8f, 1.4f, .7f)), Box(new Vector3(0, 1.65f, 0), new Vector3(.8f, .3f, .7f)) });
                MakePrefab("PF_Mesa_Ruleta", "SM_Mesa_Ruleta",
                    new[] { Box(new Vector3(0, .38f, 0), new Vector3(2.24f, .76f, 1.44f)) });
                MakePrefab("PF_Mesa_Blackjack", "SM_Mesa_Blackjack",
                    new[] { Box(new Vector3(0, .37f, 0), new Vector3(1.8f, .74f, .9f)) });

                BuildScene();
                Debug.Log("CASINO_INTERIOR_BUILD_OK: variantes, prefabs y escena " + ScenePath);
                if (Application.isBatchMode) EditorApplication.delayCall += () => EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.delayCall += () => EditorApplication.Exit(1);
            }
        }

        private struct Col { public Vector3 Center, Size; }
        private static Col Box(Vector3 c, Vector3 s) => new Col { Center = c, Size = s };

        private static void MakePrefab(string prefabName, string fbxName, Col[] colliders)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + "/" + fbxName + ".fbx");
            if (src == null) throw new InvalidOperationException("Falta el FBX " + fbxName);
            var root = new GameObject(prefabName);
            try
            {
                PrefabUtility.InstantiatePrefab(src, root.transform);
                foreach (var c in colliders)
                {
                    var bc = root.AddComponent<BoxCollider>();
                    bc.center = c.Center;
                    bc.size = c.Size;
                }
                PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/" + prefabName + ".prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        /// <summary>Crea Casino_&lt;variante&gt;_&lt;rol&gt;.mat a partir de casino_variantes.json.</summary>
        private static void CrearVariantes()
        {
            string texto = File.ReadAllText(ProjectDir + "/casino_variantes.json");
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Falta URP/Lit.");
            foreach (var v in Variantes)
            {
                int ini = texto.IndexOf("\"" + v + "\": {", StringComparison.Ordinal);
                if (ini < 0) throw new InvalidOperationException("Variante " + v + " ausente en el JSON.");
                string bloque = texto.Substring(ini, texto.IndexOf("}", ini, StringComparison.Ordinal) - ini);
                foreach (var rol in Roles)
                {
                    var m = Regex.Match(bloque, "\"" + rol + "\"\\s*:\\s*\"(#[0-9A-Fa-f]{6})\"");
                    if (!m.Success) throw new InvalidOperationException(v + "." + rol + " sin color válido.");
                    ColorUtility.TryParseHtmlString(m.Groups[1].Value, out Color color);
                    string path = VarianteDir + "/Casino_" + v + "_" + rol + ".mat";
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null)
                    {
                        mat = new Material(shader) { name = "Casino_" + v + "_" + rol };
                        AssetDatabase.CreateAsset(mat, path);
                    }
                    mat.SetColor("_BaseColor", color);
                    EditorUtility.SetDirty(mat);
                }
            }
            AssetDatabase.SaveAssets();
        }

        private static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var luz = new GameObject("Luz de vista previa (no build)").AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.1f;
            luz.transform.rotation = Quaternion.Euler(50, 30, 0);

            var suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            suelo.name = "Suelo de vista previa";
            suelo.transform.localScale = new Vector3(3f, 1f, 2f);   // 30 m × 20 m
            suelo.transform.position = new Vector3(0, 0, 2);

            var raiz = new GameObject("Casino interior");
            // fila roja (z = 0) y dorada (z = 3); frente hacia −Z, hacia la cámara
            Place("PF_Slot_Tres_Rodillos", raiz.transform, new Vector3(-2.2f, 0, 0), "Slot rojo 1", "rojo");
            Place("PF_Slot_Tres_Rodillos", raiz.transform, new Vector3(-1.1f, 0, 0), "Slot rojo 2", "rojo");
            Place("PF_Mesa_Ruleta", raiz.transform, new Vector3(1.0f, 0, 0), "Ruleta roja", "rojo");
            Place("PF_Mesa_Blackjack", raiz.transform, new Vector3(3.6f, 0, 0), "Blackjack rojo", "rojo");
            Place("PF_Slot_Tres_Rodillos", raiz.transform, new Vector3(-2.2f, 0, 3f), "Slot dorado 1", "dorado");
            Place("PF_Slot_Tres_Rodillos", raiz.transform, new Vector3(-1.1f, 0, 3f), "Slot dorado 2", "dorado");
            Place("PF_Mesa_Ruleta", raiz.transform, new Vector3(1.0f, 0, 3f), "Ruleta dorada", "dorado");
            Place("PF_Mesa_Blackjack", raiz.transform, new Vector3(3.6f, 0, 3f), "Blackjack dorado", "dorado");

            // cámara de vista previa (no build), a la altura de los ojos
            var cam = new GameObject("Camara vista previa casino (no build)");
            cam.tag = "MainCamera";
            var c = cam.AddComponent<Camera>();
            c.fieldOfView = 60f;
            cam.transform.position = new Vector3(0, 1.7f, -5.5f);
            cam.transform.LookAt(new Vector3(0.6f, 1.0f, 1.4f));

            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("No se pudo guardar " + ScenePath);
            AssetDatabase.SaveAssets();
        }

        private static void Place(string prefabName, Transform parent, Vector3 pos, string nombre, string variante)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/" + prefabName + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Falta el prefab " + prefabName);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = nombre;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, 180f, 0);
            ApplyVariante(go, variante);
        }

        /// <summary>Sustituye los materiales base por los de la variante (rojo o dorado).</summary>
        private static void ApplyVariante(GameObject go, string variante)
        {
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string nombre = mats[i].name.Replace(" (Instance)", "");
                    if (!MaterialARol.TryGetValue(nombre, out string rol)) continue;
                    var vm = AssetDatabase.LoadAssetAtPath<Material>(VarianteDir + "/Casino_" + variante + "_" + rol + ".mat");
                    if (vm != null) mats[i] = vm;
                }
                r.sharedMaterials = mats;
            }
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }

        // ------------------------------------------------------------------ verificación

        public static void Verify()
        {
            var sb = new StringBuilder();
            int fallos = 0;
            void Check(bool ok, string texto)
            {
                sb.AppendLine((ok ? "OK    " : "FALLO ") + texto);
                if (!ok) fallos++;
            }

            try
            {
                string stats = File.ReadAllText(ProjectDir + "/stats_casino.json");

                // 1. importación: FBX reales, triángulos iguales a Blender, dimensiones y base en y = 0
                foreach (var nombre in ExpectedSize.Keys)
                {
                    string path = ModelDir + "/" + nombre + ".fbx";
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    Check(go != null, "FBX " + nombre + " importa");
                    if (go == null) continue;
                    int tris = 0;
                    Bounds b = new Bounds();
                    bool primero = true;
                    foreach (var f in go.GetComponentsInChildren<MeshFilter>(true))
                    {
                        tris += f.sharedMesh.triangles.Length / 3;
                        Matrix4x4 toRoot = f.transform.localToWorldMatrix;
                        foreach (var v in f.sharedMesh.vertices)
                        {
                            Vector3 p = toRoot.MultiplyPoint3x4(v) - go.transform.position;
                            if (primero) { b = new Bounds(p, Vector3.zero); primero = false; }
                            else b.Encapsulate(p);
                        }
                    }
                    int trisBlender = TrisEnBlender(stats, nombre);
                    Check(trisBlender > 0 && tris == trisBlender, nombre + ": " + tris + " triángulos en Unity = " + trisBlender + " en Blender");
                    Vector3 s = b.size;
                    Vector3 e = ExpectedSize[nombre];
                    Check(Mathf.Abs(s.x - e.x) < .03f && Mathf.Abs(s.y - e.y) < .03f && Mathf.Abs(s.z - e.z) < .03f,
                        nombre + ": tamaño " + s.ToString("F3") + " esperado " + e.ToString("F3"));
                    Check(Mathf.Abs(b.min.y) < .01f, nombre + ": base en y = 0 (" + b.min.y.ToString("F3") + ")");
                }

                // 2. prefabs con colisión y tamaño de su FBX
                foreach (var kv in new[] { ("PF_Slot_Tres_Rodillos", "SM_Slot_Tres_Rodillos"), ("PF_Mesa_Ruleta", "SM_Mesa_Ruleta"), ("PF_Mesa_Blackjack", "SM_Mesa_Blackjack") })
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/" + kv.Item1 + ".prefab");
                    Check(prefab != null, "prefab " + kv.Item1 + " existe");
                    if (prefab == null) continue;
                    int cols = prefab.GetComponents<BoxCollider>().Length;
                    Check(cols >= 1, kv.Item1 + ": tiene colisiones simples (" + cols + ")");
                    var variantes = prefab.GetComponentsInChildren<MeshRenderer>(true);
                    Check(variantes.Length > 0, kv.Item1 + ": tiene mallas (" + variantes.Length + ")");
                }

                // 3. variantes de material: 8 archivos con su .meta
                int matsVar = 0;
                foreach (var v in Variantes)
                    foreach (var rol in Roles)
                        if (AssetDatabase.LoadAssetAtPath<Material>(VarianteDir + "/Casino_" + v + "_" + rol + ".mat") != null) matsVar++;
                Check(matsVar == Variantes.Length * Roles.Length, "materiales de variante presentes (" + matsVar + " de 8)");

                // 4. escena separada: abre, no está en la build, 8 piezas en dos filas y variantes correctas
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Check(scene.IsValid() && scene.path == ScenePath, "la escena interior abre");
                Check(!EditorBuildSettings.scenes.Any(s => s.path == ScenePath), "la escena interior no está en la build");
                var piezas = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                    .Where(t => t.parent != null && t.parent.name == "Casino interior").ToList();
                Check(piezas.Count == 8, "8 piezas en la escena (" + piezas.Count + ")");
                foreach (var t in piezas)
                {
                    Check(PrefabUtility.GetPrefabInstanceStatus(t.gameObject) == PrefabInstanceStatus.Connected, t.name + " es instancia de prefab");
                    bool rojo = t.name.Contains("roj"), dorado = t.name.Contains("dorad");   // «roja»/«rojo», «dorada»/«dorado»
                    string esperado = rojo ? "rojo" : "dorado";
                    var mats = t.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.name).ToList();
                    bool usaVariante = mats.Any(m => m.StartsWith("Casino_" + esperado + "_"));
                    bool mezcla = mats.Any(m => m.StartsWith("Casino_") && !m.StartsWith("Casino_" + esperado + "_"));
                    Check(usaVariante && !mezcla, t.name + " usa solo la variante " + esperado);
                }

                // 5. captura tras calentar shaders: deben verse dorado y rojo, y el fondo oscuro del casino
                Shader.WarmupAllShaders();
                Directory.CreateDirectory(PreviewDir);
                var cam = GameObject.FindWithTag("MainCamera")?.GetComponent<Camera>();
                Check(cam != null, "cámara de vista previa presente");
                if (cam != null)
                {
                    string ruta = PreviewDir + "/Casino_interior_unity.png";
                    bool captura = Capture(cam, ruta, 1600, 900);
                    Check(captura, "captura de la escena interior generada");
                    if (captura)
                    {
                        var tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                        tex.LoadImage(File.ReadAllBytes(ruta));
                        Color[] px = tex.GetPixels();
                        int oro = 0, rojoPx = 0;
                        foreach (var c in px)
                        {
                            if (c.r > .75f && c.g > .55f && c.b < .4f && c.r - c.b > .4f) oro++;
                            if (c.r > .35f && c.g < .2f && c.b < .25f) rojoPx++;
                        }
                        float fOro = (float)oro / px.Length, fRojo = (float)rojoPx / px.Length;
                        Check(fOro >= .002f, "la captura muestra dorado (" + (fOro * 100f).ToString("F2") + "%)");
                        Check(fRojo >= .02f, "la captura muestra rojo (" + (fRojo * 100f).ToString("F2") + "%)");
                        UnityEngine.Object.DestroyImmediate(tex);
                    }
                }
            }
            catch (Exception e)
            {
                sb.AppendLine("ERROR " + e.Message);
                fallos++;
            }

            File.WriteAllText(ReportPath, sb.ToString(), new UTF8Encoding(false));
            Debug.Log(sb.ToString());
            if (fallos == 0) Debug.Log("TYCOON_CASINO_INTERIOR_PREVIEW_OK: importación, prefabs, variantes, escena y captura comprobados.");
            else Debug.LogError("TYCOON_CASINO_INTERIOR_PREVIEW_FALLO: " + fallos + " problema(s); ver " + ReportPath);
            if (Application.isBatchMode) EditorApplication.delayCall += () => EditorApplication.Exit(fallos == 0 ? 0 : 1);
        }

        /// <summary>Triángulos de la pieza en stats_casino.json (búsqueda simple por bloque).</summary>
        private static int TrisEnBlender(string json, string pieza)
        {
            int ini = json.IndexOf("\"" + pieza + "\": {", StringComparison.Ordinal);
            if (ini < 0) return -1;
            var m = Regex.Match(json.Substring(ini), "\"triangulos\":\\s*(\\d+)");
            return m.Success ? int.Parse(m.Groups[1].Value) : -1;
        }

        private static bool Capture(Camera cam, string path, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24);
            var prev = RenderTexture.active;
            try
            {
                cam.targetTexture = rt;
                cam.Render();   // primer fotograma: se renderiza otra vez antes de leer
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Captura no disponible (" + path + "): " + e.Message);
                return false;
            }
            finally
            {
                cam.targetTexture = null;
                RenderTexture.active = prev;
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }
    }
}
