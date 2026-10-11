using System;
using System.Collections.Generic;
using Tycoon3D.Casino;
using Tycoon3D.Economy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tycoon3D.Editor
{
    /// <summary>
    /// Construye el Casino FOX en BarrioResidencial: plaza, edificio con entrada abierta, recepción, zona de estar, seis
    /// tragamonedas de tres tipos con rodillos que giran, luces, sonido, señalética y mejoras del barrio. Idempotente: borra
    /// y rehace sus grupos; no toca compra-cocción-venta. Si se re-ejecuta la integración de arte de Casa Marea, ejecutar
    /// esta después.
    /// </summary>
    public static class CasinoFoxBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/BarrioResidencial.unity";
        private const string RootName = "Casino FOX";
        private const string ServiceName = "Casino FOX - servicio y HUD";
        private const string BarrioName = "Casino FOX - plaza y señalética";

        // edificio (coordenadas de escena): entrada hacia -Z, hacia la calle
        private static readonly Vector3 C = new Vector3(16f, 0f, 1.6f);
        private const float W = 11f, D = 8.4f, H = 4.6f, T = .25f, DoorW = 3.6f, DoorH = 3.0f;
        private static float ReceptionX => C.x + 4.0f;        // el mostrador deja sitio detrás para la cajera
        private static float FrontZ => C.z - D / 2f + T / 2f;
        private static float BackZ => C.z + D / 2f - T / 2f;

        private const float TextScale = .42f;
        private static Font font;
        private static Material textMaterial;

        private sealed class Mats
        {
            public Material plum, plumLight, chile, cream, teal, wood, screen, steel, neon, neonChile, reel, marquee, mangoMatte,
                floor, gold, posterBack;
            public Material[] posters = new Material[5];
        }

        [MenuItem("Tools/Tycoon3D/Construir Casino FOX")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var wallet = UnityEngine.Object.FindFirstObjectByType<Wallet>();
            if (wallet == null) throw new InvalidOperationException("Falta el Wallet de la escena.");
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CasinoArt.ExternalUsed.Clear();
            Remove(RootName); Remove(ServiceName); Remove(BarrioName); Remove("Plaza FOX");
            ClearNeighborB();

            var atlas = CasinoArt.Texture("slot_symbols.png", false, false, true);
            var floorTex = CasinoArt.Texture("casino_floor.png", true, true, false);
            var cobbleTex = CasinoArt.Texture("cobble.png", true, true, false);
            textMaterial = TextMat();
            var m = MakeMats(atlas, floorTex);

            var root = new GameObject(RootName).transform;
            var barrio = new GameObject(BarrioName).transform;

            // ---- calle y plaza con adoquín
            var street = GameObject.Find("Calle peatonal");
            if (street != null)
                street.GetComponent<Renderer>().sharedMaterial = CasinoArt.Mat("M_Calle_Adoquin", new Color32(255, 255, 255, 255), .15f,
                    baseMap: cobbleTex, tiling: new Vector2(9f, 5f));
            var plazaMat = CasinoArt.Mat("M_Plaza_Adoquin", new Color32(255, 246, 232, 255), .15f, baseMap: cobbleTex, tiling: new Vector2(7.5f, 9f));
            Cube("Plaza FOX", new Vector3(16.5f, -.22f, -2f), new Vector3(15f, .3f, 18f), plazaMat, null);

            BuildShell(root, m);
            BuildFacade(root, m);
            BuildInterior(root, m);

            // ---- servicio, sonidos y HUD
            var service = new GameObject(ServiceName);
            var casino = service.AddComponent<CasinoService>();
            var sounds = service.AddComponent<CasinoSounds>();
            sounds.spinLoop = CasinoArt.Clip("slot_spin_loop"); sounds.spinStart = CasinoArt.Clip("slot_start");
            sounds.reelStop = CasinoArt.Clip("reel_stop"); sounds.betChange = CasinoArt.Clip("bet_change");
            sounds.lose = CasinoArt.Clip("lose"); sounds.winSmall = CasinoArt.Clip("win_small");
            sounds.winBig = CasinoArt.Clip("win_big"); sounds.jackpot = CasinoArt.Clip("jackpot"); sounds.coin = CasinoArt.Clip("coin");
            casino.Configure(wallet, sounds, atlas);
            var door = new GameObject("Puerta Casino FOX").transform;
            door.SetParent(service.transform, false);
            door.position = new Vector3(C.x, 0f, FrontZ - .8f);
            service.AddComponent<CasinoHud>().Configure(casino, wallet, door);

            var ambient = new GameObject("Ambiente del casino").AddComponent<AudioSource>();
            ambient.transform.SetParent(root, false);
            ambient.transform.position = new Vector3(C.x, 2.5f, C.z);
            ambient.clip = CasinoArt.Clip("casino_ambient");
            ambient.loop = true; ambient.playOnAwake = true; ambient.spatialBlend = 1f; ambient.volume = .4f;
            ambient.minDistance = 5f; ambient.maxDistance = 26f; ambient.rolloffMode = AudioRolloffMode.Linear;

            BuildMachines(root, casino, m);
            BuildDecor(root, m);
            BuildCashier(root, casino, m);
            BuildPlaza(barrio, m);
            ImproveTree(m);

            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("No se pudo guardar el casino.");
            AssetDatabase.SaveAssets();
            var counts = new Dictionary<string, int>();
            foreach (var n in CasinoArt.ExternalUsed) counts[n] = counts.TryGetValue(n, out int c) ? c + 1 : 1;
            foreach (var kv in counts) Debug.Log("EXTERNAL_USED " + kv.Key + " x" + kv.Value);
            Debug.Log("CASINO_FOX_OK: casino ampliado con seis tragamonedas, sala, recepción, plaza y señalética.");
        }

        // ================================================================== materiales
        private static Mats MakeMats(Texture2D atlas, Texture2D floorTex)
        {
            var m = new Mats
            {
                plum = CasinoArt.Mat("M_Casino_Ciruela", new Color32(59, 47, 74, 255), .25f),
                plumLight = CasinoArt.Mat("M_Casino_CiruelaClara", new Color32(92, 74, 112, 255), .25f),
                chile = CasinoArt.Mat("M_Casino_Chile", new Color32(228, 87, 46, 255), .35f),
                cream = CasinoArt.Mat("M_Casino_Crema", new Color32(247, 238, 221, 255), .3f),
                teal = CasinoArt.Mat("M_Casino_Turquesa", new Color32(43, 166, 160, 255), .35f),
                wood = CasinoArt.Mat("M_Casino_Caoba", new Color32(138, 90, 60, 255), .3f),
                screen = CasinoArt.Mat("M_Casino_Pantalla", new Color32(28, 22, 38, 255), .8f),
                steel = CasinoArt.Mat("M_Casino_Acero", new Color32(142, 154, 160, 255), .6f),
                neon = CasinoArt.Mat("M_Casino_Neon", new Color32(244, 201, 93, 255), .4f, new Color32(244, 201, 93, 255), 2.2f),
                neonChile = CasinoArt.Mat("M_Casino_NeonChile", new Color32(228, 87, 46, 255), .4f, new Color32(228, 87, 46, 255), 1.8f),
                reel = CasinoArt.Mat("M_Casino_Rodillos", new Color32(255, 255, 255, 255), .3f, new Color32(255, 255, 255, 255), .55f,
                    baseMap: atlas, emissionMap: atlas),
                marquee = CasinoArt.Mat("M_Casino_Marquesina", new Color32(138, 90, 60, 255), .4f, new Color32(244, 201, 93, 255), 0f),
                mangoMatte = CasinoArt.Mat("M_Casino_MangoMate", new Color32(244, 201, 93, 255), .35f),
                floor = CasinoArt.Mat("M_Casino_Piso", new Color32(255, 255, 255, 255), .45f, baseMap: floorTex, tiling: new Vector2(W / 2f, D / 2f)),
                gold = CasinoArt.Mat("M_Casino_Oro", new Color32(244, 201, 93, 255), .6f, new Color32(244, 201, 93, 255), .8f),
                posterBack = CasinoArt.Mat("M_Casino_CuadroFondo", new Color32(247, 238, 221, 255), .2f)
            };
            for (int i = 0; i < 5; i++)
            {
                m.posters[i] = CasinoArt.Mat("M_Casino_Cuadro_" + i, new Color32(255, 255, 255, 255), .25f, baseMap: atlas, tiling: new Vector2(.2f, .5f));
                m.posters[i].SetTextureOffset("_BaseMap", new Vector2(i * .2f, .25f));
                EditorUtility.SetDirty(m.posters[i]);
            }
            return m;
        }

        private static Material TextMat()
        {
            string path = CasinoArt.MaterialRoot + "/M_Casino_Texto.mat";
            var shader = Shader.Find("Tycoon3D/FoxText");
            if (shader == null) { Debug.LogWarning("Falta Tycoon3D/FoxText; se usa el material de la fuente."); return font.material; }
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = "M_Casino_Texto" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetTexture("_MainTex", font.material.mainTexture);
            material.SetColor("_Color", Color.white);
            EditorUtility.SetDirty(material);
            return material;
        }

        // ================================================================== edificio
        private static void BuildShell(Transform root, Mats m)
        {
            Cube("Piso", new Vector3(C.x, -.1f, C.z), new Vector3(W, .2f, D), m.floor, root);
            Cube("Alfombra", new Vector3(C.x, .012f, C.z + .4f), new Vector3(2.6f, .03f, D - 1.4f), m.chile, root, false);
            Cube("Alfombra filete izq", new Vector3(C.x - 1.4f, .014f, C.z + .4f), new Vector3(.14f, .03f, D - 1.4f), m.neon, root, false);
            Cube("Alfombra filete der", new Vector3(C.x + 1.4f, .014f, C.z + .4f), new Vector3(.14f, .03f, D - 1.4f), m.neon, root, false);
            Cube("Muro trasero", new Vector3(C.x, H / 2f, BackZ), new Vector3(W, H, T), m.plum, root);
            Cube("Muro oeste", new Vector3(C.x - W / 2f + T / 2f, H / 2f, C.z), new Vector3(T, H, D), m.plum, root);
            Cube("Muro este", new Vector3(C.x + W / 2f - T / 2f, H / 2f, C.z), new Vector3(T, H, D), m.plum, root);
            float side = (W - DoorW) / 2f;
            Cube("Fachada izquierda", new Vector3(C.x - DoorW / 2f - side / 2f, H / 2f, FrontZ), new Vector3(side, H, T), m.plum, root);
            Cube("Fachada derecha", new Vector3(C.x + DoorW / 2f + side / 2f, H / 2f, FrontZ), new Vector3(side, H, T), m.plum, root);
            Cube("Dintel", new Vector3(C.x, (DoorH + H) / 2f, FrontZ), new Vector3(DoorW, H - DoorH, T), m.plum, root);
            Cube("Techo", new Vector3(C.x, H + .15f, C.z), new Vector3(W + .4f, .3f, D + .4f), m.plum, root);
            Cube("Pretil", new Vector3(C.x, H + .55f, FrontZ - .22f), new Vector3(W + .4f, .5f, .2f), m.plumLight, root);
            Cube("Friso oeste", new Vector3(C.x - W / 2f + T + .03f, 1.05f, C.z), new Vector3(.06f, .12f, D - 2 * T), m.neon, root, false);
            Cube("Friso este", new Vector3(C.x + W / 2f - T - .03f, 1.05f, C.z), new Vector3(.06f, .12f, D - 2 * T), m.neon, root, false);
            Cube("Friso fondo", new Vector3(C.x, 1.05f, BackZ - T / 2f - .03f), new Vector3(W - 2 * T, .12f, .06f), m.neon, root, false);
        }

        private static void BuildFacade(Transform root, Mats m)
        {
            float signZ = FrontZ - T / 2f - .1f;
            Cube("Jamba izq", new Vector3(C.x - DoorW / 2f - .07f, DoorH / 2f, FrontZ - .14f), new Vector3(.14f, DoorH, .1f), m.neon, root, false);
            Cube("Jamba der", new Vector3(C.x + DoorW / 2f + .07f, DoorH / 2f, FrontZ - .14f), new Vector3(.14f, DoorH, .1f), m.neon, root, false);
            Cube("Rótulo tablero", new Vector3(C.x, 3.85f, signZ), new Vector3(W - .6f, 1.3f, .16f), m.plumLight, root, false);
            Cube("Rótulo neón arriba", new Vector3(C.x, 4.55f, signZ - .1f), new Vector3(W - .9f, .08f, .05f), m.neon, root, false);
            Cube("Rótulo neón abajo", new Vector3(C.x, 3.18f, signZ - .1f), new Vector3(W - .9f, .08f, .05f), m.neonChile, root, false);
            for (int i = 0; i < 15; i++)
                Prim(PrimitiveType.Sphere, "Foco " + i, new Vector3(C.x - 5.0f + i * .714f, 4.72f, signZ - .12f), new Vector3(.19f, .19f, .19f), m.neon, root, false);
            Fox(new Vector3(C.x - 3.9f, 3.85f, signZ - .13f), 1.15f, root, m);
            Text("CASINO FOX", new Vector3(C.x + 1.0f, 4.0f, signZ - .12f), Quaternion.identity, .12f, 110, new Color32(244, 201, 93, 255), root);
            Text("SIEMPRE ABIERTO · PESOS VIRTUALES", new Vector3(C.x + 1.0f, 3.62f, signZ - .12f), Quaternion.identity, .04f, 90,
                new Color32(247, 238, 221, 255), root);

            for (int i = 0; i < 9; i++)
            {
                var mat = i % 2 == 0 ? m.chile : m.cream;
                var stripe = Cube("Toldo franja " + i, new Vector3(C.x - 2.0f + i * .5f, 3.28f, FrontZ - .95f), new Vector3(.5f, .06f, 1.7f), mat, root, false);
                stripe.transform.rotation = Quaternion.Euler(-9f, 0, 0);
            }
            Cube("Toldo riel", new Vector3(C.x, 3.12f, FrontZ - .12f), new Vector3(4.6f, .08f, .08f), m.steel, root, false);

            float poleX = C.x - W / 2f - .6f;
            Cube("Poste letrero", new Vector3(poleX, 3.1f, FrontZ - 1.1f), new Vector3(.16f, 6.2f, .16f), m.steel, root);
            Cube("Brazo letrero", new Vector3(poleX, 5.6f, FrontZ - 1.6f), new Vector3(.12f, .12f, 1.0f), m.steel, root, false);
            Cube("Letrero vertical", new Vector3(poleX, 4.6f, FrontZ - 2.3f), new Vector3(.16f, 2.2f, 1.7f), m.plum, root, false);
            Cube("Letrero vertical neón", new Vector3(poleX, 4.6f, FrontZ - 2.3f), new Vector3(.2f, 2.0f, 1.5f), m.neon, root, false);
            Cube("Letrero vertical fondo", new Vector3(poleX, 4.6f, FrontZ - 2.3f), new Vector3(.22f, 1.85f, 1.35f), m.plum, root, false);
            foreach (float yaw in new[] { 90f, -90f })
            {
                float off = yaw > 0 ? -.14f : .14f;
                Text("FOX", new Vector3(poleX + off, 5.05f, FrontZ - 2.3f), Quaternion.Euler(0, yaw, 0), .14f, 100, new Color32(244, 201, 93, 255), root);
                Text("CASINO", new Vector3(poleX + off, 4.4f, FrontZ - 2.3f), Quaternion.Euler(0, yaw, 0), .06f, 100, new Color32(247, 238, 221, 255), root);
                Text("abierto", new Vector3(poleX + off, 3.95f, FrontZ - 2.3f), Quaternion.Euler(0, yaw, 0), .045f, 100, new Color32(228, 87, 46, 255), root);
            }
            Light("Luz fachada izq", new Vector3(C.x - 3.2f, 3.2f, FrontZ - 2.2f), root, new Color32(255, 214, 140, 255), 3.0f, 8f);
            Light("Luz fachada der", new Vector3(C.x + 3.2f, 3.2f, FrontZ - 2.2f), root, new Color32(255, 214, 140, 255), 3.0f, 8f);
        }

        private static void BuildInterior(Transform root, Mats m)
        {
            Light("Luz interior oeste", new Vector3(C.x - 3.2f, 3.9f, C.z - .4f), root, new Color32(255, 214, 140, 255), 2.4f, 8f);
            Light("Luz interior este", new Vector3(C.x + 3.2f, 3.9f, C.z - .4f), root, new Color32(255, 214, 140, 255), 2.4f, 8f);
            Light("Luz interior centro", new Vector3(C.x, 3.9f, C.z + 2.4f), root, new Color32(255, 200, 130, 255), 2.2f, 8f);
            Light("Luz entrada", new Vector3(C.x, 3.9f, FrontZ + .6f), root, new Color32(255, 214, 140, 255), 2.2f, 7f);
            foreach (float z in new[] { C.z - 1.6f, C.z + 1.2f, C.z + 3.6f })
                Cube("Tira de luz", new Vector3(C.x, H - .08f, z), new Vector3(W - 3f, .08f, .16f), m.neon, root, false);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f;
                Prim(PrimitiveType.Sphere, "Candil foco " + i, new Vector3(C.x + Mathf.Cos(a) * .9f, H - .55f, C.z + .6f + Mathf.Sin(a) * .9f),
                    new Vector3(.2f, .2f, .2f), m.neon, root, false);
            }
            Prim(PrimitiveType.Cylinder, "Candil aro", new Vector3(C.x, H - .55f, C.z + .6f), new Vector3(1.9f, .03f, 1.9f), m.steel, root, false);
            Cube("Candil varilla", new Vector3(C.x, H - .28f, C.z + .6f), new Vector3(.05f, .55f, .05f), m.steel, root, false);
            Text("TRAGAMONEDAS FOX", new Vector3(C.x, 3.55f, BackZ - T / 2f - .08f), Quaternion.identity, .09f, 100, new Color32(244, 201, 93, 255), root);
            Text("3 máquinas distintas · 6 en total", new Vector3(C.x, 3.05f, BackZ - T / 2f - .08f), Quaternion.identity, .035f, 100,
                new Color32(247, 238, 221, 255), root);

            float wx = C.x - W / 2f + T + .05f, ex = C.x + W / 2f - T - .05f;
            int[] order = { 0, 1, 3, 4, 2 };
            float[] zs = { C.z - 1.2f, C.z + .9f, C.z + 3.0f };
            for (int i = 0; i < 3; i++)
            {
                Poster(new Vector3(wx, 2.25f, zs[i]), 90f, m.posters[order[i]], root, m);
                Poster(new Vector3(ex, 2.25f, zs[i]), -90f, m.posters[order[i + 2]], root, m);
            }
        }

        private static void Poster(Vector3 pos, float yaw, Material symbol, Transform root, Mats m)
        {
            var frame = Cube("Marco cuadro", pos, new Vector3(1.0f, 1.25f, .06f), m.plumLight, root, false);
            frame.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var back = Cube("Fondo cuadro", pos, new Vector3(.88f, 1.1f, .07f), m.posterBack, root, false);
            back.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Cuadro símbolo";
            quad.transform.SetParent(root, false);
            Vector3 normal = Quaternion.Euler(0, yaw, 0) * Vector3.back;
            quad.transform.position = pos + normal * .045f;
            quad.transform.rotation = Quaternion.Euler(0, yaw, 0);
            quad.transform.localScale = new Vector3(.8f, .8f, 1f);
            quad.GetComponent<Renderer>().sharedMaterial = symbol;
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
        }

        // ================================================================== máquinas
        private static void BuildMachines(Transform root, CasinoService casino, Mats m)
        {
            SlotMachineKind[] kinds = { SlotMachineKind.Clasica, SlotMachineKind.MangoRapido, SlotMachineKind.GranJackpot,
                                        SlotMachineKind.GranJackpot, SlotMachineKind.MangoRapido, SlotMachineKind.Clasica };
            float z = BackZ - T / 2f - .6f;
            var mesh = ReelDrum.BuildMesh(.14f, .2f);
            for (int i = 0; i < 6; i++)
            {
                float x = C.x + (-3.75f + i * 1.5f);
                Machine("Tragamonedas FOX " + (i + 1), kinds[i], new Vector3(x, 0, z), root, casino, m, mesh);
                float top;
                CasinoArt.Place("Kenney_FurnitureKit", "stoolBar", root, new Vector3(x, 0, z - 1.25f), 0, .74f, false, false, out top);
            }
        }

        private static void Machine(string name, SlotMachineKind kind, Vector3 position, Transform parent, CasinoService casino,
            Mats m, Mesh drumMesh)
        {
            var go = new GameObject(name).transform;
            go.SetParent(parent, false);
            go.position = position;
            var col = go.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 1.2f, 0);
            col.size = new Vector3(1.0f, 2.4f, .85f);

            Material body = kind == SlotMachineKind.Clasica ? m.chile : kind == SlotMachineKind.MangoRapido ? m.teal : m.plum;
            Material header = kind == SlotMachineKind.Clasica ? m.plum : kind == SlotMachineKind.MangoRapido ? m.mangoMatte : m.plumLight;
            Material trim = kind == SlotMachineKind.MangoRapido ? m.cream : m.neon;
            Color32 textColor = new Color32(59, 47, 74, 255);       // ciruela sobre la marquesina clara: contraste alto en las tres máquinas
            string label = kind == SlotMachineKind.Clasica ? "ZORRO" : kind == SlotMachineKind.MangoRapido ? "MANGO" : "JACKPOT";
            string hint = kind == SlotMachineKind.Clasica ? "x40" : kind == SlotMachineKind.MangoRapido ? "x12 rapido" : "x60";

            Cube("Base", new Vector3(0, .45f, 0), new Vector3(.95f, .9f, .8f), m.plum, go, false, true);
            Cube("Cuerpo", new Vector3(0, 1.3f, .02f), new Vector3(.95f, .8f, .76f), body, go, false, true);
            Cube("Fondo ventana", new Vector3(0, 1.45f, -.2f), new Vector3(.82f, .34f, .2f), m.screen, go, false, true);
            var drums = new ReelDrum[3];
            for (int i = 0; i < 3; i++)
            {
                var d = new GameObject("Rodillo " + (i + 1));
                d.transform.SetParent(go, false);
                d.transform.localPosition = new Vector3(-.215f + i * .215f, 1.45f, -.26f);
                d.AddComponent<MeshFilter>().sharedMesh = drumMesh;
                d.AddComponent<MeshRenderer>().sharedMaterial = m.reel;
                drums[i] = d.AddComponent<ReelDrum>();
            }
            Cube("Marco arriba", new Vector3(0, 1.45f + .15f, -.415f), new Vector3(.74f, .05f, .05f), trim, go, false, true);
            Cube("Marco abajo", new Vector3(0, 1.45f - .15f, -.415f), new Vector3(.74f, .05f, .05f), trim, go, false, true);
            Cube("Marco izq", new Vector3(-.36f, 1.45f, -.415f), new Vector3(.05f, .35f, .05f), trim, go, false, true);
            Cube("Marco der", new Vector3(.36f, 1.45f, -.415f), new Vector3(.05f, .35f, .05f), trim, go, false, true);
            Cube("Linea de premio", new Vector3(0, 1.45f, -.425f), new Vector3(.68f, .012f, .01f), m.neonChile, go, false, true);
            Cube("Cabecera", new Vector3(0, 1.98f, 0), new Vector3(.95f, .5f, .8f), header, go, false, true);
            var marquee = Cube("Marquesina", new Vector3(0, 1.98f, -.405f), new Vector3(.84f, .26f, .02f), m.marquee, go, false, true);
            Text(label, go.position + new Vector3(0, 2.03f, -.425f), Quaternion.identity, .042f, 100, textColor, go);
            Text(hint, go.position + new Vector3(0, 1.91f, -.425f), Quaternion.identity, .022f, 100, textColor, go);
            Cube("Franja", new Vector3(0, 1.72f, -.41f), new Vector3(.95f, .05f, .04f), m.neonChile, go, false, true);
            Cube("Bandeja", new Vector3(0, .95f, -.45f), new Vector3(.54f, .08f, .2f), m.steel, go, false, true);
            Prim(PrimitiveType.Cylinder, "Palanca", new Vector3(.54f, 1.3f, 0), new Vector3(.07f, .3f, .07f), m.steel, go, false, true);
            Prim(PrimitiveType.Sphere, "Bola palanca", new Vector3(.54f, 1.66f, 0), new Vector3(.16f, .16f, .16f), kind == SlotMachineKind.MangoRapido ? m.chile : m.neon, go, false, true);
            if (kind == SlotMachineKind.GranJackpot)
            {
                Cube("Corona", new Vector3(0, 2.36f, 0), new Vector3(.78f, .22f, .6f), m.neon, go, false, true);
                Cube("Corona cima", new Vector3(0, 2.52f, 0), new Vector3(.4f, .14f, .4f), m.neonChile, go, false, true);
            }
            if (kind == SlotMachineKind.Clasica)
                Cube("Cinta lateral", new Vector3(0, 1.0f, -.405f), new Vector3(.9f, .05f, .03f), m.neon, go, false, true);

            var bulbs = new List<Renderer>();
            Vector3[] bulbPos =
            {
                new Vector3(-.42f, 2.2f, -.42f), new Vector3(-.21f, 2.2f, -.42f), new Vector3(0, 2.2f, -.42f), new Vector3(.21f, 2.2f, -.42f), new Vector3(.42f, 2.2f, -.42f),
                new Vector3(.42f, 1.98f, -.42f),
                new Vector3(.42f, 1.77f, -.42f), new Vector3(.21f, 1.77f, -.42f), new Vector3(0, 1.77f, -.42f), new Vector3(-.21f, 1.77f, -.42f), new Vector3(-.42f, 1.77f, -.42f),
                new Vector3(-.42f, 1.98f, -.42f)
            };
            foreach (var p in bulbPos)
            {
                var bulb = Prim(PrimitiveType.Sphere, "Bombilla", p, new Vector3(.075f, .075f, .075f), m.marquee, go, false, true);
                bulbs.Add(bulb.GetComponent<Renderer>());
            }
            var lightGo = new GameObject("Luz de premio");
            lightGo.transform.SetParent(go, false);
            lightGo.transform.localPosition = new Vector3(0, 2.6f, -.8f);
            var winLight = lightGo.AddComponent<Light>();
            winLight.type = LightType.Point; winLight.color = new Color32(255, 214, 140, 255); winLight.intensity = 1.1f; winLight.range = 3.6f;
            winLight.shadows = LightShadows.None;
            var coins = CasinoArt.CoinBurst(go, go.position + new Vector3(0, 1.0f, -.5f), m.gold);
            var audio = go.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false; audio.loop = true; audio.spatialBlend = 1f; audio.minDistance = 1.5f; audio.maxDistance = 14f;
            audio.rolloffMode = AudioRolloffMode.Linear;

            go.gameObject.AddComponent<SlotMachineStation>().Configure(casino, kind, drums, bulbs.ToArray(), marquee.GetComponent<Renderer>(),
                winLight, coins, audio);
        }

        // ================================================================== decoración de la sala (modelos CC0 de Kenney)
        /// <summary>Ventanilla de la cajera: colisionador con CashierDesk (E cambia pesos por fichas, Q canjea). La cajera la pone ClientesAnimadosIntegrator.</summary>
        private static void BuildCashier(Transform root, CasinoService casino, Mats m)
        {
            float rx = ReceptionX;
            var desk = new GameObject("Ventanilla de la cajera");
            desk.transform.SetParent(root, false);
            desk.transform.position = new Vector3(rx + .05f, 1.1f, C.z + .9f);
            var box = desk.AddComponent<BoxCollider>();
            box.size = new Vector3(1.9f, 2.2f, 1.7f);
            desk.AddComponent<CashierDesk>().Configure(casino, null);
            Text("CAJERA", new Vector3(rx - .48f, 1.95f, C.z + .9f), Quaternion.Euler(0, 90, 0), .035f, 100, new Color32(247, 238, 221, 255), root);
        }

        private static void BuildDecor(Transform root, Mats m)
        {
            const string F = "Kenney_FurnitureKit", Fd = "Kenney_FoodKit";
            float top;
            float lx = C.x - 3.9f;
            CasinoArt.Place(F, "rugRound", root, new Vector3(lx, 0, C.z + .2f), 0, 3.4f, true, true, out top, .02f, true);
            foreach (float z in new[] { C.z - .9f, C.z + 1.4f })
            {
                CasinoArt.Place(F, "tableRound", root, new Vector3(lx, 0, z), 0, .74f, false, false, out top);
                CasinoArt.Place(F, "lampSquareTable", root, new Vector3(lx + .3f, 0, z - .1f), 20, .42f, false, false, out _, top, true);
                CasinoArt.Place(Fd, "cocktail", root, new Vector3(lx - .35f, 0, z + .15f), 0, .17f, false, false, out _, top, true);
                CasinoArt.Place(Fd, "cup", root, new Vector3(lx - .1f, 0, z - .35f), 40, .08f, false, false, out _, top, true);
            }
            CasinoArt.Place(Fd, "glass", root, new Vector3(lx + .35f, 0, C.z - 1.1f), 0, .13f, false, false, out _, .74f, true);
            CasinoArt.Place(Fd, "mug", root, new Vector3(lx - .3f, 0, C.z + 1.15f), 70, .09f, false, false, out _, .74f, true);

            float rx = ReceptionX;
            Cube("Recepción base", new Vector3(rx, .55f, C.z - .4f), new Vector3(.9f, 1.1f, 3.6f), m.plum, root);
            Cube("Recepción tapa", new Vector3(rx - .05f, 1.12f, C.z - .4f), new Vector3(1.05f, .08f, 3.75f), m.mangoMatte, root, false);
            Cube("Recepción franja", new Vector3(rx - .46f, .75f, C.z - .4f), new Vector3(.04f, .12f, 3.4f), m.neonChile, root, false);
            Cube("Recepción filete", new Vector3(rx - .46f, 1.0f, C.z - .4f), new Vector3(.04f, .05f, 3.4f), m.neon, root, false);
            Text("CAJA FOX", new Vector3(rx - .48f, 1.75f, C.z - .4f), Quaternion.Euler(0, 90, 0), .05f, 100, new Color32(244, 201, 93, 255), root);
            Text("fichas · 1 ficha = $25", new Vector3(rx - .48f, 1.55f, C.z - .4f), Quaternion.Euler(0, 90, 0), .024f, 100, new Color32(247, 238, 221, 255), root);
            float rt = 1.16f;
            CasinoArt.Place(Fd, "burger-cheese", root, new Vector3(rx - .1f, 0, C.z + 1.0f), 0, .18f, true, false, out _, rt, true);
            CasinoArt.Place(Fd, "plate", root, new Vector3(rx - .1f, 0, C.z + 1.0f), 0, .26f, true, true, out _, rt - .01f, true);
            CasinoArt.Place(Fd, "cocktail", root, new Vector3(rx, 0, C.z + .2f), 0, .17f, false, false, out _, rt, true);
            CasinoArt.Place(Fd, "soda-glass", root, new Vector3(rx - .2f, 0, C.z - .4f), 20, .14f, false, false, out _, rt, true);
            CasinoArt.Place(Fd, "soda-can", root, new Vector3(rx, 0, C.z - 1.0f), 0, .12f, false, false, out _, rt, true);
            CasinoArt.Place(Fd, "soda-can", root, new Vector3(rx - .15f, 0, C.z - 1.15f), 50, .12f, false, false, out _, rt, true);
            CasinoArt.Place(Fd, "bottle-ketchup", root, new Vector3(rx + .1f, 0, C.z - 1.5f), 0, .22f, false, false, out _, rt, true);
            CasinoArt.Place(Fd, "cupcake", root, new Vector3(rx - .15f, 0, C.z + .75f), 0, .09f, false, false, out _, rt, true);
            CasinoArt.Place(F, "stoolBarSquare", root, new Vector3(rx - 1.0f, 0, C.z - .9f), 0, .72f, false, false, out top);
            CasinoArt.Place(F, "stoolBarSquare", root, new Vector3(rx - 1.0f, 0, C.z + .5f), 0, .72f, false, false, out top);
            CasinoArt.Place(F, "sideTable", root, new Vector3(C.x + 4.55f, 0, C.z + 2.9f), -90, .7f, false, false, out top);
            CasinoArt.Place(Fd, "mug", root, new Vector3(C.x + 4.55f, 0, C.z + 2.9f), 0, .09f, false, false, out _, top, true);

            CasinoArt.Place(F, "pottedPlant", root, new Vector3(C.x - 5.05f, 0, FrontZ + .6f), 0, .55f, false, false, out top);
            CasinoArt.Place(F, "pottedPlant", root, new Vector3(C.x + 5.05f, 0, FrontZ + .6f), 0, .55f, false, false, out top);
            CasinoArt.Place(F, "plantSmall1", root, new Vector3(C.x - 4.75f, 0, BackZ - 1.0f), 0, 1.15f, false, false, out top);
            CasinoArt.Place(F, "plantSmall2", root, new Vector3(C.x + 4.75f, 0, BackZ - 1.0f), 0, 1.0f, false, false, out top);
            CasinoArt.Place(F, "speakerSmall", root, new Vector3(C.x - 4.85f, 0, FrontZ + 2.2f), 90, .5f, false, false, out _, 2.9f, true);
            CasinoArt.Place(F, "speakerSmall", root, new Vector3(C.x + 4.85f, 0, FrontZ + 2.2f), -90, .5f, false, false, out _, 2.9f, true);
            CasinoArt.Place(F, "trashcan", root, new Vector3(C.x - 1.9f, 0, FrontZ + .6f), 0, .55f, false, false, out top);
            foreach (float z in new[] { C.z - 1.9f, C.z + .6f, C.z + 3.1f })
            {
                CasinoArt.Place(F, "lampWall", root, new Vector3(C.x - W / 2f + T + .06f, 0, z), 90, .34f, false, false, out _, 2.7f, true);
                CasinoArt.Place(F, "lampWall", root, new Vector3(C.x + W / 2f - T - .06f, 0, z), -90, .34f, false, false, out _, 2.7f, true);
            }
        }

        // ================================================================== plaza, señalética y barrio
        private static void BuildPlaza(Transform barrio, Mats m)
        {
            float cz = (FrontZ - 8.5f + FrontZ) / 2f - .2f;
            Cube("Alfombra exterior", new Vector3(C.x, -.04f, cz), new Vector3(2.4f, .02f, 8.5f), m.chile, barrio, false);
            Cube("Alfombra exterior filete izq", new Vector3(C.x - 1.3f, -.035f, cz), new Vector3(.12f, .02f, 8.5f), m.neon, barrio, false);
            Cube("Alfombra exterior filete der", new Vector3(C.x + 1.3f, -.035f, cz), new Vector3(.12f, .02f, 8.5f), m.neon, barrio, false);

            RouteSign(new Vector3(5.2f, 0, -6.6f), "CASINO FOX", m, barrio);
            RouteSign(new Vector3(10.2f, 0, -8.2f), "CASINO FOX", m, barrio);
            for (int i = 0; i < 4; i++) GroundArrow(new Vector3(2.0f + i * 3.0f, -.05f, -5.6f + i * -.4f), m, barrio);

            Add("SM_Farol_Calle", new Vector3(11.0f, 0, -4.4f), 0, barrio);
            Add("SM_Farol_Calle", new Vector3(21.0f, 0, -4.4f), 0, barrio);
            Add("SM_Farol_Calle", new Vector3(11.0f, 0, -9.0f), 0, barrio);
            Add("SM_Farol_Calle", new Vector3(21.0f, 0, -9.0f), 0, barrio);
            Add("SM_Banco_Callejon", new Vector3(12.4f, 0, -3.5f), 0, barrio);
            Add("SM_Banco_Callejon", new Vector3(19.6f, 0, -3.5f), 0, barrio);
            Add("SM_Maceta_Tropical", new Vector3(13.2f, 0, -3.45f), 0, barrio);
            Add("SM_Maceta_Tropical", new Vector3(18.8f, 0, -3.45f), 0, barrio);
            Add("SM_Barril", new Vector3(22.0f, 0, -3.6f), 0, barrio);
            foreach (float z in new[] { -5.2f, -7.4f })
            {
                Add("SM_Bolardo", new Vector3(C.x - 1.9f, 0, z), 0, barrio);
                Add("SM_Bolardo", new Vector3(C.x + 1.9f, 0, z), 0, barrio);
            }
            Light("Luz plaza oeste", new Vector3(12.5f, 3.4f, -6.5f), barrio, new Color32(255, 214, 140, 255), 2.4f, 9f);
            Light("Luz plaza este", new Vector3(19.5f, 3.4f, -6.5f), barrio, new Color32(255, 214, 140, 255), 2.4f, 9f);
        }

        private static void RouteSign(Vector3 pos, string text, Mats m, Transform parent)
        {
            Cube("Poste señal", pos + new Vector3(0, 1.4f, .1f), new Vector3(.12f, 2.8f, .12f), m.steel, parent);
            Cube("Señal tablero", pos + new Vector3(0, 2.7f, 0), new Vector3(3.2f, .95f, .1f), m.plum, parent, false);
            Cube("Señal filete", pos + new Vector3(0, 2.7f, -.06f), new Vector3(3.05f, .8f, .03f), m.neon, parent, false);
            Cube("Señal fondo", pos + new Vector3(0, 2.7f, -.07f), new Vector3(2.95f, .7f, .03f), m.plum, parent, false);
            Text(text, pos + new Vector3(-.3f, 2.7f, -.1f), Quaternion.identity, .1f, 100, new Color32(244, 201, 93, 255), parent);
            var arrow = Arrow(.5f, m.neonChile, parent);
            arrow.transform.position = pos + new Vector3(1.0f, 2.7f, -.1f);
        }

        private static void GroundArrow(Vector3 pos, Mats m, Transform parent)
        {
            var arrow = Arrow(1.0f, m.neon, parent);
            arrow.transform.position = pos;
            arrow.transform.rotation = Quaternion.Euler(90, 0, 0);
        }

        private static GameObject Arrow(float size, Material material, Transform parent)
        {
            var g = new GameObject("Flecha");
            g.transform.SetParent(parent, false);
            Prism("Flecha cuerpo", new[] { new Vector2(-.5f, -.12f) * size, new Vector2(.15f, -.12f) * size, new Vector2(.15f, .12f) * size, new Vector2(-.5f, .12f) * size }, .04f * size, material, g.transform, Vector3.zero);
            Prism("Flecha punta", new[] { new Vector2(.1f, -.3f) * size, new Vector2(.55f, 0f) * size, new Vector2(.1f, .3f) * size }, .04f * size, material, g.transform, Vector3.zero);
            return g;
        }

        private static void Add(string name, Vector3 position, float facing, Transform parent)
        {
            var prefab = CasaMareaPalette.Model(name);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            model.name = name;
            model.transform.SetParent(parent, false);
            model.transform.position = position;
            model.transform.rotation = Quaternion.Euler(0, 180 + facing, 0) * Quaternion.Euler(-90, 0, 0);
            var renderers = model.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                var sources = renderer.sharedMaterials;
                var replacements = new Material[sources.Length];
                for (int i = 0; i < sources.Length; i++) replacements[i] = CasaMareaPalette.Material(sources[i]);
                renderer.sharedMaterials = replacements;
            }
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            model.transform.position += Vector3.up * (position.y - bounds.min.y);
        }

        private static void ImproveTree(Mats m)
        {
            var crown = GameObject.Find("Arbol copa");
            var trunk = GameObject.Find("Arbol tronco");
            if (crown == null || trunk == null) return;
            Vector3 c = crown.transform.position;
            Transform parent = crown.transform.parent;
            UnityEngine.Object.DestroyImmediate(crown);
            var dark = CasinoArt.Mat("M_Arbol_Verde_Oscuro", new Color32(63, 106, 48, 255), .2f);
            var mid = CasinoArt.Mat("M_Arbol_Verde", new Color32(79, 122, 58, 255), .2f);
            var light = CasinoArt.Mat("M_Arbol_Verde_Claro", new Color32(123, 175, 75, 255), .2f);
            trunk.GetComponent<Renderer>().sharedMaterial = m.wood;
            (Vector3, float, Material)[] blobs =
            {
                (new Vector3(0, .1f, 0), 1.5f, mid), (new Vector3(-.7f, -.1f, .2f), 1.1f, dark), (new Vector3(.75f, -.05f, -.2f), 1.2f, light),
                (new Vector3(.1f, .7f, .1f), 1.05f, light), (new Vector3(-.2f, .35f, -.7f), .95f, dark)
            };
            int i = 0;
            foreach (var (offset, size, material) in blobs)
                Prim(PrimitiveType.Sphere, "Arbol follaje " + (++i), c + offset, new Vector3(size, size * .9f, size), material, parent, false);
        }

        // ================================================================== utilidades
        private static void Remove(string name)
        {
            GameObject old;
            while ((old = GameObject.Find(name)) != null) UnityEngine.Object.DestroyImmediate(old);
        }

        private static void ClearNeighborB()
        {
            var placeholder = GameObject.Find("Casa vecina derecha");
            if (placeholder != null)
            {
                var renderer = placeholder.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.enabled = false;
                var collider = placeholder.GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
            }
            var art = GameObject.Find("SM_Casa_Vecina_B");
            if (art != null) UnityEngine.Object.DestroyImmediate(art);
        }

        private static GameObject Cube(string name, Vector3 position, Vector3 size, Material material, Transform parent,
            bool collider = true, bool local = false, float rollDegrees = 0f)
            => Prim(PrimitiveType.Cube, name, position, size, material, parent, collider, local, rollDegrees);

        private static GameObject Prim(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material,
            Transform parent, bool collider = true, bool local = false, float rollDegrees = 0f)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            if (local) go.transform.localPosition = position; else go.transform.position = position;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(0, 0, rollDegrees);
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void Text(string text, Vector3 position, Quaternion rotation, float characterSize, int fontSize, Color color, Transform parent)
        {
            var go = new GameObject("Texto " + text);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = rotation;
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.font = font;
            mesh.fontSize = fontSize;
            mesh.characterSize = characterSize * TextScale;      // en la app compilada el texto sale ~2.4x mayor que en el editor
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = textMaterial;
        }

        private static void Light(string name, Vector3 position, Transform parent, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private static void Fox(Vector3 center, float scale, Transform parent, Mats m)
        {
            var g = new GameObject("Emblema zorro").transform;
            g.SetParent(parent, false);
            g.position = center;
            g.localScale = Vector3.one * scale;
            Prism("Cabeza", new[] { new Vector2(-.46f, .26f), new Vector2(.46f, .26f), new Vector2(0, -.40f) }, .08f, m.chile, g, Vector3.zero);
            Prism("Oreja izq", new[] { new Vector2(-.46f, .26f), new Vector2(-.12f, .26f), new Vector2(-.40f, .74f) }, .08f, m.chile, g, Vector3.zero);
            Prism("Oreja der", new[] { new Vector2(.12f, .26f), new Vector2(.46f, .26f), new Vector2(.40f, .74f) }, .08f, m.chile, g, Vector3.zero);
            Prism("Oreja interior izq", new[] { new Vector2(-.36f, .30f), new Vector2(-.20f, .30f), new Vector2(-.37f, .55f) }, .02f, m.plum, g, new Vector3(0, 0, -.05f));
            Prism("Oreja interior der", new[] { new Vector2(.20f, .30f), new Vector2(.36f, .30f), new Vector2(.37f, .55f) }, .02f, m.plum, g, new Vector3(0, 0, -.05f));
            Prism("Hocico", new[] { new Vector2(-.24f, -.02f), new Vector2(.24f, -.02f), new Vector2(0, -.40f) }, .06f, m.cream, g, new Vector3(0, 0, -.04f));
            Prism("Ojo izq", new[] { new Vector2(-.30f, .12f), new Vector2(-.12f, .06f), new Vector2(-.14f, .14f) }, .02f, m.plum, g, new Vector3(0, 0, -.06f));
            Prism("Ojo der", new[] { new Vector2(.12f, .06f), new Vector2(.30f, .12f), new Vector2(.14f, .14f) }, .02f, m.plum, g, new Vector3(0, 0, -.06f));
            Prism("Nariz", new[] { new Vector2(-.06f, -.32f), new Vector2(.06f, -.32f), new Vector2(0, -.40f) }, .02f, m.plum, g, new Vector3(0, 0, -.08f));
        }

        private static GameObject Prism(string name, Vector2[] poly, float thickness, Material material, Transform parent, Vector3 localPosition)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();
            float h = thickness / 2f;

            void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 wanted)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), wanted) < 0) { var t = b; b = c; c = t; }
                int i = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                normals.Add(wanted); normals.Add(wanted); normals.Add(wanted);
                triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
            }

            int n = poly.Length;
            Vector2 centroid = Vector2.zero;
            foreach (var v in poly) centroid += v;
            centroid /= n;
            for (int i = 1; i < n - 1; i++)
            {
                Tri(new Vector3(poly[0].x, poly[0].y, -h), new Vector3(poly[i].x, poly[i].y, -h), new Vector3(poly[i + 1].x, poly[i + 1].y, -h), Vector3.back);
                Tri(new Vector3(poly[0].x, poly[0].y, h), new Vector3(poly[i].x, poly[i].y, h), new Vector3(poly[i + 1].x, poly[i + 1].y, h), Vector3.forward);
            }
            for (int i = 0; i < n; i++)
            {
                Vector2 p = poly[i], q = poly[(i + 1) % n];
                Vector2 e = q - p;
                var outward = new Vector3(e.y, -e.x, 0).normalized;
                if (Vector3.Dot(outward, new Vector3(p.x - centroid.x, p.y - centroid.y, 0)) < 0) outward = -outward;
                var a = new Vector3(p.x, p.y, -h); var b = new Vector3(q.x, q.y, -h);
                var c = new Vector3(q.x, q.y, h); var d = new Vector3(p.x, p.y, h);
                Tri(a, b, c, outward);
                Tri(a, c, d, outward);
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }
    }

    public static class CasinoFoxIntegration
    {
        [MenuItem("Tools/Tycoon3D/Integrar arte completo + Casino FOX")]
        public static void IntegrateAllWithCasino()
        {
            CasaMareaPaletteIntegration.IntegrateAll();
            CasinoFoxBuilder.Build();
            ClientesAnimadosIntegrator.Integrate();
        }
    }
}
