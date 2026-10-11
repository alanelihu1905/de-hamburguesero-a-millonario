using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tycoon3D.Editor
{
    /// <summary>
    /// Sala cerrada del Casino FOX (DEF-01 y DEF-03 del QA): muros con zócalo, techo con vigas, puertas de ENTRADA y SALIDA,
    /// mostrador de cambio y seis taburetes, alrededor del mobiliario ya existente (bancos rojo y dorado, ruleta, blackjack y
    /// letrero JACKPOT). Escena de vista previa aislada; no cambia la ubicación urbana del casino ni la lógica del juego.
    /// ARTE, NO MECÁNICA: sin apuestas, pagos ni RNG.
    /// Uso en batch:
    ///   -executeMethod Tycoon3D.Editor.PuertoAlbaCasinoSalaBuilder.Build
    ///   -executeMethod Tycoon3D.Editor.PuertoAlbaCasinoSalaBuilder.Verify
    /// Informe: ArtSource/PuertoAlba/Casino/unity_sala_check.txt
    /// </summary>
    public static class PuertoAlbaCasinoSalaBuilder
    {
        public const string ModelDir = "Assets/_Game/Models/PuertoAlba/Casino";
        public const string SalaDir = ModelDir + "/Sala";
        public const string PrefabDir = ModelDir + "/Prefabs";
        public const string VarianteDir = ModelDir + "/Variantes";
        public const string ScenePath = "Assets/_Game/Scenes/PuertoAlba_CasinoSalaPreview.unity";
        private const string ProjectDir = "ArtSource/PuertoAlba/Casino";
        private const string PreviewDir = "ArtSource/PuertoAlba/Preview/Unity";
        private const string ReportPath = ProjectDir + "/unity_sala_check.txt";
        public const string RootName = "Casino sala";
        public const string RefName = "Referencia humana 1.80 m (no build)";

        private static readonly Dictionary<string, string> MaterialARol = new Dictionary<string, string>
        {
            { "M_Casino_Rojo", "cuerpo" }, { "M_Casino_Oro", "acento" }, { "M_Fieltro", "fieltro" }, { "M_Madera", "madera" },
        };

        // tamaño esperado en Unity (X, alto, fondo) = (X, Z de Blender, Y de Blender), según stats_sala.json
        private static readonly Dictionary<string, Vector3> ExpectedSize = new Dictionary<string, Vector3>
        {
            { "PF_Sala_Muro", new Vector3(4.0f, 5.0f, 0.40f) },
            { "PF_Sala_Esquina", new Vector3(0.9f, 5.0f, 0.9f) },
            { "PF_Sala_Puerta_Entrada", new Vector3(4.0f, 5.0f, 1.533f) },
            { "PF_Sala_Puerta_Salida", new Vector3(4.0f, 5.0f, 1.533f) },
            { "PF_Sala_Techo", new Vector3(4.0f, 0.4f, 4.0f) },
            { "PF_Mostrador_Cambio", new Vector3(3.2f, 2.455f, 1.0f) },
            { "PF_Taburete", new Vector3(0.429f, 0.69f, 0.44f) },
        };

        private struct Col { public Vector3 Center, Size; }
        private static Col Box(Vector3 c, Vector3 s) => new Col { Center = c, Size = s };

        // ------------------------------------------------------------------ construcción

        public static void Build()
        {
            try
            {
                AssetDatabase.Refresh();
                // colisiones simples en coordenadas de Unity: (x, z de Blender, −y de Blender)
                MakePrefab("PF_Sala_Muro", "SM_Sala_Muro", new[] { Box(new Vector3(0, 2.5f, 0), new Vector3(4f, 5f, .3f)) });
                MakePrefab("PF_Sala_Esquina", "SM_Sala_Esquina", new[] { Box(new Vector3(0, 2.5f, 0), new Vector3(.9f, 5f, .9f)) });
                var puerta = new[]
                {
                    Box(new Vector3(-1.75f, 2.5f, 0), new Vector3(.5f, 5f, .3f)),   // jamba izquierda
                    Box(new Vector3(1.75f, 2.5f, 0), new Vector3(.5f, 5f, .3f)),    // jamba derecha
                    Box(new Vector3(0, 4.1f, 0), new Vector3(3f, 1.8f, .3f)),       // dintel
                };
                MakePrefab("PF_Sala_Puerta_Entrada", "SM_Sala_Puerta_Entrada", puerta);
                MakePrefab("PF_Sala_Puerta_Salida", "SM_Sala_Puerta_Salida", puerta);
                MakePrefab("PF_Sala_Techo", "SM_Sala_Techo", new[] { Box(new Vector3(0, .35f, 0), new Vector3(4f, .1f, 4f)) });
                MakePrefab("PF_Mostrador_Cambio", "SM_Mostrador_Cambio", new[]
                {
                    Box(new Vector3(0, .525f, 0), new Vector3(3f, 1.05f, .9f)),     // cuerpo
                    Box(new Vector3(0, 1.08f, 0), new Vector3(3.2f, .06f, 1f)),     // tablero
                });
                MakePrefab("PF_Taburete", "SM_Taburete", new[] { Box(new Vector3(0, .345f, 0), new Vector3(.44f, .69f, .44f)) });

                BuildScene();
                Debug.Log("CASINO_SALA_BUILD_OK: prefabs y escena " + ScenePath);
                if (Application.isBatchMode) EditorApplication.delayCall += () => EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.delayCall += () => EditorApplication.Exit(1);
            }
        }

        private static void MakePrefab(string prefabName, string fbxName, Col[] colliders)
        {
            string fbx = SalaDir + "/" + fbxName + ".fbx";
            AssetDatabase.ImportAsset(fbx, ImportAssetOptions.ForceUpdate);
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            if (src == null) throw new InvalidOperationException("Falta el FBX " + fbx);
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
                PrefabUtility.SaveAsPrefabAsset(root, SalaDir + "/" + prefabName + ".prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject Place(string prefabName, Transform parent, Vector3 pos, float yaw, string nombre, string variante)
        {
            string path = (prefabName.StartsWith("PF_Sala") || prefabName == "PF_Mostrador_Cambio" || prefabName == "PF_Taburete" ? SalaDir : PrefabDir) + "/" + prefabName + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Falta el prefab " + path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = nombre;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            if (variante != null) ApplyVariante(go, variante);
            return go;
        }

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

        private static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // luz ambiente plana: sin ella el interior cerrado queda casi a oscuras (solo vista previa)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.5f, .46f, .42f);
            var luz = new GameObject("Luz de vista previa (no build)").AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = .45f;
            luz.transform.rotation = Quaternion.Euler(60, 20, 0);
            foreach (var p in new[] { new Vector3(-5, 4.2f, -5), new Vector3(5, 4.2f, -5), new Vector3(-5, 4.2f, 5), new Vector3(5, 4.2f, 5) })
                LuzPunto("Luz sala (no build)", p, 14f, 1.6f, Color.white);
            LuzPunto("Luz letrero (no build)", new Vector3(0, 3.0f, 6f), 8f, 3f, new Color(1f, .8f, .45f));
            LuzPunto("Luz puerta entrada (no build)", new Vector3(-4f, 3.4f, -7.5f), 9f, 5f, Color.white);
            LuzPunto("Luz puerta salida (no build)", new Vector3(4f, 3.4f, -7.5f), 9f, 5f, Color.white);

            // suelo de la sala: 20 × 20 m
            var suelo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            suelo.name = "Suelo sala";
            suelo.transform.position = new Vector3(0, -.05f, 0);
            suelo.transform.localScale = new Vector3(20f, .1f, 20f);
            suelo.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(VarianteDir + "/Casino_rojo_madera.mat");

            var raiz = new GameObject(RootName);
            var arq = new GameObject("Arquitectura").transform;
            arq.SetParent(raiz.transform, false);
            // muros: la cara interior de cada módulo mira a la sala; el origen queda 0.15 m fuera del plano interior
            float[] cs = { -8f, -4f, 0f, 4f, 8f };
            for (int i = 0; i < 5; i++)
            {
                string frente = i == 1 ? "PF_Sala_Puerta_Entrada" : i == 3 ? "PF_Sala_Puerta_Salida" : "PF_Sala_Muro";
                string nombre = i == 1 ? "Puerta Entrada" : i == 3 ? "Puerta Salida" : "Muro frente " + (i + 1);
                Place(frente, arq, new Vector3(cs[i], 0, -10.15f), 0f, nombre, "rojo");
                Place("PF_Sala_Muro", arq, new Vector3(cs[i], 0, 10.15f), 180f, "Muro fondo " + (i + 1), "rojo");
                Place("PF_Sala_Muro", arq, new Vector3(-10.15f, 0, cs[i]), 90f, "Muro izq " + (i + 1), "rojo");
                Place("PF_Sala_Muro", arq, new Vector3(10.15f, 0, cs[i]), -90f, "Muro der " + (i + 1), "rojo");
            }
            int n = 0;
            foreach (float x in new[] { -10.05f, 10.05f })
                foreach (float z in new[] { -10.05f, 10.05f })
                    Place("PF_Sala_Esquina", arq, new Vector3(x, 0, z), 0f, "Esquina " + (++n), "rojo");
            // techo: 25 módulos de 4 × 4 m a 4.6 m (sus vigas bajan hasta esa altura; el techo queda a 5.0 m)
            var techo = new GameObject("Techo").transform;
            techo.SetParent(raiz.transform, false);
            n = 0;
            foreach (float x in cs)
                foreach (float z in cs)
                    Place("PF_Sala_Techo", techo, new Vector3(x, 4.6f, z), 0f, "Techo " + (++n), "rojo");

            // mobiliario: bancos rojo (izquierda) y dorado (derecha), con un taburete delante de cada máquina
            var mob = new GameObject("Mobiliario").transform;
            mob.SetParent(raiz.transform, false);
            for (int i = 0; i < 3; i++)
            {
                float z = -4.5f + i * 1.2f;
                Place("PF_Slot_Tres_Rodillos", mob, new Vector3(-6f, 0, z), 90f, "Banco rojo " + (i + 1), "rojo");
                Place("PF_Slot_Tres_Rodillos", mob, new Vector3(6f, 0, z), -90f, "Banco dorado " + (i + 1), "dorado");
                Place("PF_Taburete", mob, new Vector3(-5.08f, 0, z), 0f, "Taburete rojo " + (i + 1), "rojo");
                Place("PF_Taburete", mob, new Vector3(5.08f, 0, z), 0f, "Taburete dorado " + (i + 1), "dorado");
            }
            Place("PF_Mesa_Ruleta", mob, new Vector3(-5.5f, 0, 4f), 90f, "Zona ruleta", "rojo");
            Place("PF_Mesa_Blackjack", mob, new Vector3(5.5f, 0, 4f), -90f, "Mesa blackjack", "dorado");
            Place("PF_Letrero_Jackpot", mob, new Vector3(0, 0, 9f), 180f, "Letrero JACKPOT", "rojo");
            // mostrador de cambio en la esquina de la entrada (izquierda), de frente a la sala
            Place("PF_Mostrador_Cambio", mob, new Vector3(-9.3f, 0, -7.6f), 90f, "Mostrador de cambio", "rojo");

            // referencia humana de 1.80 m junto al pasillo
            var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cap.name = RefName;
            cap.transform.position = new Vector3(-2f, .9f, -6f);
            cap.transform.localScale = new Vector3(.6f, .9f, .6f);
            cap.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(VarianteDir + "/Casino_rojo_madera.mat");

            // cámaras de captura (no build)
            var frontal = Camara("Camara frontal (no build)", new Vector3(0, 1.7f, -8.6f), new Vector3(0, 2.5f, 9f), 75f);
            frontal.tag = "MainCamera";
            Camara("Camara entrada (no build)", new Vector3(-4f, 1.7f, -4f), new Vector3(-4f, 3.7f, -10f), 55f);
            Camara("Camara salida (no build)", new Vector3(4f, 1.7f, -4f), new Vector3(4f, 3.7f, -10f), 55f);
            Camara("Camara mostrador (no build)", new Vector3(-4.5f, 1.7f, -3.5f), new Vector3(-9.3f, 1.3f, -7.6f), 60f);
            Camara("Camara general (no build)", new Vector3(0, 26f, -26f), new Vector3(0, 0, 0), 60f);

            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("No se pudo guardar " + ScenePath);
            AssetDatabase.SaveAssets();
        }

        private static void LuzPunto(string nombre, Vector3 pos, float rango, float intensidad, Color color)
        {
            var go = new GameObject(nombre);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = rango;
            l.intensity = intensidad;
            l.color = color;
        }

        private static Camera Camara(string nombre, Vector3 pos, Vector3 mira, float fov)
        {
            var go = new GameObject(nombre);
            var c = go.AddComponent<Camera>();
            c.fieldOfView = fov;
            go.transform.position = pos;
            go.transform.LookAt(mira);
            return c;
        }

        // ------------------------------------------------------------------ verificación

        public static void Verify()
        {
            var sb = new StringBuilder();
            int fallos = 0, errores = 0;
            void Check(bool ok, string texto)
            {
                sb.AppendLine((ok ? "OK    " : "FALLO ") + texto);
                if (!ok) fallos++;
            }
            Application.LogCallback manejador = (cond, stack, tipo) => { if (tipo == LogType.Error || tipo == LogType.Exception) errores++; };
            Application.logMessageReceived += manejador;

            try
            {
                // 1. importación y prefabs: triángulos de Blender, tamaño y colisión
                string stats = File.ReadAllText(ProjectDir + "/stats_sala.json");
                foreach (var kv in new[] { ("PF_Sala_Muro", "SM_Sala_Muro"), ("PF_Sala_Esquina", "SM_Sala_Esquina"), ("PF_Sala_Puerta_Entrada", "SM_Sala_Puerta_Entrada"),
                                           ("PF_Sala_Puerta_Salida", "SM_Sala_Puerta_Salida"), ("PF_Sala_Techo", "SM_Sala_Techo"), ("PF_Mostrador_Cambio", "SM_Mostrador_Cambio"), ("PF_Taburete", "SM_Taburete") })
                {
                    var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(SalaDir + "/" + kv.Item2 + ".fbx");
                    Check(fbx != null, "FBX " + kv.Item2 + " importa");
                    if (fbx != null)
                    {
                        int tris = fbx.GetComponentsInChildren<MeshFilter>(true).Sum(f => f.sharedMesh.triangles.Length / 3);
                        int trisB = TrisEnBlender(stats, kv.Item2);
                        Check(trisB > 0 && tris == trisB, kv.Item2 + ": " + tris + " triángulos en Unity = " + trisB + " en Blender");
                    }
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SalaDir + "/" + kv.Item1 + ".prefab");
                    Check(prefab != null && prefab.GetComponents<BoxCollider>().Length >= 1, kv.Item1 + ": prefab con colisión");
                    if (prefab == null) continue;
                    var rs = prefab.GetComponentsInChildren<MeshRenderer>(true);
                    Bounds b = rs[0].bounds;
                    foreach (var r in rs) b.Encapsulate(r.bounds);
                    Vector3 e = ExpectedSize[kv.Item1];
                    Check(Mathf.Abs(b.size.x - e.x) < .03f && Mathf.Abs(b.size.y - e.y) < .03f && Mathf.Abs(b.size.z - e.z) < .03f,
                        kv.Item1 + ": tamaño " + b.size.ToString("F3") + " esperado " + e.ToString("F3"));
                    Check(Mathf.Abs(b.min.y) < .01f, kv.Item1 + ": base en y = 0");
                }

                // 2. escena y conteos
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Check(scene.IsValid() && scene.path == ScenePath, "la escena de sala abre");
                Check(!EditorBuildSettings.scenes.Any(s => s.path == ScenePath), "la escena de sala no está en la build");
                var todos = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None);
                int Contar(string prefijo) => todos.Count(t => t.name.StartsWith(prefijo));
                Check(Contar("Muro ") == 18, "18 módulos de muro (" + Contar("Muro ") + ")");
                Check(Contar("Puerta Entrada") == 1 && Contar("Puerta Salida") == 1, "puertas de entrada y de salida presentes");
                Check(Contar("Esquina ") == 4, "4 esquinas (" + Contar("Esquina ") + ")");
                Check(Contar("Techo ") == 25, "25 módulos de techo (" + Contar("Techo ") + ")");
                Check(Contar("Taburete ") == 6, "6 taburetes (" + Contar("Taburete ") + ")");
                Check(Contar("Mostrador de cambio") == 1, "mostrador de cambio presente");
                Check(Contar("Banco rojo") == 3 && Contar("Banco dorado") == 3, "3 máquinas rojas y 3 doradas");
                var arq = GameObject.Find("Arquitectura");
                var mob = GameObject.Find("Mobiliario");
                Check(arq != null && mob != null, "grupos de arquitectura y de mobiliario presentes");

                // 3. recinto cerrado: cada punto del contorno topa con un muro, salvo las dos aberturas de las puertas
                Physics.SyncTransforms();
                int huecosInesperados = 0, huecosPuerta = 0;
                for (float x = -9.25f; x <= 9.25f; x += .5f)   // desplazado .25 m: cae en el centro de cada tramo, no en el borde de una jamba
                {
                    bool enPuerta = Mathf.Abs(Mathf.Abs(x) - 4f) < 1.4f;
                    bool frente = Physics.Raycast(new Vector3(x, 1.5f, -9f), Vector3.back, 2f, ~0, QueryTriggerInteraction.Ignore);
                    if (enPuerta) { if (!frente) huecosPuerta++; } else if (!frente) huecosInesperados++;
                    if (!Physics.Raycast(new Vector3(x, 1.5f, 9.6f), Vector3.forward, 1.5f, ~0, QueryTriggerInteraction.Ignore)) huecosInesperados++;
                    if (!Physics.Raycast(new Vector3(-9.6f, 1.5f, x), Vector3.left, 1.5f, ~0, QueryTriggerInteraction.Ignore)) huecosInesperados++;
                    if (!Physics.Raycast(new Vector3(9.6f, 1.5f, x), Vector3.right, 1.5f, ~0, QueryTriggerInteraction.Ignore)) huecosInesperados++;
                }
                Check(huecosInesperados == 0, "muros cerrados: 0 huecos inesperados en el contorno (" + huecosInesperados + ")");
                Check(huecosPuerta > 0, "las aberturas de las puertas existen (" + huecosPuerta + " rayos pasan)");
                int sinTecho = 0;
                foreach (float x in new[] { -9f, -6f, -3f, 0f, 3f, 6f, 9f })
                    foreach (float z in new[] { -9f, -6f, -3f, 0f, 3f, 6f, 9f })
                        if (!Physics.Raycast(new Vector3(x, 1.5f, z), Vector3.up, 10f, ~0, QueryTriggerInteraction.Ignore)) sinTecho++;
                Check(sinTecho == 0, "techo cerrado: todos los rayos verticales topan (" + sinTecho + " sin techo)");

                // 4. un cuerpo de 1.80 m × 0.60 m pasa por cada puerta y no atraviesa un muro
                foreach (float x in new[] { -4f, 4f })
                {
                    bool golpe = Physics.CapsuleCast(new Vector3(x, .4f, -13f), new Vector3(x, 1.5f, -13f), .3f, Vector3.forward, out RaycastHit h, 7f, ~0, QueryTriggerInteraction.Ignore);
                    Check(!golpe, "un cuerpo de 1.80 m pasa por la puerta de x = " + x + (golpe ? " (choca con " + h.collider.name + ")" : ""));
                }
                bool muroBloquea = Physics.CapsuleCast(new Vector3(0, .4f, -13f), new Vector3(0, 1.5f, -13f), .3f, Vector3.forward, out RaycastHit hm, 7f, ~0, QueryTriggerInteraction.Ignore);
                Check(muroBloquea && hm.distance < 3.3f, "un muro bloquea el paso por x = 0 (distancia al choque " + (muroBloquea ? hm.distance.ToString("F2") : "—") + " m)");

                // 5. pasillo libre y jackpot visible
                var pasillo = new Bounds(new Vector3(0, 1.2f, .4f), new Vector3(9f, 2.4f, 16.8f));
                string ocupante = "";
                foreach (var r in mob != null ? mob.GetComponentsInChildren<MeshRenderer>(true) : new MeshRenderer[0])
                    if (r.bounds.Intersects(pasillo)) { ocupante = r.transform.root.name + "/" + r.name; break; }
                if (ocupante == "" && arq != null)
                    foreach (var r in arq.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        if (r.bounds.max.y < 2.5f && r.bounds.Intersects(pasillo)) { ocupante = r.name; break; }   // la arquitectura bajo 2.5 m no puede cruzar el pasillo
                    }
                Check(ocupante == "", "pasillo libre de x −4.5 a 4.5 y de z −8 a 8.8" + (ocupante == "" ? "" : " (ocupa " + ocupante + ")"));
                var signGo = GameObject.Find("Letrero JACKPOT");
                var frontal = GameObject.FindWithTag("MainCamera")?.GetComponent<Camera>();
                if (signGo != null && frontal != null)
                {
                    Vector3 centro = signGo.transform.position + Vector3.up * 2.05f;
                    Vector3 vp = frontal.WorldToViewportPoint(centro);
                    Check(vp.z > 0 && vp.x > .05f && vp.x < .95f && vp.y > .05f && vp.y < .95f, "el letrero JACKPOT cae dentro del encuadre frontal");
                    Vector3 dir = centro - frontal.transform.position;
                    bool obstruido = Physics.Raycast(frontal.transform.position, dir.normalized, out RaycastHit hit, dir.magnitude - .3f, ~0, QueryTriggerInteraction.Ignore);
                    Check(!obstruido, "nada tapa el letrero desde la cámara frontal" + (obstruido ? " (tapa " + hit.collider.name + ")" : ""));
                }

                // 6. taburetes y mostrador: escala humana y posición
                var tabs = todos.Where(t => t.name.StartsWith("Taburete ")).ToList();
                bool alturaOk = tabs.Count > 0, cercaOk = tabs.Count > 0;
                foreach (var t in tabs)
                {
                    var rs = t.GetComponentsInChildren<MeshRenderer>(true);
                    float h = rs.Max(r => r.bounds.max.y) - rs.Min(r => r.bounds.min.y);
                    if (Mathf.Abs(h - .69f) > .03f) alturaOk = false;
                    float dMin = todos.Where(m => m.name.StartsWith("Banco ")).Min(m => Vector3.Distance(new Vector3(m.position.x, 0, m.position.z), new Vector3(t.position.x, 0, t.position.z)));
                    if (dMin < .6f || dMin > 1.2f) cercaOk = false;
                }
                Check(alturaOk, "los taburetes miden 0.69 m (altura de asiento humana)");
                Check(cercaOk, "cada taburete está a 0.6–1.2 m de su tragamonedas");
                var mostr = GameObject.Find("Mostrador de cambio");
                Check(mostr != null && Vector3.Distance(new Vector3(mostr.transform.position.x, 0, mostr.transform.position.z), new Vector3(-4f, 0, -10f)) < 8f,
                    "el mostrador de cambio está en la esquina de la entrada (a menos de 8 m de la puerta de entrada)");
                Check(mostr != null && Vector3.Dot(mostr.transform.forward, Vector3.right) > .99f, "el mostrador mira a la sala");

                // 7. piezas del mobiliario sin solaparse con la arquitectura
                int solapes = 0;
                var colsMob = mob == null ? new List<BoxCollider>() : mob.GetComponentsInChildren<BoxCollider>(true).ToList();
                var colsArq = arq == null ? new List<BoxCollider>() : arq.GetComponentsInChildren<BoxCollider>(true).ToList();
                foreach (var a in colsMob)
                    foreach (var b in colsArq)
                        if (a.bounds.Intersects(b.bounds)) solapes++;
                Check(solapes == 0, "el mobiliario no se solapa con muros, puertas, esquinas ni techo (" + solapes + " solapes)");

                // 8. capturas tras calentar shaders (techo oculto solo en la vista general)
                Shader.WarmupAllShaders();
                Directory.CreateDirectory(PreviewDir);
                var techoGo = GameObject.Find("Techo");
                var camFrontal = frontal;
                if (camFrontal != null) Check(Capture(camFrontal, PreviewDir + "/Sala_frontal_unity.png", 1600, 900), "captura frontal generada");
                var camEntrada = GameObject.Find("Camara entrada (no build)")?.GetComponent<Camera>();
                var camSalida = GameObject.Find("Camara salida (no build)")?.GetComponent<Camera>();
                if (camEntrada != null && Capture(camEntrada, PreviewDir + "/Sala_entrada_unity.png", 1600, 900))
                    Check(LetrasVisibles(camEntrada, new Vector3(-5.3f, 3.65f, -9.97f), new Vector3(-2.7f, 4.35f, -9.97f), PreviewDir + "/Sala_entrada_unity.png", out string dEnt, true),
                        "el letrero ENTRADA se lee en la captura (" + dEnt + ")");
                if (camSalida != null && Capture(camSalida, PreviewDir + "/Sala_salida_unity.png", 1600, 900))
                    Check(LetrasVisibles(camSalida, new Vector3(2.7f, 3.65f, -9.97f), new Vector3(5.3f, 4.35f, -9.97f), PreviewDir + "/Sala_salida_unity.png", out string dSal, false),
                        "el letrero SALIDA se lee en la captura (" + dSal + ")");
                var camMostr = GameObject.Find("Camara mostrador (no build)")?.GetComponent<Camera>();
                if (camMostr != null) Check(Capture(camMostr, PreviewDir + "/Sala_mostrador_unity.png", 1600, 900), "captura del mostrador generada");
                var camGen = GameObject.Find("Camara general (no build)")?.GetComponent<Camera>();
                if (camGen != null)
                {
                    if (techoGo != null) techoGo.SetActive(false);
                    Check(Capture(camGen, PreviewDir + "/Sala_general_unity.png", 1600, 900), "captura general (sin techo) generada");
                    if (techoGo != null) techoGo.SetActive(true);
                }
            }
            catch (Exception e)
            {
                sb.AppendLine("ERROR " + e.Message);
                fallos++;
            }
            Application.logMessageReceived -= manejador;
            sb.AppendLine((errores == 0 ? "OK    " : "FALLO ") + "consola sin errores durante la verificación (" + errores + ")");
            if (errores != 0) fallos++;

            File.WriteAllText(ReportPath, sb.ToString(), new UTF8Encoding(false));
            Debug.Log(sb.ToString());
            if (fallos == 0) Debug.Log("TYCOON_CASINO_SALA_PREVIEW_OK: recinto cerrado, puertas, techo, mostrador, taburetes, pasillo, jackpot y capturas comprobados.");
            else Debug.LogError("TYCOON_CASINO_SALA_PREVIEW_FALLO: " + fallos + " problema(s); ver " + ReportPath);
            if (Application.isBatchMode) EditorApplication.delayCall += () => EditorApplication.Exit(fallos == 0 ? 0 : 1);
        }

        /// <summary>
        /// Letras en la captura: píxeles blancos dentro de la proyección del letrero (aspecto 16:9 de la captura), sobre un
        /// tablero del color propio de la puerta (verde para ENTRADA).
        /// </summary>
        private static bool LetrasVisibles(Camera cam, Vector3 min, Vector3 max, string path, out string detalle, bool verde)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            tex.LoadImage(File.ReadAllBytes(path));
            float aspectoAntes = cam.aspect;
            cam.aspect = (float)tex.width / tex.height;
            var esquinas = new[] { new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z), new Vector3(min.x, max.y, min.z), new Vector3(max.x, max.y, min.z) };
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            foreach (var e in esquinas)
            {
                Vector3 vp = cam.WorldToViewportPoint(e);
                minX = Mathf.Min(minX, vp.x * tex.width); maxX = Mathf.Max(maxX, vp.x * tex.width);
                minY = Mathf.Min(minY, vp.y * tex.height); maxY = Mathf.Max(maxY, vp.y * tex.height);
            }
            cam.aspect = aspectoAntes;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(minX), 0, tex.width - 1), x1 = Mathf.Clamp(Mathf.CeilToInt(maxX), 0, tex.width - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(minY), 0, tex.height - 1), y1 = Mathf.Clamp(Mathf.CeilToInt(maxY), 0, tex.height - 1);
            Color[] px = tex.GetPixels(x0, y0, Mathf.Max(1, x1 - x0), Mathf.Max(1, y1 - y0));
            int blanco = 0, tablero = 0;
            foreach (var c in px)
            {
                float mx = Mathf.Max(c.r, Mathf.Max(c.g, c.b)), mn = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                if (mn > .45f && mx - mn < .14f) blanco++;                                     // letra: clara y casi gris
                else if (verde && c.g > c.r + .05f && c.g > c.b + .03f) tablero++;             // tablero verde
                else if (!verde && c.r > .2f && c.g < .15f && c.b < .15f) tablero++;           // tablero rojo
            }
            float fb = px.Length > 0 ? (float)blanco / px.Length : 0f, ft = px.Length > 0 ? (float)tablero / px.Length : 0f;
            detalle = "letras blancas " + blanco + " px (" + (fb * 100f).ToString("F1") + "%), tablero " + (ft * 100f).ToString("F1") + "%";
            UnityEngine.Object.DestroyImmediate(tex);
            return blanco >= 250 && fb >= .04f && fb <= .6f && ft >= .15f;
        }

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
                cam.Render();
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
