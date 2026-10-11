using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tycoon3D.Editor
{
    /// <summary>Capturas de revisión del Casino FOX desde el editor (cuatro vistas). Los colores del editor se ven más pálidos que en la build.</summary>
    public static class CasinoPreviewCapture
    {
        [MenuItem("Tools/Tycoon3D/Capturar Casino FOX")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/BarrioResidencial.unity", OpenSceneMode.Single);
            var views = new[]
            {
                ("Casino_01_desde_el_inicio", new Vector3(0, 1.65f, -8.5f), new Vector3(14.5f, 2.2f, -3f), 78f),
                ("Casino_02_fachada", new Vector3(14.5f, 1.8f, -9f), new Vector3(14.5f, 2.6f, -2f), 55f),
                ("Casino_03_interior", new Vector3(14.5f, 1.65f, -1.2f), new Vector3(14.5f, 1.5f, 3.8f), 70f),
                ("Casino_04_maquina", new Vector3(13.6f, 1.65f, 1.7f), new Vector3(13.6f, 1.45f, 3.5f), 42f),
            };
            Directory.CreateDirectory("ArtSource/CasaMarea/Preview");
            foreach (var (name, position, target, fov) in views)
            {
                var go = new GameObject("Camara temporal casino");
                var camera = go.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.51f, .75f, .79f);
                camera.fieldOfView = fov;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 100;
                go.transform.position = position;
                go.transform.LookAt(target);
                var rt = new RenderTexture(1600, 900, 24);
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                image.Apply();
                File.WriteAllBytes("ArtSource/CasaMarea/Preview/" + name + ".png", image.EncodeToPNG());
                RenderTexture.active = null;
                camera.targetTexture = null;
                Object.DestroyImmediate(image);
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(go);
            }
            Debug.Log("CASINO_PREVIEW_OK: cuatro capturas en ArtSource/CasaMarea/Preview.");
        }
    }
}
