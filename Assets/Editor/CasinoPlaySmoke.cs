using System;
using System.IO;
using System.Linq;
using Tycoon3D.Core;
using Tycoon3D.Save;
using Tycoon3D.Casino;
using Tycoon3D.Economy;
using Tycoon3D.Interaction;
using Tycoon3D.Restaurant;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tycoon3D.Editor
{
    /// <summary>
    /// Prueba de Play Mode del casino. Fase 1: el casino existe desde el inicio, se llega caminando, la puerta está libre, se
    /// puede apostar sin desbloquear nada, el giro de cortesía evita quedarse sin juego y la compra-cocción-venta sigue
    /// intacta. Fase 2: tras recargar la escena (lo que hace la tecla R) el casino sigue ahí y funciona.
    /// </summary>
    public static class CasinoPlaySmoke
    {
        private const string ScenePath = "Assets/_Game/Scenes/BarrioResidencial.unity";
        private static bool prevOptionsEnabled;
        private static EnterPlayModeOptions prevOptions;
        private static int reloadFrames = -1;
        private static int expectedPesos, expectedRounds, expectedSales;
        private static string saveDir;

        private sealed class Scripted : IRandomSource
        {
            private readonly System.Collections.Generic.Queue<int> values;
            private readonly System.Random fallback = new System.Random(5);
            public Scripted(params int[] v) { values = new System.Collections.Generic.Queue<int>(v); }
            public int Next(int max) => values.Count > 0 ? Math.Min(values.Dequeue(), max - 1) : fallback.Next(max);
        }

        public static void Run()
        {
            try
            {
                saveDir = Path.Combine(Path.GetTempPath(), "tycoon-smoke-" + Guid.NewGuid().ToString("N"));
                if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
                GameProfile.DirectoryOverride = saveDir;                       // perfil limpio y aparte del real
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
            try
            {
                PhaseOne();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);   // equivale a pulsar R
                reloadFrames = 0;
                EditorApplication.update += WaitForReload;
            }
            catch (Exception e) { Fail(e); }
        }

        private static void WaitForReload()
        {
            if (++reloadFrames < 6) return;
            EditorApplication.update -= WaitForReload;
            try
            {
                PhaseTwo();
                Debug.Log("TYCOON_CASINO_SMOKE_OK: casino presente al inicio y tras recargar; apuesta, jackpot, cortesía y venta de hamburguesa verificados.");
                Finish(0);
            }
            catch (Exception e) { Fail(e); }
        }

        private static void PhaseOne()
        {
            var wallet = UnityEngine.Object.FindFirstObjectByType<Wallet>();
            var loop = UnityEngine.Object.FindFirstObjectByType<BurgerLoop>();
            var casino = UnityEngine.Object.FindFirstObjectByType<CasinoService>();
            var hud = UnityEngine.Object.FindFirstObjectByType<CasinoHud>();
            var machines = UnityEngine.Object.FindObjectsByType<SlotMachineStation>(FindObjectsSortMode.None);
            var burger = UnityEngine.Object.FindObjectsByType<BurgerStation>(FindObjectsSortMode.None);
            Physics.SyncTransforms();
            Require(wallet != null && loop != null, "Faltan Wallet o BurgerLoop.");
            Require(casino != null && hud != null, "Falta el servicio o el HUD del casino.");
            Require(machines.Length == 6, "El casino debe tener 6 tragamonedas; hay " + machines.Length);
            Require(machines.Select(x => x.Kind).Distinct().Count() == 3, "Debe haber tres tipos distintos de máquina.");
            var customers = UnityEngine.Object.FindObjectsByType<AnimatedCustomer>(FindObjectsSortMode.None);
            Require(customers.Length == 4, "Debe haber 4 clientes animados (fila, paseante y cajera); hay " + customers.Length);
            Require(customers.Count(c => c.Mode == CustomerMode.Queue) == 2 && customers.Count(c => c.Mode == CustomerMode.Stroll) == 1 && customers.Count(c => c.Mode == CustomerMode.Cashier) == 1, "Clientes: 2 en la fila, 1 paseante y 1 cajera.");
            foreach (var c in customers)
            {
                var an = c.Animator;
                Require(an != null && an.runtimeAnimatorController != null, "Cliente sin Animator/controlador: " + c.name);
                Require(an.avatar != null && an.avatar.isValid, "Avatar no válido en " + c.name);
                var clips = an.runtimeAnimatorController.animationClips;
                foreach (var need in new[] { "Idle", "Caminar", "Pedir" })
                    Require(clips.Any(k => k.name == need && k.length > .5f), "Falta el clip " + need + " en " + c.name);
            }
            Require(machines.All(x => x.Drums.Length == 3 && x.Drums.All(d => d != null)), "Cada máquina necesita tres rodillos.");
            Require(casino.Sounds != null && casino.Sounds.Complete, "Faltan sonidos del casino.");
            Require(casino.SymbolAtlas != null, "Falta el atlas de símbolos.");
            Require(GameObject.Find("Casino FOX").GetComponentsInChildren<Transform>().Count(t => t.name == "stoolBar") >= 6, "Faltan los taburetes CC0 junto a las máquinas.");
            Require(burger.Length == 3, "Las tres estaciones de hamburguesas deben seguir en la escena.");
            Require(wallet.Pesos == 100, "Saldo inicial distinto de $100.");
            Require(GameObject.Find("Casino FOX") != null, "Falta el edificio Casino FOX.");

            // se llega caminando desde el inicio: suelo continuo bajo una ruta desde el punto de aparición hasta el fondo del casino
            var route = new[] { new Vector3(0, 0, -8.5f), new Vector3(9.5f, 0, -6f), new Vector3(16f, 0, -3.2f), new Vector3(16f, 0, 2.4f) };
            for (int i = 0; i < route.Length - 1; i++)
                for (float t = 0; t <= 1.001f; t += .05f)
                {
                    Vector3 p = Vector3.Lerp(route[i], route[i + 1], t);
                    var hits = Physics.RaycastAll(new Vector3(p.x, 3f, p.z), Vector3.down, 6f);
                    Require(hits.Any(h => h.point.y > -.2f && h.point.y < .1f),
                        "No hay suelo caminable en " + p + " de la ruta al casino; colliders hallados: " +
                        string.Join(", ", hits.Select(h => h.collider.name + "@" + h.point.y.ToString("F2"))));
                }
            // la puerta y el pasillo están libres para la cápsula del jugador (radio .35, alto 1.8)
            for (float z = -6f; z <= 2.4f; z += .4f)
                Require(!Physics.CheckCapsule(new Vector3(16f, .45f, z), new Vector3(16f, 1.45f, z), .34f),
                    "La ruta al casino está bloqueada en z=" + z);

            // cada máquina se alcanza con el raycast de interacción
            var camera = Camera.main;
            Require(camera != null, "Sin cámara principal.");
            foreach (var m in machines)
            {
                camera.transform.position = m.transform.position + new Vector3(0, 1.6f, -1.6f);
                camera.transform.LookAt(m.transform.position + new Vector3(0, 1.3f, 0));
                Physics.SyncTransforms();
                Require(Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, 3f) &&
                        hit.collider.GetComponentInParent<SlotMachineStation>() == m,
                    "El raycast no alcanza " + m.name);
                Require(m.Prompt.StartsWith("E - Girar ($25)"), "Prompt inicial inesperado en " + m.name + ": " + m.Prompt);
            }

            // apuesta sin desbloqueos: tirada 99 = jackpot FOX (x40 sobre $25 = $1000)
            casino.UseRandom(new Scripted(99));
            var first = machines.First(x => x.Kind == SlotMachineKind.Clasica);
            first.Interact();
            Require(casino.Spinning && wallet.Pesos == 75, "La apuesta de $25 no se cobró al girar.");
            Require(first.Prompt.StartsWith("Girando"), "El prompt debe indicar que está girando.");
            first.Interact();
            Require(wallet.Pesos == 75, "No debe cobrarse un segundo giro simultáneo.");
            casino.RevealNow();
            Require(wallet.Pesos == 1075 && !casino.Spinning, "El jackpot FOX debía dejar $1075; saldo " + wallet.Pesos);
            Require(first.Drums.All(d => d.SymbolAtFront() == SlotSymbol.Fox), "Los rodillos no quedaron en el símbolo FOX.");
            Require(casino.Message.Contains("JACKPOT"), "Mensaje de jackpot ausente.");

            // la compra-cocción-venta sigue intacta con el casino de por medio
            var ingredients = burger.Single(s => s.name == "Caja de ingredientes");
            var grill = burger.Single(s => s.name == "Parrilla");
            var counter = burger.Single(s => s.name == "Punto de venta");
            ingredients.Interact(); grill.Interact(); counter.Interact();
            Require(wallet.Pesos == 1110 && loop.Sales == 1, "La venta tras el casino no dejó $1110; saldo " + wallet.Pesos);

            // cambiar la apuesta (Q) y giro de cortesía con saldo cero
            ((ISecondaryInteractable)first).InteractSecondary();
            Require(casino.Session.CurrentBet == 100 && first.Prompt.StartsWith("E - Girar ($100)"), "Q no cambió la apuesta.");
            wallet.TrySpend(wallet.Pesos);
            Require(wallet.Pesos == 0 && casino.Session.NextSpinIsFree && first.Prompt.Contains("cortesía"),
                "Con saldo $0 el casino debe ofrecer un giro de cortesía.");
            casino.UseRandom(new Scripted(68));                      // trío de chiles x3 sobre $25
            first.Interact();
            Require(wallet.Pesos == 0, "El giro de cortesía no debe cobrar.");
            casino.RevealNow();
            Require(wallet.Pesos == 75, "El giro de cortesía ganador debía dejar $75; saldo " + wallet.Pesos);

            // ---- perfil: el estado vivo está escrito en disco y la ventanilla de la cajera funciona
            var profile = GameProfile.Current;
            Require(profile != null && profile.Directory == saveDir, "Falta el perfil o usa otro directorio.");
            profile.Flush();
            Require(File.Exists(Path.Combine(saveDir, SaveRepository.MainFile)) && profile.Data.Pesos == 75 && profile.Data.BurgerSales == 1,
                "El perfil no quedó escrito con $75 y 1 venta.");

            var desk = UnityEngine.Object.FindFirstObjectByType<CashierDesk>();
            Require(desk != null && desk.Cashier != null && desk.Cashier.Mode == CustomerMode.Cashier, "Falta la ventanilla con su cajera.");
            Require(desk.Prompt.Contains("1 ficha = $25") && desk.Prompt.Contains("Cambiar $75 por 3 fichas"), "Prompt de la cajera inesperado: " + desk.Prompt);
            Camera camera2 = Camera.main;
            Require(camera2 != null, "Sin cámara principal.");
            camera2.transform.position = new Vector3(18.4f, 1.6f, 2.5f);
            camera2.transform.LookAt(new Vector3(20.0f, 1.2f, 2.5f));
            Physics.SyncTransforms();
            Require(Physics.Raycast(camera2.transform.position, camera2.transform.forward, out RaycastHit deskHit, 3f) &&
                    deskHit.collider.GetComponentInParent<CashierDesk>() == desk, "El raycast no alcanza la ventanilla de la cajera.");

            casino.SetBet(0);
            wallet.Earn(925);                                                  // $1000 para el caso normal
            int worth = casino.Session.NetWorth;
            desk.Interact();
            Require(wallet.Pesos == 500 && casino.Session.Chips == 20 && casino.Session.NetWorth == worth, "Cambiar un lote debía dar 20 fichas por $500; $" + wallet.Pesos + " y " + casino.Session.Chips + " fichas.");
            Require(Time.time - desk.Cashier.LastGestureTime < 1f, "La cajera no hizo su gesto de atención.");
            Require(casino.ToastText.Contains("20 fichas") && casino.ToastText.Contains("$500"), "Aviso de la cajera inesperado: " + casino.ToastText);
            Require(first.Prompt.StartsWith("E - Girar ($25) con 1 ficha"), "La máquina debía anunciar que paga con fichas: " + first.Prompt);

            casino.UseRandom(new Scripted(99));
            first.Interact();                                                  // $25 = 1 ficha, jackpot x40
            Require(wallet.Pesos == 500 && casino.Session.Chips == 19, "La apuesta debía salir de las fichas (19) y no de los pesos ($500).");
            desk.Interact();
            Require(wallet.Pesos == 500 && casino.Session.Chips == 19, "La cajera no debe operar mientras giran los rodillos.");
            casino.RevealNow();
            Require(wallet.Pesos == 500 && casino.Session.Chips == 59 && casino.Session.LastPaidWithChips, "El premio debía volver en fichas (59); " + casino.Session.Chips);
            Require(casino.Session.NetWorth == worth - 25 + 1000, "El valor total no cuadra tras jugar con fichas.");

            ((ISecondaryInteractable)desk).InteractSecondary();
            Require(casino.Session.Chips == 0 && wallet.Pesos == 500 + 59 * 25, "Canjear las 59 fichas debía dar $1975; saldo " + wallet.Pesos);
            ((ISecondaryInteractable)desk).InteractSecondary();
            Require(wallet.Pesos == 1975 && casino.ToastText.Contains("No tienes fichas"), "Un segundo canje no debe pagar de nuevo.");

            // ronda aceptada y sin liquidar al cerrar la app: se liquida una sola vez al volver
            casino.UseRandom(new Scripted(99));
            first.Interact();
            Require(casino.Spinning && wallet.Pesos == 1950, "La ronda pendiente no se cobró.");
            profile.Flush();
            Require(profile.Data.PendingActive == 1 && profile.Data.PendingPayout == 1000 && profile.Data.Pesos == 1950, "La ronda pendiente no quedó en el perfil.");
            expectedPesos = 1950 + 1000;
            expectedRounds = casino.Session.RoundsStarted;
            expectedSales = loop.Sales;
            profile.ReloadFromDisk();                                          // como cerrar y reabrir la app: la memoria se descarta y se lee el disco
            Require(profile.Outcome == LoadOutcome.Loaded && profile.Data.PendingActive == 1, "Releer el disco debía traer la ronda pendiente.");
        }

        private static void PhaseTwo()
        {
            var wallet = UnityEngine.Object.FindFirstObjectByType<Wallet>();
            var loop = UnityEngine.Object.FindFirstObjectByType<BurgerLoop>();
            var casino = UnityEngine.Object.FindFirstObjectByType<CasinoService>();
            var machines = UnityEngine.Object.FindObjectsByType<SlotMachineStation>(FindObjectsSortMode.None);
            var profile = GameProfile.Current;
            Require(wallet != null && casino != null && profile != null, "Tras recargar faltan Wallet, el servicio del casino o el perfil.");
            Require(machines.Length == 6, "Tras recargar el casino debe tener 6 tragamonedas; hay " + machines.Length);
            Require(GameObject.Find("Casino FOX") != null, "Tras recargar falta el edificio.");
            Require(wallet.Pesos != 100, "Recargar no debe devolver los $100 iniciales.");
            Require(wallet.Pesos == expectedPesos, "Tras recargar el saldo debía ser $" + expectedPesos + " (ronda pendiente liquidada una vez); es $" + wallet.Pesos);
            Require(loop.Sales == expectedSales, "Las ventas de hamburguesas no se conservaron: " + loop.Sales);
            Require(casino.Session.RoundsStarted == expectedRounds && casino.Session.TotalSpins >= 1 && !casino.Session.Busy, "El libro del casino no se conservó.");
            Require(casino.ToastText.Contains("giro pendiente") && casino.ToastText.Contains("$1000"), "Falta el aviso de la ronda liquidada: " + casino.ToastText);
            profile.Flush();
            Require(profile.Data.PendingActive == 0 && profile.Data.Pesos == expectedPesos, "El perfil debía quedar sin ronda pendiente.");
            var desk = UnityEngine.Object.FindFirstObjectByType<CashierDesk>();
            Require(desk != null && desk.Cashier != null, "Tras recargar falta la ventanilla o la cajera.");

            casino.UseRandom(new Scripted(99));
            machines.First(x => x.Kind == SlotMachineKind.Clasica).Interact();
            casino.RevealNow();
            Require(wallet.Pesos == expectedPesos - 25 + 1000, "Tras recargar el casino debe seguir pagando; saldo " + wallet.Pesos);

            // un perfil principal dañado se recupera desde la copia de seguridad sin perder el progreso
            profile.Flush();
            File.WriteAllText(Path.Combine(saveDir, SaveRepository.MainFile), "archivo dañado");
            profile.ReloadFromDisk(false);                                     // sin guardar antes: lo dañado está en disco, como tras un cierre sucio
            Require(profile.Outcome == LoadOutcome.RecoveredFromBackup && profile.Data.HasProfile && profile.Data.Pesos >= expectedPesos - 25,
                "Un principal dañado debía recuperarse de la copia: " + profile.Outcome);
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
            EditorApplication.update -= WaitForReload;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorSettings.enterPlayModeOptions = prevOptions;
            EditorSettings.enterPlayModeOptionsEnabled = prevOptionsEnabled;
            if (code == 0) EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(code);
        }
    }
}
