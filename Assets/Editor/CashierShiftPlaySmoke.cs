using System;
using System.IO;
using System.Linq;
using Tycoon3D.Casino;
using Tycoon3D.Core;
using Tycoon3D.Economy;
using Tycoon3D.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tycoon3D.Editor
{
    /// <summary>
    /// Prueba en Play Mode del turno de la cajera: en turno cambia fichas y se ven sobre el mostrador; en descanso no cambia
    /// pesos por fichas, pero el casino sigue abierto (se gira, y las fichas se canjean en la ventanilla automática). El rótulo
    /// muestra el horario y el estado y la cajera se oculta fuera de turno. Marcador: TYCOON_CASHIER_SHIFT_OK (sin -quit).
    /// </summary>
    public static class CashierShiftPlaySmoke
    {
        private const string ScenePath = "Assets/_Game/Scenes/BarrioResidencial.unity";
        private static bool prevOptionsEnabled;
        private static EnterPlayModeOptions prevOptions;
        private static int frames;

        public static void Run()
        {
            try
            {
                GameProfile.DirectoryOverride = Path.Combine(Path.GetTempPath(), "tycoon-smoke-cashier-" + Guid.NewGuid().ToString("N"));
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

        private static CashierDesk desk;
        private static CasinoService casino;
        private static Wallet wallet;
        private static DayNightCycle cycle;

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            frames = 0;
            EditorApplication.update += WaitForStart;
        }

        private static void WaitForStart()
        {
            if (++frames < 4) return;           // Start() de la ventanilla ya corrió y creó su vista
            EditorApplication.update -= WaitForStart;
            try
            {
                desk = UnityEngine.Object.FindFirstObjectByType<CashierDesk>();
                casino = UnityEngine.Object.FindFirstObjectByType<CasinoService>();
                wallet = UnityEngine.Object.FindFirstObjectByType<Wallet>();
                cycle = DayNightCycle.Current;
                Require(desk != null && casino != null && wallet != null && cycle != null, "Faltan la ventanilla, el casino, el monedero o el reloj.");
                Require(desk.View != null && desk.Cashier != null, "La ventanilla no creó su vista o no tiene cajera.");
                cycle.Frozen = true;

                // --- en turno (10:00 al arrancar): cambia fichas y se ven sobre el mostrador
                cycle.SetHour(10f); desk.View.Refresh();
                Require(casino.CashierOnDuty && desk.View.CashierVisible, "A las 10:00 la cajera debe estar en turno y visible.");
                Require(desk.View.SignText.Contains("Turno 08:00–22:00") && desk.View.SignText.Contains("En turno"), "Rótulo en turno: " + desk.View.SignText);
                Require(desk.Prompt.Contains("Cambiar $"), "Letrero en turno: " + desk.Prompt);
                Fund(1000);
                desk.Interact();
                Require(casino.Session.Chips == 20 && wallet.Pesos == 500, "En turno debía cambiar 20 fichas por $500; fichas " + casino.Session.Chips + ", saldo " + wallet.Pesos);
                Require(desk.View.ChipsOnCounter == 20, "Las 20 fichas debían dibujarse sobre el mostrador; hay " + desk.View.ChipsOnCounter);

                // --- en descanso: no cambia pesos por fichas, el casino sigue abierto y el canje funciona
                cycle.SetHour(23f); desk.View.Refresh();
                Require(!casino.CashierOnDuty && !desk.View.CashierVisible, "A las 23:00 la cajera debe estar en descanso y oculta.");
                Require(desk.Cashier.GetComponentsInChildren<Renderer>(true).All(r => !r.enabled), "La cajera en descanso debía quedar oculta.");
                Require(desk.View.SignText.Contains("En descanso") && desk.View.SignText.Contains("08:00"), "Rótulo en descanso: " + desk.View.SignText);
                Require(desk.Prompt.Contains("descanso") && desk.Prompt.Contains("ventanilla automática"), "Letrero en descanso: " + desk.Prompt);
                int pesosAntes = wallet.Pesos, fichasAntes = casino.Session.Chips;
                desk.Interact();
                Require(wallet.Pesos == pesosAntes && casino.Session.Chips == fichasAntes, "En descanso no debía cambiar pesos por fichas.");
                Require(casino.ToastText.Contains("descanso"), "Aviso de descanso: " + casino.ToastText);
                var machine = UnityEngine.Object.FindObjectsByType<SlotMachineStation>(FindObjectsSortMode.None).First();
                Require(machine.Prompt.StartsWith("E - Girar"), "Con la cajera en descanso las máquinas siguen abiertas: " + machine.Prompt);
                Require(casino.TrySpin(machine), "Con la cajera en descanso se debe poder girar.");
                casino.RevealNow();
                int fichasTrasGiro = casino.Session.Chips;
                ((Tycoon3D.Interaction.ISecondaryInteractable)desk).InteractSecondary();
                Require(casino.Session.Chips == 0, "El canje en la ventanilla automática debía vaciar las fichas.");
                Require(wallet.Pesos >= pesosAntes + fichasTrasGiro * CasinoSession.ChipValue, "El canje debía pagar al menos las fichas: saldo " + wallet.Pesos);
                Require(casino.ToastText.Contains("automática"), "Aviso del canje fuera de turno: " + casino.ToastText);

                // --- vuelve a la hora de su turno
                cycle.SetHour(8.5f); desk.View.Refresh();
                Require(casino.CashierOnDuty && desk.View.CashierVisible && desk.Cashier.GetComponentsInChildren<Renderer>(true).Any(r => r.enabled), "A las 08:30 la cajera debe volver.");
                Require(GameProfile.Current.Ledger.Matches(wallet.Pesos, casino.Session.Chips), "El libro común no cuadra tras el turno.");

                frames = 0;
                EditorApplication.update += WaitForChips;
            }
            catch (Exception e) { Fail(e); }
        }

        // las fichas aparecen escalonadas: unas décimas después de comprarlas ya se ven apiladas sobre el mostrador
        private static void WaitForChips()
        {
            try
            {
                if (frames == 0)
                {
                    Fund(1000);
                    desk.Interact();
                    Require(desk.View.ChipsOnCounter > 0, "Debía haber fichas sobre el mostrador tras comprar.");
                }
                if (++frames < 30) return;
                EditorApplication.update -= WaitForChips;
                var visible = desk.View.GetComponentsInChildren<Renderer>().Count(r => r.name.StartsWith("Ficha") && r.gameObject.activeSelf);
                Require(visible > 0, "Pasado un momento las fichas debían verse sobre el mostrador; se ven " + visible);
                Debug.Log("TYCOON_CASHIER_SHIFT_OK: en turno cambia fichas y se ven en el mostrador; en descanso no cambia pesos pero el casino sigue abierto, el canje funciona y la cajera se oculta.");
                Finish(0);
            }
            catch (Exception e) { Fail(e); }
        }

        // pone saldo de prueba y lo anota en el libro común como un ajuste, para que el libro siga cuadrando
        private static void Fund(int target)
        {
            int before = wallet.Pesos;
            wallet.Restore(target);
            int delta = target - before;
            if (delta > 0) GameProfile.Current.Ledger.Record(MovementKind.Adjustment, delta, MovementAccount.Outside, MovementAccount.Wallet);
            else if (delta < 0) GameProfile.Current.Ledger.Record(MovementKind.Adjustment, -delta, MovementAccount.Wallet, MovementAccount.Outside);
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
            EditorApplication.update -= WaitForChips;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            GameProfile.DirectoryOverride = null;
            EditorSettings.enterPlayModeOptions = prevOptions;
            EditorSettings.enterPlayModeOptionsEnabled = prevOptionsEnabled;
            if (code == 0) EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(code);
        }
    }
}
