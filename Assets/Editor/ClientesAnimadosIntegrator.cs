using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Tycoon3D.Restaurant;

namespace Tycoon3D.Editor
{
    /// <summary>Importa los 12 FBX animados de Casa Marea (avatar + clips), arma un Animator por cliente y los coloca en el barrio.</summary>
    public static class ClientesAnimadosIntegrator
    {
        private const string ScenePath = "Assets/_Game/Scenes/BarrioResidencial.unity";
        private const string Folder = "Assets/_Game/Models/CasaMarea/Animados/";
        private const string ControllerFolder = "Assets/_Game/Animation";
        private const string GroupName = "Clientes animados";
        private static readonly string[] Names = { "Pescador", "Vecina", "Joven" };
        private static readonly string[] Clips = { "Idle", "Caminar", "Pedir" };

        [MenuItem("Tools/Tycoon3D/Integrar clientes animados")]
        public static void Integrate()
        {
            controllers.Clear();
            foreach (var n in Names) ConfigureImports(n);
            AssetDatabase.SaveAssets();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var previous = GameObject.Find(GroupName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous);
            var group = new GameObject(GroupName).transform;

            // la fila ante el despacho y dos paseantes por la calle; los clientes estáticos equivalentes se ocultan
            Place("Pescador", CustomerMode.Queue, new Vector3(6.1f, 0, 1.75f), new[] { new Vector3(6.1f, 0, 1.75f) }, 1f, group);
            Place("Vecina", CustomerMode.Queue, new Vector3(6.1f, 0, .4f), new[] { new Vector3(6.1f, 0, .4f) }, 2f, group);
            Place("Joven", CustomerMode.Stroll, new Vector3(-7.5f, 0, -7.2f),
                new[] { new Vector3(-7.5f, 0, -7.2f), new Vector3(11.5f, 0, -7.2f) }, 3f, group);
            // cajera del casino: detrás del mostrador de la recepción, mirando a los clientes (oeste)
            var cashierCustomer = Place("Joven", CustomerMode.Cashier, new Vector3(20.85f, 0, 2.5f), new[] { new Vector3(20.85f, 0, 2.5f) }, 4f, group, "Cajera FOX");
            var desk = UnityEngine.Object.FindFirstObjectByType<Tycoon3D.Casino.CashierDesk>();
            var service = UnityEngine.Object.FindFirstObjectByType<Tycoon3D.Casino.CasinoService>();
            if (desk != null) desk.Configure(service, cashierCustomer);
            HideStatic("SM_Cliente_Pescador"); HideStatic("SM_Cliente_Vecina"); HideStatic("SM_Cliente_Joven");

            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("No se pudo guardar la escena.");
            AssetDatabase.SaveAssets();
            Debug.Log("CLIENTES_ANIMADOS_OK: 4 personajes con Animator (fila, paseante y cajera) integrados.");
        }

        private static void ConfigureImports(string who)
        {
            string main = Folder + "SM_Cliente_" + who + ".fbx";
            var mainImporter = (ModelImporter)AssetImporter.GetAtPath(main);
            if (mainImporter == null) throw new InvalidOperationException("Falta " + main);
            mainImporter.animationType = ModelImporterAnimationType.Generic;
            mainImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mainImporter.importAnimation = false;
            mainImporter.SaveAndReimport();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(main).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid) throw new InvalidOperationException("Avatar no válido: " + who);

            var mask = BuildMask(who, main);
            foreach (var clip in Clips)
            {
                string path = Folder + "SM_Cliente_" + who + "@" + clip + ".fbx";
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                if (importer == null) throw new InvalidOperationException("Falta " + path);
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = avatar;
                importer.importAnimation = true;
                var defaults = importer.defaultClipAnimations;
                if (defaults == null || defaults.Length == 0) throw new InvalidOperationException("Sin clip en " + path);
                var c = defaults[0];
                c.name = clip;
                bool loop = clip != "Pedir";
                c.loopTime = loop;
                c.loopPose = loop;
                if (loop) c.lastFrame = Mathf.Max(c.firstFrame + 1, c.lastFrame - 1);    // el último fotograma repite el primero
                c.maskType = ClipAnimationMaskType.CopyFromOther;       // sin el nodo raíz: el clip lo gira -90° y tumbaba al cliente
                c.maskSource = mask;
                importer.clipAnimations = new[] { c };
                importer.SaveAndReimport();
            }
        }

        private static AvatarMask BuildMask(string who, string mainPath)
        {
            if (!AssetDatabase.IsValidFolder(ControllerFolder)) AssetDatabase.CreateFolder("Assets/_Game", "Animation");
            string path = ControllerFolder + "/Cliente_" + who + "_SinRaiz.mask";
            AssetDatabase.DeleteAsset(path);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(mainPath);
            var all = prefab.GetComponentsInChildren<Transform>(true);
            var mask = new AvatarMask { transformCount = all.Length };
            for (int i = 0; i < all.Length; i++)
            {
                string p = AnimationUtility.CalculateTransformPath(all[i], prefab.transform);
                mask.SetTransformPath(i, p);
                mask.SetTransformActive(i, p.Length > 0);          // "" = nodo raíz del modelo
            }
            AssetDatabase.CreateAsset(mask, path);
            return mask;
        }

        private static readonly System.Collections.Generic.Dictionary<string, AnimatorController> controllers = new System.Collections.Generic.Dictionary<string, AnimatorController>();

        private static AnimatorController BuildController(string who)
        {
            if (controllers.TryGetValue(who, out var cached) && cached != null) return cached;     // un controlador por personaje, aunque haya varias copias
            if (!AssetDatabase.IsValidFolder(ControllerFolder)) AssetDatabase.CreateFolder("Assets/_Game", "Animation");
            string path = ControllerFolder + "/Cliente_" + who + ".controller";
            AssetDatabase.DeleteAsset(path);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Pedir", AnimatorControllerParameterType.Trigger);
            var sm = controller.layers[0].stateMachine;

            AnimationClip Load(string clip)
            {
                var found = AssetDatabase.LoadAllAssetsAtPath(Folder + "SM_Cliente_" + who + "@" + clip + ".fbx")
                    .OfType<AnimationClip>().FirstOrDefault(a => !a.name.StartsWith("__preview__"));
                if (found == null) throw new InvalidOperationException("Clip no encontrado: " + who + "@" + clip);
                return found;
            }

            var idle = sm.AddState("Idle"); idle.motion = Load("Idle");
            var walk = sm.AddState("Caminar"); walk.motion = Load("Caminar");
            var ask = sm.AddState("Pedir"); ask.motion = Load("Pedir");
            sm.defaultState = idle;

            var toWalk = idle.AddTransition(walk); toWalk.hasExitTime = false; toWalk.duration = .15f;
            toWalk.AddCondition(AnimatorConditionMode.Greater, .1f, "Speed");
            var toIdle = walk.AddTransition(idle); toIdle.hasExitTime = false; toIdle.duration = .15f;
            toIdle.AddCondition(AnimatorConditionMode.Less, .1f, "Speed");
            var toAsk = idle.AddTransition(ask); toAsk.hasExitTime = false; toAsk.duration = .2f;
            toAsk.AddCondition(AnimatorConditionMode.If, 0, "Pedir");
            var back = ask.AddTransition(idle); back.hasExitTime = true; back.exitTime = .95f; back.duration = .25f;
            EditorUtility.SetDirty(controller);
            controllers[who] = controller;
            return controller;
        }

        private static AnimatedCustomer Place(string who, CustomerMode mode, Vector3 position, Vector3[] path, float seed, Transform parent, string label = null)
        {
            var controller = BuildController(who);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "SM_Cliente_" + who + ".fbx");
            var holder = new GameObject(label ?? "Cliente animado " + who);
            holder.transform.SetParent(parent, false);
            holder.transform.position = position;
            // mira hacia el despacho (oeste) en la fila y hacia el primer destino al pasear
            Vector3 face = mode != CustomerMode.Stroll ? Vector3.left : (path[1] - path[0]).normalized;
            holder.transform.rotation = Quaternion.LookRotation(face);

            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            model.name = "Modelo";
            model.transform.SetParent(holder.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;      // el importador ya convierte el eje (con -90° quedaban tumbados)
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var src = r.sharedMaterials;
                var dst = new Material[src.Length];
                for (int i = 0; i < src.Length; i++) dst[i] = CasaMareaPalette.Material(src[i]);
                r.sharedMaterials = dst;
                r.updateWhenOffscreen = true;
            }
            var animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            var customer = holder.AddComponent<AnimatedCustomer>();
            customer.Configure(mode, animator, path.Select(p => p).ToArray(), seed);
            return customer;
        }

        private static void HideStatic(string name)
        {
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == name && t.GetComponentInParent<AnimatedCustomer>() == null) t.gameObject.SetActive(false);
        }
    }
}
