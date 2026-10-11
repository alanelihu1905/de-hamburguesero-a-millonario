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
    /// Escena de integración PREVIEW del interior del casino (aislada): bancos rojo y dorado separados por un pasillo amplio,
    /// zona de ruleta, blackjack, letrero JACKPOT grande al fondo y una cápsula humana de 1.80 m como referencia.
    /// ARTE, NO MECÁNICA: no hay apuestas, pagos ni RNG; no toca Assets/_Game/Scripts/Casino ni escenas compartidas.
    /// Uso en batch:
    ///   -executeMethod Tycoon3D.Editor.PuertoAlbaCasinoIntegracionBuilder.Build
    ///   -executeMethod Tycoon3D.Editor.PuertoAlbaCasinoIntegracionBuilder.Verify
    /// Informe: ArtSource/PuertoAlba/Casino/unity_integracion_check.txt
    /// </summary>
    public static class PuertoAlbaCasinoIntegracionBuilder
    {
        public const string ModelDir = "Assets/_Game/Models/PuertoAlba/Casino";
        public const string PrefabDir = ModelDir + "/Prefabs";
        public const string VarianteDir = ModelDir + "/Variantes";
        public const string ScenePath = "Assets/_Game/Scenes/PuertoAlba_CasinoIntegracionPreview.unity";
        private const string ProjectDir = "ArtSource/PuertoAlba/Casino";
        private const string PreviewDir = "ArtSource/PuertoAlba/Preview/Unity";
        private const string ReportPath = ProjectDir + "/unity_integracion_check.txt";
        private const string RootName = "Casino integracion";
        private const string RefName = "Referencia humana 1.80 m (no build)";
        private const string SignName = "Letrero JACKPOT";

        private static readonly string[] Roles = { "cuerpo", "acento", "fieltro", "madera" };
        private static readonly Dictionary<string, string> MaterialARol = new Dictionary<string, string>
        {
            { "M_Casino_Rojo", "cuerpo" }, { "M_Casino_Oro", "acento" }, { "M_Fieltro", "fieltro" }, { "M_Madera", "madera" },
        };

        // tamaños esperados en Unity (X, Y alto, Z fondo) del letrero y de las piezas del interior
        private static readonly Dictionary<string, Vector3> ExpectedSize = new Dictionary<string, Vector3>
        {
            { "PF_Letrero_Jackpot", new Vector3(10.8f, 4.1f, .205f) },
        };

        // ------------------------------------------------------------------ construcción

        public static void Build()
        {
            try
            {
                MakePrefab("PF_Letrero_Jackpot", "SM_Letrero_Jackpot", new[]
                {
                    Box(new Vector3(0, 3.0f, 0), new Vector3(10.8f, 2.2f, .14f)),      // tablero
                    Box(new Vector3(-4.5f, 1.0f, 0), new Vector3(.2f, 2f, .2f)),       // pata izquierda
                    Box(new Vector3(4.5f, 1.0f, 0), new Vector3(.2f, 2f, .2f)),        // pata derecha
                });
                BuildScene();
                Debug.Log("CASINO_INTEGRACION_BUILD_OK: escena " + ScenePath);
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

        private static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // luces y suelo de vista previa (no forman parte de la build)
            var sol = new GameObject("Luz de vista previa (no build)").AddComponent<Light>();
            sol.type = LightType.Directional;
            sol.intensity = .8f;
            sol.transform.rotation = Quaternion.Euler(50, 30, 0);
            LuzPunto("Luz sala izquierda (no build)", new Vector3(-3, 4.5f, 2), 16f, 2f, Color.white);
            LuzPunto("Luz sala derecha (no build)", new Vector3(3, 4.5f, 2), 16f, 2f, Color.white);
            LuzPunto("Luz letrero (no build)", new Vector3(0, 3.0f, 5f), 8f, 3f, new Color(1f, .8f, .45f));

            var suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            suelo.name = "Suelo de vista previa (no build)";
            suelo.transform.localScale = new Vector3(3.5f, 1f, 3.5f);   // 35 m × 35 m
            suelo.transform.position = new Vector3(0, 0, 2);

            var raiz = new GameObject(RootName);
            // bancos separados por un pasillo de 9 m; frente de cada máquina hacia el pasillo
            for (int i = 0; i < 3; i++)
            {
                float z = -4.5f + i * 1.2f;
                Place("PF_Slot_Tres_Rodillos", raiz.transform, new Vector3(-6f, 0, z), 90f, "Banco rojo " + (i + 1), "rojo");
                Place("PF_Slot_Tres_Rodillos", raiz.transform, new Vector3(6f, 0, z), -90f, "Banco dorado " + (i + 1), "dorado");
            }
            Place("PF_Mesa_Ruleta", raiz.transform, new Vector3(-5.5f, 0, 4f), 90f, "Zona ruleta", "rojo");
            Place("PF_Mesa_Blackjack", raiz.transform, new Vector3(5.5f, 0, 4f), -90f, "Mesa blackjack", "dorado");
            // letrero JACKPOT al fondo del pasillo, de frente a la entrada (−Z)
            Place("PF_Letrero_Jackpot", raiz.transform, new Vector3(0, 0, 9f), 180f, SignName, "rojo");

            // referencia humana de 1.80 m, junto al pasillo y fuera de la línea de visión a la cámara frontal
            var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cap.name = RefName;
            cap.transform.position = new Vector3(-2f, .9f, -6f);
            cap.transform.localScale = new Vector3(.6f, .9f, .6f);   // cápsula de 2 m de alto → 1.80 m
            cap.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(VarianteDir + "/Casino_rojo_madera.mat");

            // cámaras de captura (no build)
            var frontal = Camara("Camara frontal (no build)", new Vector3(0, 1.7f, -14f), new Vector3(0, 2.2f, 9f), 55f);
            Camara("Camara general (no build)", new Vector3(0, 14f, -24f), new Vector3(0, 1f, 2f), 60f);
            frontal.tag = "MainCamera";

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

        private static void Place(string prefabName, Transform parent, Vector3 pos, float yaw, string nombre, string variante)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/" + prefabName + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Falta el prefab " + prefabName);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = nombre;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            ApplyVariante(go, variante);
        }

        /// <summary>Sustituye los materiales base por los de la variante rojo o dorado.</summary>
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

                // 1. letrero: FBX importa, triángulos y tamaño de Blender, y prefab con colisión
                AssetDatabase.ImportAsset(ModelDir + "/SM_Letrero_Jackpot.fbx", ImportAssetOptions.ForceUpdate);
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + "/SM_Letrero_Jackpot.fbx");
                Check(fbx != null, "FBX SM_Letrero_Jackpot importa");
                if (fbx != null)
                {
                    int tris = 0;
                    foreach (var f in fbx.GetComponentsInChildren<MeshFilter>(true)) tris += f.sharedMesh.triangles.Length / 3;
                    int trisB = TrisEnBlender(stats, "SM_Letrero_Jackpot");
                    Check(trisB > 0 && tris == trisB, "SM_Letrero_Jackpot: " + tris + " triángulos en Unity = " + trisB + " en Blender");
                }
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/PF_Letrero_Jackpot.prefab");
                Check(prefab != null, "prefab PF_Letrero_Jackpot existe");
                if (prefab != null)
                {
                    Check(prefab.GetComponents<BoxCollider>().Length == 3, "PF_Letrero_Jackpot: 3 colisiones simples");
                    var rs = prefab.GetComponentsInChildren<MeshRenderer>(true);
                    Bounds b = rs[0].bounds;
                    foreach (var r in rs) b.Encapsulate(r.bounds);
                    Vector3 s = b.size, e = ExpectedSize["PF_Letrero_Jackpot"];
                    Check(Mathf.Abs(s.x - e.x) < .03f && Mathf.Abs(s.y - e.y) < .03f && Mathf.Abs(s.z - e.z) < .03f,
                        "PF_Letrero_Jackpot: tamaño " + s.ToString("F3") + " esperado " + e.ToString("F3"));
                }

                // 2. escena aislada: abre, no está en la build, y tiene las piezas esperadas
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Check(scene.IsValid() && scene.path == ScenePath, "la escena de integración abre");
                Check(!EditorBuildSettings.scenes.Any(s => s.path == ScenePath), "la escena de integración no está en la build");
                var root = GameObject.Find(RootName);
                Check(root != null, "raíz del casino presente");
                var props = root == null ? new List<Transform>() : root.GetComponentsInChildren<Transform>(false).Where(t => t.parent == root.transform).ToList();
                int bancoRojo = props.Count(t => t.name.StartsWith("Banco rojo")), bancoDorado = props.Count(t => t.name.StartsWith("Banco dorado"));
                Check(bancoRojo == 3 && bancoDorado == 3, "bancos: 3 rojos y 3 dorados (" + bancoRojo + " y " + bancoDorado + ")");
                Check(props.Any(t => t.name == "Zona ruleta") && props.Any(t => t.name == "Mesa blackjack"), "zona de ruleta y mesa de blackjack presentes");
                var signTr = props.FirstOrDefault(t => t.name == SignName);
                Check(signTr != null && PrefabUtility.GetPrefabInstanceStatus(signTr.gameObject) == PrefabInstanceStatus.Connected, "letrero JACKPOT es instancia de prefab");
                var ref_ = GameObject.Find(RefName);
                Check(ref_ != null, "referencia humana presente");

                // 3. geometría: pasillo libre, bancos separados, escala humana, colisiones sin solaparse y letrero visible
                var mallasProps = root == null ? new Renderer[0] : root.GetComponentsInChildren<MeshRenderer>(true);
                var pasillo = new Bounds(new Vector3(0, 1.2f, .4f), new Vector3(9f, 2.4f, 16.8f));   // x −4.5…4.5, z −8…8.8
                string ocupante = "";
                foreach (var r in mallasProps)
                    if (r.bounds.Intersects(pasillo)) { ocupante = r.transform.root.name + "/" + r.name; break; }
                Check(ocupante == "", "pasillo libre de x −4.5 a 4.5 y de z −8 a 8.8" + (ocupante == "" ? "" : " (ocupa " + ocupante + ")"));

                float izqMax = float.MinValue, derMin = float.MaxValue;
                foreach (var t in props)
                {
                    if (!t.name.StartsWith("Banco")) continue;
                    foreach (var r in t.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        if (t.name.StartsWith("Banco rojo")) izqMax = Mathf.Max(izqMax, r.bounds.max.x);
                        else derMin = Mathf.Min(derMin, r.bounds.min.x);
                    }
                }
                float separacion = derMin - izqMax;
                Check(separacion >= 8f, "bancos separados por un pasillo de " + separacion.ToString("F2") + " m (mínimo 8 m)");

                var refR = ref_ == null ? null : ref_.GetComponent<Renderer>();
                float alturaRef = refR == null ? 0f : refR.bounds.size.y;
                Check(Mathf.Abs(alturaRef - 1.8f) < .02f, "cápsula de referencia mide " + alturaRef.ToString("F3") + " m (1.80 m)");
                var slot = props.FirstOrDefault(t => t.name == "Banco rojo 1");
                float alturaSlot = slot == null ? 0f : slot.GetComponentsInChildren<MeshRenderer>(true).Select(r => r.bounds.max.y).Max()
                    - slot.GetComponentsInChildren<MeshRenderer>(true).Select(r => r.bounds.min.y).Min();
                Check(Mathf.Abs(alturaSlot - 1.8f) < .03f, "tragamonedas mide " + alturaSlot.ToString("F3") + " m, escala humana frente a la cápsula");

                // colisiones de props sin solapamiento entre piezas distintas
                var cols = props.SelectMany(t => t.GetComponentsInChildren<BoxCollider>(true).Select(c => new { t, c })).ToList();
                Physics.SyncTransforms();
                int solapes = 0;
                for (int i = 0; i < cols.Count; i++)
                    for (int j = i + 1; j < cols.Count; j++)
                        if (cols[i].t != cols[j].t && cols[i].c.bounds.Intersects(cols[j].c.bounds)) solapes++;
                Check(cols.Count > 0 && solapes == 0, "colisiones de piezas distintas sin solaparse (" + cols.Count + " colisiones, " + solapes + " solapes)");

                var frontal = GameObject.FindWithTag("MainCamera")?.GetComponent<Camera>();
                Check(frontal != null, "cámara frontal presente");
                if (signTr != null && frontal != null)
                {
                    Vector3 centro = signTr.position + Vector3.up * 2.05f;
                    Vector3 vp = frontal.WorldToViewportPoint(centro);
                    Check(vp.z > 0 && vp.x > .05f && vp.x < .95f && vp.y > .05f && vp.y < .95f, "el letrero JACKPOT cae dentro del encuadre frontal");
                    Vector3 dir = centro - frontal.transform.position;
                    bool obstruido = Physics.Raycast(frontal.transform.position, dir.normalized, out RaycastHit hit, dir.magnitude - .3f, ~0, QueryTriggerInteraction.Ignore);
                    Check(!obstruido, "nada tapa el letrero desde la cámara frontal" + (obstruido ? " (tapa " + hit.collider.name + ")" : ""));
                }

                // 4. capturas tras calentar shaders (dos renders por captura)
                Shader.WarmupAllShaders();
                Directory.CreateDirectory(PreviewDir);
                if (frontal != null)
                {
                    string ruta = PreviewDir + "/Casino_integracion_frontal_unity.png";
                    bool ok = Capture(frontal, ruta, 1600, 900);
                    Check(ok, "captura frontal generada");
                    if (ok && signTr != null)
                    {
                        Check(LetrasVisibles(frontal, signTr.position, ruta, out string detalle), "el letrero JACKPOT muestra letras en la captura frontal (" + detalle + ")");
                        var tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                        tex.LoadImage(File.ReadAllBytes(ruta));
                        Color[] px = tex.GetPixels();
                        int oro = px.Count(c => c.r > .75f && c.g > .55f && c.b < .4f && c.r - c.b > .4f);
                        int rojo = px.Count(c => c.r > .35f && c.g < .2f && c.b < .25f);
                        Check(oro > px.Length * .002f, "captura frontal con dorado (" + (100f * oro / px.Length).ToString("F2") + "%)");
                        Check(rojo > px.Length * .02f, "captura frontal con rojo (" + (100f * rojo / px.Length).ToString("F2") + "%)");
                        UnityEngine.Object.DestroyImmediate(tex);
                    }
                }
                var general = GameObject.Find("Camara general (no build)")?.GetComponent<Camera>();
                if (general != null)
                {
                    bool ok = Capture(general, PreviewDir + "/Casino_integracion_general_unity.png", 1600, 900);
                    Check(ok, "captura general generada");
                }
            }
            catch (Exception e)
            {
                sb.AppendLine("ERROR " + e.Message);
                fallos++;
            }

            File.WriteAllText(ReportPath, sb.ToString(), new UTF8Encoding(false));
            Debug.Log(sb.ToString());
            if (fallos == 0) Debug.Log("TYCOON_CASINO_INTEGRACION_PREVIEW_OK: escena, pasillos, escala, colisiones, letrero y capturas comprobados.");
            else Debug.LogError("TYCOON_CASINO_INTEGRACION_PREVIEW_FALLO: " + fallos + " problema(s); ver " + ReportPath);
            if (Application.isBatchMode) EditorApplication.delayCall += () => EditorApplication.Exit(fallos == 0 ? 0 : 1);
        }

        /// <summary>Letras del JACKPOT en la captura: píxeles ámbar dentro de la proyección del letrero (aspecto 16:9 de la captura).</summary>
        private static bool LetrasVisibles(Camera cam, Vector3 origen, string path, out string detalle)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            tex.LoadImage(File.ReadAllBytes(path));
            var esquinas = new[]
            {
                new Vector3(-5.4f, 1.9f, 9f), new Vector3(5.4f, 1.9f, 9f),
                new Vector3(-5.4f, 4.1f, 9f), new Vector3(5.4f, 4.1f, 9f),
            };
            float aspectoAntes = cam.aspect;
            cam.aspect = (float)tex.width / tex.height;
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
            int ambar = 0, oscuro = 0;
            foreach (var c in px)
            {
                if (c.r > .75f && c.r - c.b > .45f && c.g > .35f) ambar++;
                else if (c.r > .4f && c.g < .2f && c.b < .25f) oscuro++;   // tablero rojo del letrero
            }
            float fa = px.Length > 0 ? (float)ambar / px.Length : 0f, fo = px.Length > 0 ? (float)oscuro / px.Length : 0f;
            detalle = "ámbar " + ambar + " px (" + (fa * 100f).ToString("F1") + "%), tablero rojo " + (fo * 100f).ToString("F1") + "%";
            UnityEngine.Object.DestroyImmediate(tex);
            return ambar >= 400 && fa >= .03f && fa <= .6f && fo >= .2f;   // letras sobre tablero rojo
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
