using System;
using System.IO;
using System.Linq;
using Tycoon3D.Core;
using Tycoon3D.Restaurant;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tycoon3D.Editor
{
    /// <summary>
    /// Prueba en Play Mode de la variedad de clientes: la cajera viste otra variante de la paleta, salen dos paseantes más con
    /// otra ropa (copias de otros modelos), las variantes usan solo colores de la paleta del proyecto y dejan intactos piel y
    /// pelo, ningún cliente pasa de 1 000 triángulos y los nuevos paseantes caminan por la calle. Marcador: TYCOON_VARIETY_OK
    /// (no lanzar con -quit).
    /// </summary>
    public static class CustomerVarietyPlaySmoke
    {
        private const string ScenePath = "Assets/_Game/Scenes/BarrioResidencial.unity";
        private const int TriangleBudget = 1000;
        private static bool prevOptionsEnabled;
        private static EnterPlayModeOptions prevOptions;
        private static int frames;
        private static float startedAt;
        private static CustomerVariety variety;
        private static Vector3[] startPositions;

        public static void Run()
        {
            try
            {
                GameProfile.DirectoryOverride = Path.Combine(Path.GetTempPath(), "tycoon-smoke-variety-" + Guid.NewGuid().ToString("N"));
                EditorSceneManager.OpenScene(ScenePath);
                prevOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
                prevOptions = EditorSettings.enterPlayModeOptions;
                EditorSettings.enterPlayModeOptionsEnabled = true;
                EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
                EditorApplication.playModeStateChanged += OnPlayMode;
                EditorApplication.EnterPlaymode();
            }
            catch (Exception e) { Fail(e); }
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            frames = 0;
            EditorApplication.update += WaitForStart;
        }

        private static void WaitForStart()
        {
            if (++frames < 12) return;
            EditorApplication.update -= WaitForStart;
            try
            {
                variety = UnityEngine.Object.FindFirstObjectByType<CustomerVariety>();
                Require(variety != null, "No se creó la variedad de clientes.");
                Require(variety.Spawned == CustomerVariety.ExtraStrollers, "Debían salir 2 paseantes más; salieron " + variety.Spawned);
                var all = AnimatedCustomer.All.ToArray();
                Require(all.Length == 6, "Debía haber 6 clientes (fila, 3 paseantes y cajera); hay " + all.Length);
                Require(all.Count(c => c.Mode == CustomerMode.Stroll) == 3 && all.Count(c => c.Mode == CustomerMode.Queue) == 2 && all.Count(c => c.Mode == CustomerMode.Cashier) == 1,
                    "Reparto de clientes: 2 en la fila, 3 paseantes y 1 cajera.");
                Require(variety.Looks.Count == 3, "Debía haber 3 clientes con ropa distinta; hay " + variety.Looks.Count);
                Require(variety.Looks.Select(l => l.Variant).Distinct().Count() == 3, "Las 3 variantes deben ser distintas.");
                Require(variety.Looks.Any(l => l.Customer.Mode == CustomerMode.Cashier && l.Variant == 1), "La cajera debía vestir la variante 1.");

                // presupuesto de triángulos y animadores válidos
                foreach (var c in all)
                {
                    Require(c.Animator != null && c.Animator.runtimeAnimatorController != null && c.Animator.avatar != null && c.Animator.avatar.isValid, "Animador inválido en " + c.name);
                    long triangles = 0;
                    foreach (var r in c.GetComponentsInChildren<SkinnedMeshRenderer>(true)) triangles += r.sharedMesh.triangles.Length / 3;
                    foreach (var f in c.GetComponentsInChildren<MeshFilter>(true)) triangles += f.sharedMesh.triangles.Length / 3;
                    Require(triangles > 100 && triangles <= TriangleBudget, c.name + " tiene " + triangles + " triángulos (tope " + TriangleBudget + ")");
                }

                // colores: cada variante usa solo colores de la paleta, deja intactos piel y pelo y cambia la ropa
                foreach (var look in variety.Looks)
                {
                    var mat = look.Customer.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).First(m => m != null && m.HasProperty("_BaseMap"));
                    var tex = mat.GetTexture("_BaseMap") as Texture2D;
                    Require(tex != null && tex.width == 128 && tex.height == 64 && tex.isReadable, "La variante " + look.Variant + " no tiene su textura de paleta.");
                    var pixels = tex.GetPixels32();
                    foreach (var swatch in PaletteVariants.Palette)
                    {
                        int px = swatch.Col * PaletteVariants.CellPx + 8, py = (PaletteVariants.Rows - 1 - swatch.Row) * PaletteVariants.CellPx + 8;
                        var c = pixels[py * PaletteVariants.Width + px];
                        bool original = c.r == swatch.R && c.g == swatch.G && c.b == swatch.B;
                        bool protectedCell = PaletteVariants.Protected.Contains(swatch.Name);
                        if (protectedCell) Require(original, "La variante " + look.Variant + " cambió " + swatch.Name + " (piel o pelo).");
                        Require(PaletteVariants.Palette.Any(p => p.R == c.r && p.G == c.g && p.B == c.b), "La variante " + look.Variant + " usa un color fuera de la paleta en " + swatch.Name);
                    }
                    var turquesa = PaletteVariants.Find("M_Turquesa");
                    var shown = pixels[((PaletteVariants.Rows - 1 - turquesa.Row) * PaletteVariants.CellPx + 8) * PaletteVariants.Width + turquesa.Col * PaletteVariants.CellPx + 8];
                    var expected = PaletteVariants.CellColor(PaletteVariants.BuildPixels(look.Variant), turquesa.Col, turquesa.Row);
                    Require((shown.r, shown.g, shown.b) == expected, "El turquesa de la variante " + look.Variant + " no coincide con la tabla.");
                }
                // los clientes originales siguen con su aspecto de siempre
                var original0 = all.Where(c => !variety.Looks.Any(l => l.Customer == c)).ToArray();
                Require(original0.Length == 3, "Debían quedar 3 clientes con el aspecto original; hay " + original0.Length);
                Require(original0.All(c => c.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Where(m => m != null && m.HasProperty("_BaseMap"))
                    .All(m => !m.name.Contains(" - "))), "Un cliente original cambió de material.");

                // los paseantes nuevos caminan por la calle
                var extras = variety.Looks.Where(l => l.Customer.Mode == CustomerMode.Stroll).Select(l => l.Customer).ToArray();
                Require(extras.Length == 2, "Debía haber 2 paseantes nuevos con ropa distinta.");
                Require(extras.All(c => c.Flow == null), "Los paseantes nuevos no deben llevar flujo de pedido.");
                startPositions = extras.Select(c => c.transform.position).ToArray();
                startedAt = Time.realtimeSinceStartup;
                EditorApplication.update += WaitForWalk;
            }
            catch (Exception e) { Fail(e); }
        }

        private static void WaitForWalk()
        {
            try
            {
                if (Time.realtimeSinceStartup - startedAt < 7f) return;
                EditorApplication.update -= WaitForWalk;
                var extras = variety.Looks.Where(l => l.Customer.Mode == CustomerMode.Stroll).Select(l => l.Customer).ToArray();
                for (int i = 0; i < extras.Length; i++)
                    Require(Vector3.Distance(extras[i].transform.position, startPositions[i]) > 1.5f, "El paseante nuevo " + extras[i].name + " no avanzó por la calle.");
                Debug.Log("TYCOON_VARIETY_OK: la cajera y dos paseantes nuevos visten variantes de la paleta del proyecto (piel y pelo intactos), ningún cliente pasa de 1000 triángulos y los nuevos caminan por la calle.");
                Finish(0);
            }
            catch (Exception e) { Fail(e); }
        }

        private static void Require(bool ok, string message)
        {
            if (!ok) throw new InvalidOperationException(message);
        }

        private static void Fail(Exception e)
        {
            Debug.LogException(e);
            Finish(1);
        }

        private static void Finish(int code)
        {
            EditorApplication.update -= WaitForStart;
            EditorApplication.update -= WaitForWalk;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            GameProfile.DirectoryOverride = null;
            EditorSettings.enterPlayModeOptions = prevOptions;
            EditorSettings.enterPlayModeOptionsEnabled = prevOptionsEnabled;
            if (code == 0) EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(code);
        }
    }
}
