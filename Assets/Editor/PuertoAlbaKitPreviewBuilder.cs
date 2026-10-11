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
            { "PF_Hito_Casino", new Vector3(12f, 10.3f, 4.47f) },   // letras hasta y = -2.47
            { "PF_Mercado_Pabellon_Reloj", new Vector3(10.6f, 9.4f, 8.6f) },
            { "PF_Muelle_Almacen_Grua", new Vector3(14.1f, 9.6f, 6.4f) },
            { "PF_Boulevard_Terraza", new Vector3(6.2f, 3.11f, 4.0f) },
            { "PF_Taller_Brisa", new Vector3(8.4f, 7.5f, 6.4f) },
            { "PF_Parque_Quiosco", new Vector3(4.6f, 4.0f, 5.45f) },
            { "PF_Centro_Torre", new Vector3(8.0f, 28.3f, 9.0f) },
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

                // seis piezas de zona (Blender → Unity: x = x, y = z, z = −y). Frente en +Z, igual que el kit.
                MakePrefab("PF_Mercado_Pabellon_Reloj", new[] { ("SM_Mercado_Pabellon_Reloj", Vector3.zero) },
                    new[]
                    {
                        Box(new Vector3(0, 4.5f, -3.0f), new Vector3(2.2f, 9f, 2.2f)),      // torre de reloj
                        Box(new Vector3(-4.4f, 2f, -3.4f), new Vector3(.3f, 4f, .3f)),       // pilares
                        Box(new Vector3(4.4f, 2f, -3.4f), new Vector3(.3f, 4f, .3f)),
                        Box(new Vector3(-4.4f, 2f, 3.4f), new Vector3(.3f, 4f, .3f)),
                        Box(new Vector3(4.4f, 2f, 3.4f), new Vector3(.3f, 4f, .3f)),
                    });
                MakePrefab("PF_Muelle_Almacen_Grua", new[] { ("SM_Muelle_Almacen_Grua", Vector3.zero) },
                    new[]
                    {
                        Box(new Vector3(-1.85f, 2.25f, 0), new Vector3(10f, 4.5f, 6f)),      // almacén
                        Box(new Vector3(6.65f, 4.5f, -2.5f), new Vector3(.5f, 9f, .5f)),     // patas de la grúa
                        Box(new Vector3(6.65f, 4.5f, 2.5f), new Vector3(.5f, 9f, .5f)),
                    });
                MakePrefab("PF_Boulevard_Terraza", new[] { ("SM_Boulevard_Terraza", Vector3.zero) },
                    new[]
                    {
                        Box(new Vector3(-2.8f, 1.5f, -1.8f), new Vector3(.16f, 3f, .16f)),   // postes de la pérgola
                        Box(new Vector3(2.8f, 1.5f, -1.8f), new Vector3(.16f, 3f, .16f)),
                        Box(new Vector3(-2.8f, 1.5f, 1.8f), new Vector3(.16f, 3f, .16f)),
                        Box(new Vector3(2.8f, 1.5f, 1.8f), new Vector3(.16f, 3f, .16f)),
                        Box(new Vector3(-1.5f, .375f, -.6f), new Vector3(.9f, .75f, .9f)),   // mesas
                        Box(new Vector3(1.5f, .375f, -.6f), new Vector3(.9f, .75f, .9f)),
                    });
                MakePrefab("PF_Taller_Brisa", new[] { ("SM_Taller_Brisa", Vector3.zero) },
                    new[]
                    {
                        Box(new Vector3(0, 2.1f, 0), new Vector3(8f, 4.2f, 6f)),             // nave
                        Box(new Vector3(2.8f, 6f, -2f), new Vector3(.7f, 3f, .7f)),          // chimenea
                    });
                MakePrefab("PF_Parque_Quiosco", new[] { ("SM_Parque_Quiosco", Vector3.zero) },
                    new[] { Box(new Vector3(0, 1.7f, 0), new Vector3(3.6f, 2.6f, 3.6f)) });   // cuerpo octogonal aproximado
                MakePrefab("PF_Centro_Torre", new[] { ("SM_Centro_Torre", Vector3.zero) },
                    new[]
                    {
                        Box(new Vector3(0, 12f, 0), new Vector3(8f, 24f, 8f)),               // fuste
                        Box(new Vector3(0, 26f, 0), new Vector3(6f, 4f, 6f)),                // retranqueo
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

            // recorrido urbano compacto alrededor del casino: todo detrás del hito (z ≥ 19) o a los lados (|x| ≥ 19).
            // La plaza de acceso del casino (x en −7…7, z en −26…−1.5) queda libre; la comprueba Verify.
            var piezasZona = new (string prefab, Vector3 pos, string zona, string nombre)[]
            {
                ("PF_Mercado_Pabellon_Reloj", new Vector3(-26, 0, 24), "MercadoDelSol", "Zona Mercado del Sol pabellon"),
                ("PF_Muelle_Almacen_Grua", new Vector3(26, 0, 24), "MuelleFarol", "Zona Muelle Farol almacen y grua"),
                ("PF_Boulevard_Terraza", new Vector3(-12, 0, 33), "CentroDelFaro", "Zona Centro del Faro terraza"),
                ("PF_Taller_Brisa", new Vector3(12, 0, 33), "TalleresBrisa", "Zona Talleres Brisa taller"),
                ("PF_Parque_Quiosco", new Vector3(-40, 0, 40), "PaseoArena", "Zona Paseo Arena quiosco"),
                ("PF_Centro_Torre", new Vector3(46, 0, 42), "CentroDelFaro", "Zona Centro del Faro torre"),
            };
            foreach (var pz in piezasZona)
                ApplyZone(Place(pz.prefab, raiz.transform, pz.pos, 180f, pz.nombre), pz.zona);

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

                // 3b. seis piezas de zona presentes, instancias de prefab y con materiales de su zona
                var zonasPiezas = new[] { "Zona Mercado", "Zona Muelle", "Zona Centro del Faro terraza", "Zona Talleres", "Zona Paseo", "Zona Centro del Faro torre" };
                foreach (var nombreZona in zonasPiezas)
                {
                    var go = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(t => t.name.StartsWith(nombreZona));
                    Check(go != null && PrefabUtility.GetPrefabInstanceStatus(go.gameObject) == PrefabInstanceStatus.Connected, "pieza de zona '" + nombreZona + "' es instancia de prefab");
                    if (go != null)
                    {
                        var mats = go.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r => r.sharedMaterials).Where(m => m != null && m.name.StartsWith("Zona_")).Select(m => m.name).ToList();
                        Check(mats.Count > 0, "pieza '" + nombreZona + "' usa materiales de zona (" + mats.Count + ")");
                    }
                }

                // 3c. plaza de acceso del casino libre: ningún renderer de la fila (salvo el hito) cruza el rectángulo x −7…7, z −26…−1.5
                var plaza = new Bounds(new Vector3(0, 6f, -13.75f), new Vector3(14f, 12f, 24.5f));
                var raizKit = GameObject.Find("Kit");
                bool cruza = false;
                string quien = "";
                if (raizKit != null)
                    foreach (var r in raizKit.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        bool dentroDelHito = false;
                        for (var t = r.transform; t != null; t = t.parent) if (t.name == "Hito Casino FOX") dentroDelHito = true;
                        if (dentroDelHito) continue;
                        if (r.bounds.Intersects(plaza)) { cruza = true; quien = r.transform.root.name + "/" + r.name; break; }
                    }
                Check(!cruza, "plaza de acceso del casino libre" + (cruza ? " (ocupa " + quien + ")" : ""));

                // 3d. el casino es visible: desde la cámara del hito, el primer obstáculo del rayo es el propio hito
                if (cam != null && hito != null)
                {
                    var cc = cam.GetComponent<Camera>();
                    Vector3 centro = hito.transform.position + Vector3.up * 4f;
                    Vector3 vp = cc.WorldToViewportPoint(centro);
                    Check(vp.z > 0 && vp.x > 0 && vp.x < 1 && vp.y > 0 && vp.y < 1, "el casino está dentro del encuadre de su cámara");
                    Vector3 dir = centro - cc.transform.position;
                    bool obstruido = Physics.Raycast(cc.transform.position, dir.normalized, out RaycastHit hit, dir.magnitude - .5f, ~0, QueryTriggerInteraction.Ignore);
                    Check(!obstruido, "nada tapa el casino desde su cámara" + (obstruido ? " (tapa " + hit.collider.name + ")" : ""));
                }

                // 4. capturas reales (solo si el render funciona)
                Shader.WarmupAllShaders();   // calentar shaders antes de capturar
                Directory.CreateDirectory(PreviewDir);
                string capturas = "";
                if (cam != null)
                {
                    var camHito = cam.GetComponent<Camera>();
                    bool capturaHito = Capture(camHito, PreviewDir + "/Kit_hito_casino_unity.png", 1600, 900);
                    capturas += capturaHito ? " hito" : "";
                    if (capturaHito && hito != null)
                    {
                        Check(LetrasVisibles(camHito, hito.transform.position, PreviewDir + "/Kit_hito_casino_unity.png", out string detalle),
                            "el rótulo del casino muestra letras en la captura (" + detalle + ")");
                    }
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

        /// <summary>
        /// Comprueba en la captura que el rótulo tiene letras: dentro de la proyección del rótulo hay píxeles ámbar
        /// (las letras) sobre el fondo oscuro, y no todo el rótulo es ámbar (como cuando estaba vacío).
        /// </summary>
        private static bool LetrasVisibles(Camera cam, Vector3 origen, string path, out string detalle)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            tex.LoadImage(File.ReadAllBytes(path));
            // rótulo: 8 m de ancho (x), altura de 5.8 a 8.2 m (y) y a 2.2 m delante del centro (z), según build_kit_ciudad.py
            var esquinas = new[]
            {
                new Vector3(-4f, 5.8f, 2.2f), new Vector3(4f, 5.8f, 2.2f),
                new Vector3(-4f, 8.2f, 2.2f), new Vector3(4f, 8.2f, 2.2f),
            };
            // proyectar con el aspecto de la captura (1600×900), no con el de la ventana de batch
            float aspectoAntes = cam.aspect;
            cam.aspect = (float)tex.width / tex.height;
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            foreach (var e in esquinas)
            {
                Vector3 vp = cam.WorldToViewportPoint(origen + e);
                Vector3 sp = new Vector3(vp.x * tex.width, vp.y * tex.height, vp.z);
                minX = Mathf.Min(minX, sp.x); maxX = Mathf.Max(maxX, sp.x);
                minY = Mathf.Min(minY, sp.y); maxY = Mathf.Max(maxY, sp.y);
            }
            cam.aspect = aspectoAntes;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(minX), 0, tex.width - 1), x1 = Mathf.Clamp(Mathf.CeilToInt(maxX), 0, tex.width - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(minY), 0, tex.height - 1), y1 = Mathf.Clamp(Mathf.CeilToInt(maxY), 0, tex.height - 1);
            Color[] px = tex.GetPixels(x0, y0, Mathf.Max(1, x1 - x0), Mathf.Max(1, y1 - y0));
            // color de las letras tras la luz de Unity: naranja cálido (el hex #F4C95D sale más saturado en pantalla)
            int ambar = 0, oscuro = 0;
            foreach (var c in px)
            {
                if (c.r > .75f && c.r - c.b > .45f && c.g > .35f) ambar++;
                else if (c.r < .2f && c.g < .2f && c.b < .25f) oscuro++;
            }
            float fraccion = px.Length > 0 ? (float)ambar / px.Length : 0f;
            float fraccionOscuro = px.Length > 0 ? (float)oscuro / px.Length : 0f;
            detalle = "ámbar " + ambar + " px (" + (fraccion * 100f).ToString("F1") + "%), fondo oscuro " + (fraccionOscuro * 100f).ToString("F1") + "%";
            UnityEngine.Object.DestroyImmediate(tex);
            // letras: suficiente ámbar, y fondo oscuro visible alrededor (el rótulo vacío era casi todo ámbar)
            return ambar >= 400 && fraccion >= .03f && fraccion <= .6f && fraccionOscuro >= .2f;
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
