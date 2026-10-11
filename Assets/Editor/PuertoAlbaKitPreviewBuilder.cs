using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tycoon3D.Editor
{
    /// <summary>
    /// Kit de Puerto Alba: prefabs ensamblados con colisiones y escena de vista previa propia
    /// (Assets/_Game/Scenes/PuertoAlba_KitPreview.unity). Ni la escena ni la cámara de vista previa forman parte de la build.
    /// Uso en batch:
    ///   -executeMethod Tycoon3D.Editor.PuertoAlbaKitPreviewBuilder.Build   (prefabs y escena)
    ///   -executeMethod Tycoon3D.Editor.PuertoAlbaKitPreviewBuilder.Verify  (comprobación y capturas)
    /// Informe: ArtSource/PuertoAlba/unity_preview_check.txt
    /// </summary>
    public static class PuertoAlbaKitPreviewBuilder
    {
        public const string ModelDir = "Assets/_Game/Models/PuertoAlba";
        public const string PrefabDir = ModelDir + "/Prefabs";
        public const string ZonasDir = ModelDir + "/Zonas";
        public const string ScenePath = "Assets/_Game/Scenes/PuertoAlba_KitPreview.unity";
        private const string ProjectDir = "ArtSource/PuertoAlba";
        private const string PreviewDir = ProjectDir + "/Preview/Unity";
        private const string ReportPath = ProjectDir + "/unity_preview_check.txt";

        private static readonly string[] ZoneIds =
        {
            "CasaMarea", "MercadoDelSol", "MuelleFarol", "CentroDelFaro", "TalleresBrisa", "PaseoArena", "ColinaCobalto", "EstacionSur"
        };

        // material del kit -> campo de zonas_variacion.json
        private static readonly Dictionary<string, string> MaterialAField = new Dictionary<string, string>
        {
            { "M_Muro_Base", "muro" }, { "M_Zocalo", "zocalo" }, { "M_Cornisa", "acento" },
            { "M_Tejado", "tejado" }, { "M_Toldo", "toldo" }, { "M_Vidrio", "vidrio" },
        };

        // tamaños esperados de cada prefab en Unity (X, Y alto, Z fondo) para la prueba
        private static readonly Dictionary<string, Vector3> ExpectedSize = new Dictionary<string, Vector3>
        {
            { "PF_Casa_Dos_Plantas", new Vector3(5.3f, 6.45f, 7.05f) },
            { "PF_Tienda_Barrio", new Vector3(6.2f, 4.45f, 4.95f) },
            { "PF_Gasolinera_Completa", new Vector3(19.85f, 5.5f, 8f) },   // letrero en x = -9.7 y caseta hasta x = 10.15
            { "PF_Hito_Casino", new Vector3(12f, 10.3f, 4.43f) },
        };

        // ------------------------------------------------------------------ construcción

        public static void Build()
        {
            try
            {
                EnsureFolder(ModelDir, "Prefabs");
                MakePrefab("PF_Casa_Dos_Plantas", new[] { ("SM_Casa_Dos_Plantas", Vector3.zero) },
                    new[] { Box(new Vector3(0, 3.1f, 0), new Vector3(5f, 6.2f, 6f)) });
                MakePrefab("PF_Tienda_Barrio", new[] { ("SM_Tienda_Barrio", Vector3.zero) },
                    new[] { Box(new Vector3(0, 2.1f, 0), new Vector3(6f, 4.2f, 4f)) });
                // canopy: cuatro pilares; el techo no bloquea. Surtidores y caseta/letrero con su colisión.
                MakePrefab("PF_Gasolinera_Completa",
                    new[]
                    {
                        ("SM_Gasolinera_Marquesina", Vector3.zero),
                        ("SM_Gasolinera_Surtidor", new Vector3(-2.5f, 0, 0)),
                        ("SM_Gasolinera_Surtidor", new Vector3(2.5f, 0, 0)),
                        ("SM_Gasolinera_Caseta", new Vector3(8.5f, 0, 0)),
                        ("SM_Gasolinera_Letrero", new Vector3(-8.5f, 0, 0)),
                    },
                    new[]
                    {
                        Box(new Vector3(-5.4f, 2.45f, -3.4f), new Vector3(.4f, 4.9f, .4f)),
                        Box(new Vector3(5.4f, 2.45f, -3.4f), new Vector3(.4f, 4.9f, .4f)),
                        Box(new Vector3(-5.4f, 2.45f, 3.4f), new Vector3(.4f, 4.9f, .4f)),
                        Box(new Vector3(5.4f, 2.45f, 3.4f), new Vector3(.4f, 4.9f, .4f)),
                        Box(new Vector3(-2.5f, .8f, 0), new Vector3(.9f, 1.6f, .65f)),
                        Box(new Vector3(2.5f, .8f, 0), new Vector3(.9f, 1.6f, .65f)),
                        Box(new Vector3(8.5f, 1.5f, 0), new Vector3(3.3f, 3f, 2.8f)),
                        Box(new Vector3(-9.4f, 1.8f, 0), new Vector3(.2f, 3.6f, .2f)),
                        Box(new Vector3(-7.6f, 1.8f, 0), new Vector3(.2f, 3.6f, .2f)),
                    });
                MakePrefab("PF_Hito_Casino", new[] { ("SM_Hito_Casino", Vector3.zero) },
                    new[]
                    {
                        Box(new Vector3(0, .5f, 0), new Vector3(12f, 1f, 4f)),
                        Box(new Vector3(-4.2f, 5.5f, 0), new Vector3(2f, 9f, 2f)),
                        Box(new Vector3(4.2f, 5.5f, 0), new Vector3(2f, 9f, 2f)),
                    });

                BuildScene();
                Debug.Log("KIT_PREVIEW_BUILD_OK: prefabs y escena " + ScenePath);
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

        private static void MakePrefab(string name, (string fbx, Vector3 at)[] parts, Col[] colliders)
        {
            var root = new GameObject(name);
            try
            {
                foreach (var (fbx, at) in parts)
                {
                    var src = AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + "/" + fbx + ".fbx");
                    if (src == null) throw new InvalidOperationException("Falta el FBX " + fbx);
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(src, root.transform);
                    inst.transform.localPosition = at;
                }
                foreach (var c in colliders)
                {
                    var bc = root.AddComponent<BoxCollider>();
                    bc.center = c.Center;
                    bc.size = c.Size;
                }
                string path = PrefabDir + "/" + name + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var sol = new GameObject("Luz de vista previa (no build)");
            var luz = sol.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.1f;
            sol.transform.rotation = Quaternion.Euler(45, 35, 0);

            var suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            suelo.name = "Calle de vista previa";
            suelo.transform.localScale = new Vector3(20f, 1f, 8f);   // 200 m × 80 m
            suelo.transform.position = new Vector3(0, 0, 0);

            var raiz = new GameObject("Kit");
            // frente de los prefabs hacia -Z (hacia la cámara y la calle)
            Place("PF_Hito_Casino", raiz.transform, new Vector3(0, 0, 0), 180f, "Hito Casino FOX");
            Place("PF_Casa_Dos_Plantas", raiz.transform, new Vector3(-16, 0, 0), 180f, "Casa de dos plantas");
            Place("PF_Tienda_Barrio", raiz.transform, new Vector3(16, 0, 0), 180f, "Tienda de barrio");
            Place("PF_Gasolinera_Completa", raiz.transform, new Vector3(36, 0, 0), 180f, "Gasolinera completa");

            // muestras de las ocho zonas en la fila de atrás: casa + tienda con sus materiales de zona
            for (int i = 0; i < ZoneIds.Length; i++)
            {
                float x = -56f + i * 16f;
                string zona = ZoneIds[i];
                var casa = Place("PF_Casa_Dos_Plantas", raiz.transform, new Vector3(x - 4f, 0, 14f), 180f, "Muestra " + zona + " casa");
                var tienda = Place("PF_Tienda_Barrio", raiz.transform, new Vector3(x + 4.5f, 0, 14f), 180f, "Muestra " + zona + " tienda");
                ApplyZone(casa, zona);
                ApplyZone(tienda, zona);
            }

            // cámara de vista previa: no forma parte de la build
            var cam = new GameObject("Camara vista previa hito (no build)");
            cam.tag = "MainCamera";
            var c = cam.AddComponent<Camera>();
            c.fieldOfView = 50f;
            cam.transform.position = new Vector3(0, 6, -26);
            cam.transform.LookAt(new Vector3(0, 4, 0));

            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("No se pudo guardar " + ScenePath);
            AssetDatabase.SaveAssets();
        }

        private static GameObject Place(string prefabName, Transform parent, Vector3 pos, float yaw, string nombre)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/" + prefabName + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Falta el prefab " + prefabName);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = nombre;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            return go;
        }

        /// <summary>Sustituye los materiales del kit por los de la zona (Zona_&lt;id&gt;_&lt;campo&gt;.mat).</summary>
        private static void ApplyZone(GameObject go, string zona)
        {
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string nombre = mats[i].name.Replace(" (Instance)", "");
                    if (!MaterialAField.TryGetValue(nombre, out string campo)) continue;
                    var zm = AssetDatabase.LoadAssetAtPath<Material>(ZonasDir + "/Zona_" + zona + "_" + campo + ".mat");
                    if (zm != null) mats[i] = zm;
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
                // 1. prefabs: existen, tienen malla y colisión, base en y = 0 y tamaño esperado
                foreach (var kv in ExpectedSize)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/" + kv.Key + ".prefab");
                    Check(prefab != null, "prefab " + kv.Key + " existe");
                    if (prefab == null) continue;
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    inst.transform.position = Vector3.zero;
                    inst.transform.rotation = Quaternion.identity;
                    var rs = inst.GetComponentsInChildren<MeshRenderer>(true);
                    Check(rs.Length > 0, kv.Key + ": tiene mallas (" + rs.Length + ")");
                    Check(inst.GetComponents<BoxCollider>().Length + inst.GetComponentsInChildren<BoxCollider>(true).Length > 0, kv.Key + ": tiene colisiones");
                    Bounds b = rs[0].bounds;
                    foreach (var r in rs) b.Encapsulate(r.bounds);
                    Vector3 s = b.size;
                    Check(Mathf.Abs(b.min.y) < .01f, kv.Key + ": base en y = 0 (" + b.min.y.ToString("F3") + ")");
                    Check(Mathf.Abs(s.x - kv.Value.x) < .05f && Mathf.Abs(s.y - kv.Value.y) < .05f && Mathf.Abs(s.z - kv.Value.z) < .05f,
                        kv.Key + ": tamaño " + s.ToString("F2") + " esperado " + kv.Value.ToString("F2"));
                    UnityEngine.Object.DestroyImmediate(inst);
                }

                // 2. escena: abre, tiene cámara de vista previa fuera de la build y el hito centrado
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Check(scene.IsValid() && scene.path == ScenePath, "la escena de vista previa abre");
                bool enBuild = EditorBuildSettings.scenes.Any(s => s.path == ScenePath);
                Check(!enBuild, "la escena de vista previa no está en la build");
                var hito = GameObject.Find("Hito Casino FOX");
                Check(hito != null, "hito Casino FOX presente");
                if (hito != null)
                {
                    Check(Mathf.Abs(hito.transform.position.x) < .01f && Mathf.Abs(hito.transform.position.z) < .01f, "hito centrado en x = 0, z = 0");
                    Check(PrefabUtility.GetPrefabInstanceStatus(hito) == PrefabInstanceStatus.Connected, "hito es instancia de prefab");
                }
                var casa = GameObject.Find("Casa de dos plantas");
                Check(casa != null && PrefabUtility.GetPrefabInstanceStatus(casa) == PrefabInstanceStatus.Connected, "casa es instancia de prefab");
                var cam = GameObject.FindWithTag("MainCamera");
                Check(cam != null && cam.GetComponent<Camera>() != null, "cámara de vista previa presente");

                // 3. muestras de zona: cada una usa solo materiales de su zona, y entre todas, 48 distintos
                var usados = new HashSet<string>();
                foreach (var zona in ZoneIds)
                {
                    var muestras = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                        .Where(t => t.name.StartsWith("Muestra " + zona)).ToList();
                    Check(muestras.Count == 2, "muestra " + zona + " tiene casa y tienda (" + muestras.Count + ")");
                    foreach (var t in muestras)
                    {
                        foreach (var r in t.GetComponentsInChildren<MeshRenderer>(true))
                            foreach (var m in r.sharedMaterials)
                                if (m != null && m.name.StartsWith("Zona_"))
                                {
                                    usados.Add(m.name);
                                    Check(m.name.StartsWith("Zona_" + zona + "_"), "material " + m.name + " pertenece a " + zona);
                                }
                    }
                }
                Check(usados.Count == 48, "48 materiales de zona distintos en las muestras (" + usados.Count + ")");

                // 4. capturas reales (solo si el render funciona)
                Directory.CreateDirectory(PreviewDir);
                string capturas = "";
                if (cam != null)
                {
                    var camHito = cam.GetComponent<Camera>();
                    capturas += Capture(camHito, PreviewDir + "/Kit_hito_casino_unity.png", 1600, 900) ? " hito" : "";
                    // vista general de la fila de atrás y del puesto de gasolina
                    var general = new GameObject("Camara vista general (no build)").AddComponent<Camera>();
                    general.fieldOfView = 60f;
                    general.transform.position = new Vector3(0, 40, -70);
                    general.transform.LookAt(new Vector3(0, 0, 4));
                    capturas += Capture(general, PreviewDir + "/Kit_vista_general_unity.png", 1600, 900) ? " general" : "";
                    UnityEngine.Object.DestroyImmediate(general.gameObject);
                    Check(capturas.Length > 0, "capturas generadas:" + capturas);
                }
            }
            catch (Exception e)
            {
                sb.AppendLine("ERROR " + e.Message);
                fallos++;
            }

            File.WriteAllText(ReportPath, sb.ToString(), new UTF8Encoding(false));
            Debug.Log(sb.ToString());
            if (fallos == 0) Debug.Log("TYCOON_KIT_PREVIEW_OK: prefabs, escena, muestras de zona y capturas comprobados.");
            else Debug.LogError("TYCOON_KIT_PREVIEW_FALLO: " + fallos + " problema(s); ver " + ReportPath);
            if (Application.isBatchMode) EditorApplication.delayCall += () => EditorApplication.Exit(fallos == 0 ? 0 : 1);
        }

        private static bool Capture(Camera cam, string path, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24);
            var prev = RenderTexture.active;
            try
            {
                cam.targetTexture = rt;
                cam.Render();   // primer fotograma: los shaders pueden salir sin cargar; se renderiza otra vez antes de leer
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
