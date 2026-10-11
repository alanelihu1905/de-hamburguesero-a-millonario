using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tycoon3D.Editor
{
    public static class CasaMareaPreviewCapture
    {
        [MenuItem("Tools/Tycoon3D/Capturar barrio Casa Marea")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/BarrioResidencial.unity", OpenSceneMode.Single);
            var house = GameObject.Find("SM_Casa_Vecina_A");
            if (house != null)
            {
                foreach (var renderer in house.GetComponentsInChildren<MeshRenderer>())
                {
                    var filter = renderer.GetComponent<MeshFilter>();
                    var uv = filter != null ? filter.sharedMesh.uv : new Vector2[0];
                    var uniqueUvs = new System.Collections.Generic.HashSet<Vector2>(uv);
                    Debug.Log("PALETTE_DIAG " + renderer.name + " material=" + renderer.sharedMaterial.name
                        + " texture=" + renderer.sharedMaterial.mainTexture?.name
                        + " uv0=" + (uv.Length > 0 ? uv[0].ToString() : "none")
                        + " uvCount=" + uniqueUvs.Count + " uvs=" + string.Join(";", uniqueUvs));
                }
            }
            var view = new GameObject("Camara temporal de revision");
            var camera = view.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.51f, .75f, .79f);
            camera.fieldOfView = 60;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 100;
            view.transform.position = new Vector3(0, 4.6f, -15);
            view.transform.LookAt(new Vector3(0, 1.1f, 0));
            var target = new RenderTexture(1600, 900, 24);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            image.Apply();
            var path = "ArtSource/CasaMarea/Preview/Unity_Barrio_Integrado.png";
            File.WriteAllBytes(path, image.EncodeToPNG());
            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(view);
            Debug.Log("CASAMAREA_PREVIEW_OK: " + path);
        }
    }
}
