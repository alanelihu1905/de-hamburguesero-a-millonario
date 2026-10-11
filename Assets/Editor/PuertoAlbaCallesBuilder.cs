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
    /// Escena de calles PREVIEW de Puerto Alba (aislada): una avenida este–oeste, una calle norte–sur que la cruza,
    /// aceras, pasos de cebra, farolas y edificios del kit con el frente hacia la calle.
    /// No cambia CityMap ni CityLayout, no coloca el casino (decisión pendiente, handoff 11) y no está en la build.
    /// Uso en batch:
    ///   -executeMethod Tycoon3D.Editor.PuertoAlbaCallesBuilder.Build
    ///   -executeMethod Tycoon3D.Editor.PuertoAlbaCallesBuilder.Verify
    /// Informe: ArtSource/PuertoAlba/unity_calles_check.txt
    /// </summary>
    public static class PuertoAlbaCallesBuilder
    {
        public const string PrefabDir = "Assets/_Game/Models/PuertoAlba/Prefabs";
        public const string ScenePath = "Assets/_Game/Scenes/PuertoAlba_CallesPreview.unity";
        public const string MatDir = "Assets/_Game/Models/PuertoAlba/Calles/Materiales";
        private const string PreviewDir = "ArtSource/PuertoAlba/Preview/Unity";
        private const string ReportPath = "ArtSource/PuertoAlba/unity_calles_check.txt";
        private const string Root = "Calles y edificios";

        // zonas libres de la vía: la calzada más las aceras. Ningún edificio puede entrar en ellas.
        private static readonly Rect AvenidaLibre = new Rect(-60f, -6f, 120f, 12f);   // x −60…60, z −6…6
        private static readonly Rect CruceLibre = new Rect(-6f, -40f, 12f, 80f);      // x −6…6, z −40…40

        private struct Pieza { public string Prefab, Nombre, Tipo; public Vector3 Pos; public float Yaw; }

        // edificios: norte (z > 0, frente a −Z), sur (z < 0, frente a +Z), y los de la calle cruzada (frente hacia la calle)
        private static readonly Pieza[] Edificios =
        {
            new Pieza { Prefab = "PF_Casa_Dos_Plantas", Nombre = "Edif avenida norte 1", Tipo = "Avenida", Pos = new Vector3(-44, 0, 9.7f), Yaw = 180 },
            new Pieza { Prefab = "PF_Fachada_Residencial", Nombre = "Edif avenida norte 2", Tipo = "Avenida", Pos = new Vector3(-34, 0, 6.56f), Yaw = 180 },
            new Pieza { Prefab = "PF_Tienda_Barrio", Nombre = "Edif avenida norte 3", Tipo = "Avenida", Pos = new Vector3(-24, 0, 8.7f), Yaw = 180 },
            new Pieza { Prefab = "PF_Fachada_Residencial_B", Nombre = "Edif avenida norte 4", Tipo = "Avenida", Pos = new Vector3(-12, 0, 6.7f), Yaw = 180 },
            new Pieza { Prefab = "PF_Taller_Brisa", Nombre = "Edif avenida norte 5", Tipo = "Avenida", Pos = new Vector3(14, 0, 9.4f), Yaw = 180 },
            new Pieza { Prefab = "PF_Mercado_Pabellon_Reloj", Nombre = "Edif avenida norte 6", Tipo = "Avenida", Pos = new Vector3(30, 0, 10.5f), Yaw = 180 },
            new Pieza { Prefab = "PF_Gasolinera_Completa", Nombre = "Edif avenida norte 7", Tipo = "Avenida", Pos = new Vector3(46, 0, 10.2f), Yaw = 180 },
            new Pieza { Prefab = "PF_Fachada_Residencial_B", Nombre = "Edif avenida sur 1", Tipo = "Avenida", Pos = new Vector3(-46, 0, -6.7f), Yaw = 0 },
            new Pieza { Prefab = "PF_Tienda_Barrio", Nombre = "Edif avenida sur 2", Tipo = "Avenida", Pos = new Vector3(-36, 0, -8.7f), Yaw = 0 },
            new Pieza { Prefab = "PF_Casa_Dos_Plantas", Nombre = "Edif avenida sur 3", Tipo = "Avenida", Pos = new Vector3(-24, 0, -9.7f), Yaw = 0 },
            new Pieza { Prefab = "PF_Fachada_Residencial", Nombre = "Edif avenida sur 4", Tipo = "Avenida", Pos = new Vector3(-14, 0, -6.56f), Yaw = 0 },
            new Pieza { Prefab = "PF_Fachada_Residencial_B", Nombre = "Edif avenida sur 5", Tipo = "Avenida", Pos = new Vector3(12, 0, -6.7f), Yaw = 0 },
            new Pieza { Prefab = "PF_Tienda_Barrio", Nombre = "Edif avenida sur 6", Tipo = "Avenida", Pos = new Vector3(24, 0, -8.7f), Yaw = 0 },
            new Pieza { Prefab = "PF_Casa_Dos_Plantas", Nombre = "Edif avenida sur 7", Tipo = "Avenida", Pos = new Vector3(36, 0, -9.7f), Yaw = 0 },
            new Pieza { Prefab = "PF_Fachada_Residencial", Nombre = "Edif avenida sur 8", Tipo = "Avenida", Pos = new Vector3(48, 0, -6.56f), Yaw = 0 },
            new Pieza { Prefab = "PF_Casa_Dos_Plantas", Nombre = "Edif cruce este 1", Tipo = "Cruce", Pos = new Vector3(11.5f, 0, 24), Yaw = -90 },
            new Pieza { Prefab = "PF_Taller_Brisa", Nombre = "Edif cruce oeste 1", Tipo = "Cruce", Pos = new Vector3(-11.5f, 0, 24), Yaw = 90 },
            new Pieza { Prefab = "PF_Tienda_Barrio", Nombre = "Edif cruce oeste 2", Tipo = "Cruce", Pos = new Vector3(-11.5f, 0, -24), Yaw = 90 },
            new Pieza { Prefab = "PF_Mercado_Pabellon_Reloj", Nombre = "Edif cruce este 2", Tipo = "Cruce", Pos = new Vector3(11.5f, 0, -36), Yaw = -90 },
            new Pieza { Prefab = "PF_Centro_Torre", Nombre = "Torre de oficinas", Tipo = "Libre", Pos = new Vector3(-26, 0, 28), Yaw = 0 },
            new Pieza { Prefab = "PF_Parque_Quiosco", Nombre = "Quiosco de plaza", Tipo = "Libre", Pos = new Vector3(22, 0, 16), Yaw = 0 },
        };

        // ------------------------------------------------------------------ construcción

        public static void Build()
        {
            try
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Game/Models/PuertoAlba/Calles")) AssetDatabase.CreateFolder("Assets/_Game/Models/PuertoAlba", "Calles");
                if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/_Game/Models/PuertoAlba/Calles", "Materiales");
                var asfalto = Mat("Calle asfalto", new Color(.25f, .25f, .27f));
                var acera = Mat("Calle acera", new Color(.72f, .70f, .66f));
                var bordillo = Mat("Calle bordillo", new Color(.92f, .92f, .9f));
                var pintura = Mat("Calle pintura", new Color(.95f, .95f, .93f));
                var poste = Mat("Calle poste", new Color(.2f, .2f, .22f));
                var luz = Mat("Calle luz", new Color(1f, .9f, .6f));
                var suelo = Mat("Calle terreno", new Color(.55f, .62f, .5f));

                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                var sol = new GameObject("Luz de vista previa (no build)").AddComponent<Light>();
                sol.type = LightType.Directional;
                sol.intensity = 1.1f;
                sol.transform.rotation = Quaternion.Euler(50, 30, 0);

                var terreno = Cubo("Terreno (no build)", new Vector3(0, -.05f, 0), new Vector3(200, .1f, 200), suelo, null);

                var calles = new GameObject("Calles");
                Cubo("Calle avenida", new Vector3(0, 0, 0), new Vector3(120, .02f, 8), asfalto, calles.transform);
                Cubo("Calle cruce", new Vector3(0, 0, 0), new Vector3(8, .02f, 80), asfalto, calles.transform);
                // aceras a los lados de la avenida
                Cubo("Acera avenida norte", new Vector3(0, .05f, 5), new Vector3(120, .1f, 2), acera, calles.transform);
                Cubo("Acera avenida sur", new Vector3(0, .05f, -5), new Vector3(120, .1f, 2), acera, calles.transform);
                // aceras de la calle cruzada, solo fuera de la avenida (z de 4 a 40 y de −4 a −40)
                foreach (float x in new[] { -5f, 5f })
                    foreach (float z in new[] { -22f, 22f })
                        Cubo("Acera cruce " + (x < 0 ? "oeste" : "este") + " " + (z > 0 ? "norte" : "sur"),
                            new Vector3(x, .05f, z), new Vector3(2, .1f, 36), acera, calles.transform);
                // bordillos (líneas blancas) junto a las aceras
                Cubo("Bordillo avenida norte", new Vector3(0, .06f, 4f), new Vector3(120, .12f, .15f), bordillo, calles.transform);
                Cubo("Bordillo avenida sur", new Vector3(0, .06f, -4f), new Vector3(120, .12f, .15f), bordillo, calles.transform);
                // pasos de cebra sobre la calle cruzada, al norte y al sur de la avenida
                foreach (float z0 in new[] { 11f, -11f })
                    for (int i = 0; i < 4; i++)
                        Cubo("Paso de cebra " + (z0 > 0 ? "norte" : "sur") + " " + (i + 1),
                            new Vector3(0, .03f, z0 + i * .8f), new Vector3(8, .02f, .45f), pintura, calles.transform);
                // farolas en ambas aceras de la avenida
                for (int i = -4; i <= 4; i++)
                    foreach (float z in new[] { -5f, 5f })
                    {
                        float x = i * 12f;
                        var p = Cubo("Farola " + i + (z > 0 ? " norte" : " sur"), new Vector3(x, 2f, z), new Vector3(.16f, 4f, .16f), poste, calles.transform);
                        Cubo("Luz farola " + i + (z > 0 ? " norte" : " sur"), new Vector3(x, 4.1f, z), new Vector3(.4f, .25f, .4f), luz, calles.transform);
                        if (p.GetComponent<BoxCollider>() != null) UnityEngine.Object.DestroyImmediate(p.GetComponent<BoxCollider>());
                    }

                var raiz = new GameObject(Root);
                foreach (var e in Edificios)
                {
                    var go = Place(e.Prefab, raiz.transform, e.Pos, e.Yaw, e.Nombre + " [" + e.Tipo + "]");
                    Apoyar(go, e.Tipo, e.Yaw);
                }

                var camCalle = Camara("Camara calle (no build)", new Vector3(-30, 1.7f, -2.5f), new Vector3(10, 2.5f, 1.5f), 60f);
                camCalle.tag = "MainCamera";
                Camara("Camara general (no build)", new Vector3(0, 70, -90), new Vector3(0, 0, 4), 60f);

                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("No se pudo guardar " + ScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log("CALLES_BUILD_OK: escena " + ScenePath);
                if (Application.isBatchMode) EditorApplication.delayCall += () => EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.delayCall += () => EditorApplication.Exit(1);
            }
        }

        private static GameObject Cubo(string nombre, Vector3 pos, Vector3 escala, Material mat, Transform padre)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            if (padre != null) go.transform.SetParent(padre, false);
            go.transform.position = pos;
            go.transform.localScale = escala;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
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

        /// <summary>
        /// Desplaza el edificio para que su cara más cercana a la calle quede a la distancia de retranqueo.
        /// Los prefabs no están centrados en su fondo (la casa tiene el balcón hacia el frente), así que se mide la caja real.
        /// </summary>
        private static void Apoyar(GameObject go, string tipo, float yaw)
        {
            const float Retranqueo = 6.3f;   // justo fuera de la acera (que llega a 6 m)
            var rs = go.GetComponentsInChildren<MeshRenderer>(true);
            if (rs.Length == 0) return;
            Bounds b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            Vector3 p = go.transform.position;
            if (tipo == "Avenida")
            {
                if (p.z > 0) p.z += Retranqueo - b.min.z;     // norte: la cara más baja de z toca el retranqueo
                else p.z += -Retranqueo - b.max.z;           // sur: la cara más alta de z toca el retranqueo
            }
            else if (tipo == "Cruce")
            {
                if (p.x > 0) p.x += Retranqueo - b.min.x;     // este
                else p.x += -Retranqueo - b.max.x;           // oeste
            }
            go.transform.position = p;
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

        private static Material Mat(string nombre, Color color)
        {
            string path = MatDir + "/" + nombre.Replace(' ', '_') + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Falta URP/Lit.");
            if (m == null)
            {
                m = new Material(shader) { name = nombre };
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(m);
            return m;
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
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Check(scene.IsValid() && scene.path == ScenePath, "la escena de calles abre");
                Check(!EditorBuildSettings.scenes.Any(s => s.path == ScenePath), "la escena de calles no está en la build");

                var calleAv = GameObject.Find("Calle avenida");
                var calleCr = GameObject.Find("Calle cruce");
                Check(calleAv != null && Mathf.Abs(calleAv.transform.localScale.z - 8f) < .01f && Mathf.Abs(calleAv.transform.localScale.x - 120f) < .01f,
                    "avenida de 120 m × 8 m");
                Check(calleCr != null && Mathf.Abs(calleCr.transform.localScale.x - 8f) < .01f && Mathf.Abs(calleCr.transform.localScale.z - 80f) < .01f,
                    "calle cruzada de 8 m × 80 m");
                Check(calleAv != null && calleAv.GetComponent<BoxCollider>() != null && calleCr != null && calleCr.GetComponent<BoxCollider>() != null,
                    "las dos calles son transitables (tienen colisión)");

                int cebras = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.name.StartsWith("Paso de cebra"));
                Check(cebras == 8, "8 franjas de paso de cebra (" + cebras + ")");
                int farolas = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.name.StartsWith("Farola "));
                Check(farolas == 18, "18 farolas (" + farolas + ")");

                // edificios: cada uno es instancia de prefab, no invade la avenida ni el cruce, y su frente mira a la calle
                var raiz = GameObject.Find(Root);
                var edificios = raiz == null ? new List<Transform>() : raiz.GetComponentsInChildren<Transform>(false).Where(t => t.parent == raiz.transform).ToList();
                Check(edificios.Count == Edificios.Length, edificios.Count + " edificios y piezas en la escena (esperadas " + Edificios.Length + ")");
                foreach (var t in edificios)
                {
                    bool prefab = PrefabUtility.GetPrefabInstanceStatus(t.gameObject) == PrefabInstanceStatus.Connected;
                    Check(prefab, t.name + ": es instancia de prefab");
                    var rs = t.GetComponentsInChildren<MeshRenderer>(true);
                    if (rs.Length == 0) { Check(false, t.name + ": sin mallas"); continue; }
                    Bounds b = rs[0].bounds;
                    foreach (var r in rs) b.Encapsulate(r.bounds);
                    bool invade = BoundsCruzaRect(b, AvenidaLibre) || BoundsCruzaRect(b, CruceLibre);
                    Check(!invade, t.name + ": fuera de la avenida y del cruce" + (invade ? " (INVADE)" : ""));
                    Check(Mathf.Abs(b.min.y) < .01f, t.name + ": base en el suelo");

                    // frente hacia la calle: los edificios de la avenida miran hacia z = 0; los del cruce, hacia x = 0
                    var tipo = Edificios.FirstOrDefault(e => e.Nombre == t.name.Split(new[] { " [" }, StringSplitOptions.None)[0]).Tipo;
                    Vector3 hacia;
                    if (tipo == "Avenida") hacia = new Vector3(0, 0, -Mathf.Sign(t.position.z));
                    else if (tipo == "Cruce") hacia = new Vector3(-Mathf.Sign(t.position.x), 0, 0);
                    else hacia = Vector3.zero;
                    if (hacia != Vector3.zero)
                    {
                        float d = Vector3.Dot(t.forward, hacia);
                        Check(d > .99f, t.name + ": frente hacia la calle (producto punto " + d.ToString("F3") + ")");
                    }
                }

                // cámara de calle: capturas tras calentar shaders (dos renders)
                Shader.WarmupAllShaders();
                Directory.CreateDirectory(PreviewDir);
                var cam = GameObject.FindWithTag("MainCamera")?.GetComponent<Camera>();
                Check(cam != null, "cámara de calle presente");
                if (cam != null)
                {
                    string ruta = PreviewDir + "/Calles_calle_unity.png";
                    bool ok = Capture(cam, ruta, 1600, 900);
                    Check(ok, "captura de calle generada");
                    if (ok)
                    {
                        var tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                        tex.LoadImage(File.ReadAllBytes(ruta));
                        Color[] px = tex.GetPixels();
                        int asfalto = px.Count(c => c.r > .15f && c.r < .42f && Mathf.Abs(c.r - c.g) < .05f && Mathf.Abs(c.g - c.b) < .06f);
                        float f = (float)asfalto / px.Length;
                        Check(f >= .05f, "la captura de calle muestra asfalto (" + (f * 100f).ToString("F1") + "%)");
                        UnityEngine.Object.DestroyImmediate(tex);
                    }
                }
                var general = GameObject.Find("Camara general (no build)")?.GetComponent<Camera>();
                if (general != null)
                    Check(Capture(general, PreviewDir + "/Calles_general_unity.png", 1600, 900), "captura general generada");
            }
            catch (Exception e)
            {
                sb.AppendLine("ERROR " + e.Message);
                fallos++;
            }

            File.WriteAllText(ReportPath, sb.ToString(), new UTF8Encoding(false));
            Debug.Log(sb.ToString());
            if (fallos == 0) Debug.Log("TYCOON_CALLES_PREVIEW_OK: avenida, cruce, cebras, edificios fuera de la vía, frente a la calle y capturas comprobados.");
            else Debug.LogError("TYCOON_CALLES_PREVIEW_FALLO: " + fallos + " problema(s); ver " + ReportPath);
            if (Application.isBatchMode) EditorApplication.delayCall += () => EditorApplication.Exit(fallos == 0 ? 0 : 1);
        }

        /// <summary>True si la caja de los renderers cruza el rectángulo libre en el plano XZ.</summary>
        private static bool BoundsCruzaRect(Bounds b, Rect r)
        {
            return b.max.x > r.xMin && b.min.x < r.xMax && b.max.z > r.yMin && b.min.z < r.yMax;
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
