using System;
using System.IO;
using System.Linq;
using Tycoon3D.Core;
using Tycoon3D.Economy;
using Tycoon3D.Restaurant;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tycoon3D.Editor
{
    /// <summary>
    /// Prueba en Play Mode de los clientes que piden una hamburguesa real: sin nadie esperando la venta paga igual; con un pedido
    /// en curso (cartel sobre el cliente) la venta paga lo mismo una sola vez, el cliente que más esperaba se va caminando a la
    /// calle, repetir la venta no cobra de nuevo y el libro común cuadra. Marcador: TYCOON_ORDERS_OK (no lanzar con -quit).
    /// </summary>
    public static class CustomerOrdersPlaySmoke
    {
        private const string ScenePath = "Assets/_Game/Scenes/BarrioResidencial.unity";
        private static bool prevOptionsEnabled;
        private static EnterPlayModeOptions prevOptions;
        private static int frames;
        private static float walkStart;
        private static AnimatedCustomer paid;
        private static Wallet wallet;
        private static BurgerLoop loop;
        private static BurgerStation[] stations;
        private static CustomerOrders orders;

        public static void Run()
        {
            try
            {
                GameProfile.DirectoryOverride = Path.Combine(Path.GetTempPath(), "tycoon-smoke-orders-" + Guid.NewGuid().ToString("N"));
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

        private static void Sell()
        {
            stations.Single(s => s.name == "Caja de ingredientes").Interact();
            stations.Single(s => s.name == "Parrilla").Interact();
            stations.Single(s => s.name == "Punto de venta").Interact();
        }

        private static void WaitForStart()
        {
            if (++frames < 4) return;
            EditorApplication.update -= WaitForStart;
            try
            {
                wallet = UnityEngine.Object.FindFirstObjectByType<Wallet>();
                loop = UnityEngine.Object.FindFirstObjectByType<BurgerLoop>();
                stations = UnityEngine.Object.FindObjectsByType<BurgerStation>(FindObjectsSortMode.None);
                orders = loop != null ? loop.GetComponent<CustomerOrders>() : null;
                var queue = AnimatedCustomer.All.Where(c => c.Mode == CustomerMode.Queue).ToArray();
                Require(wallet != null && loop != null && stations.Length == 3, "Faltan el monedero, las hamburguesas o las estaciones.");
                Require(orders != null, "No se creó el conector de pedidos con la venta.");
                Require(queue.Length == 2 && queue.All(c => c.Flow != null), "Debe haber 2 clientes en la fila con flujo de pedido.");
                Require(AnimatedCustomer.All.Count(c => c.Mode == CustomerMode.Cashier) == 1 && AnimatedCustomer.All.Count(c => c.Mode == CustomerMode.Stroll) == 1,
                    "La cajera y el paseante no deben tener flujo de pedido.");
                Require(AnimatedCustomer.All.Where(c => c.Mode != CustomerMode.Queue).All(c => c.Flow == null), "Solo los de la fila llevan flujo.");

                // 1) sin nadie esperando: venta de paso, mismo precio
                foreach (var c in queue) Require(!c.Flow.HasOrder || c.Flow.WaitingSeconds < 20f, "Un cliente lleva demasiado esperando al arrancar.");
                var esperando = queue.Where(c => c.Flow.HasOrder).ToArray();
                foreach (var c in esperando) c.Flow.TryPay();           // se despeja la fila para medir la venta sin cliente
                if (esperando.Length > 0) { /* los que estaban pidiendo ya se fueron contentos: esta venta no es la medida */ }
                int servidosAntes = queue.Sum(c => c.Flow.Served);
                int pesosAntes = wallet.Pesos;
                Require(queue.All(c => !c.Flow.HasOrder), "No debía quedar nadie pidiendo.");
                Sell();
                Require(wallet.Pesos == pesosAntes + 35 && loop.Sales == 1, "Sin cliente esperando la venta debe pagar igual (+$35 netos); saldo " + wallet.Pesos);
                Require(queue.Sum(c => c.Flow.Served) == servidosAntes && orders.Served == 0, "Sin pedidos nadie debía pagar.");

                // 2) con un pedido en curso: cartel sobre el cliente
                var A = queue[0];
                var B = queue[1];
                // se aparta B del flujo para que la prueba sea determinista: ni pide ni paga durante la medida
                A.Flow.ForceOrder();
                Require(A.Flow.HasOrder, "ForceOrder debía abrir un pedido (estado " + A.Flow.State + ").");
                frames = 0;
                paid = null;
                EditorApplication.update += WaitForOrderVisible;
            }
            catch (Exception e) { Fail(e); }
        }

        private static void WaitForOrderVisible()
        {
            if (++frames < 4) return;
            EditorApplication.update -= WaitForOrderVisible;
            try
            {
                var queue = AnimatedCustomer.All.Where(c => c.Mode == CustomerMode.Queue).ToArray();
                var waiting = queue.Where(c => c.Flow.HasOrder).ToArray();
                Require(waiting.Length >= 1, "Debía haber al menos un cliente con pedido.");
                bool hasTemplate = GameObject.Find("Texto CAJERA") != null;
                if (hasTemplate)
                    Require(waiting.All(c => c.BubbleText.StartsWith("HAMBURGUESA $50")), "Cartel del pedido: " + string.Join(" | ", waiting.Select(c => c.BubbleText)));

                int pesosAntes = wallet.Pesos, ventasAntes = loop.Sales;
                int servidosAntes = queue.Sum(c => c.Flow.Served);
                var masTiempo = waiting.OrderByDescending(c => c.Flow.WaitingSeconds).First();
                Sell();
                Require(wallet.Pesos == pesosAntes + 35 && loop.Sales == ventasAntes + 1, "La venta con cliente debe pagar lo mismo (+$35 netos); saldo " + wallet.Pesos);
                Require(queue.Sum(c => c.Flow.Served) == servidosAntes + 1 && orders.Served == 1, "Debía pagar exactamente un cliente.");
                paid = orders.LastServed;
                Require(paid == masTiempo, "Debía pagar el cliente que más esperaba.");
                Require(paid.Flow.State == OrderState.Leaving && paid.Flow.LeftHappy, "El cliente servido debía irse contento.");
                int pesosTras = wallet.Pesos;
                stations.Single(s => s.name == "Punto de venta").Interact();          // repetir la venta sin hamburguesa lista
                Require(wallet.Pesos == pesosTras && orders.Served == 1, "Repetir la venta no debe cobrar ni servir a otro cliente.");
                Require(GameProfile.Current.Ledger.Matches(wallet.Pesos, 0), "El libro común no cuadra tras las ventas con clientes.");
                walkStart = Time.realtimeSinceStartup;
                frames = 0;
                EditorApplication.update += WaitForWalk;
            }
            catch (Exception e) { Fail(e); }
        }

        private static bool sawWalking;

        private static void WaitForWalk()
        {
            try
            {
                sawWalking |= paid.IsWalking;
                if (Time.realtimeSinceStartup - walkStart < 4f) return;
                EditorApplication.update -= WaitForWalk;
                float lejos = Vector3.Distance(paid.transform.position, paid.Home);
                Require(sawWalking, "El cliente servido debía caminar.");
                Require(lejos > 2f, "El cliente servido debía alejarse del mostrador; está a " + lejos.ToString("F1") + " m.");
                Debug.Log("TYCOON_ORDERS_OK: sin pedido la venta paga igual; con pedido paga lo mismo una sola vez, el cliente que más esperaba se va caminando, no hay cobro doble y el libro cuadra.");
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
            EditorApplication.update -= WaitForOrderVisible;
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
