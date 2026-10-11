using Tycoon3D.Interaction;
using Tycoon3D.Restaurant;
using UnityEngine;

namespace Tycoon3D.Casino
{
    /// <summary>
    /// Ventanilla de la cajera del casino. E cambia pesos por fichas (un lote de 20 o lo que alcance), Q canjea todas las fichas
    /// por pesos al mismo valor. Es un servicio, no una puerta: no bloquea ninguna máquina ni exige fichas para jugar.
    /// </summary>
    public sealed class CashierDesk : MonoBehaviour, IInteractable, ISecondaryInteractable
    {
        [SerializeField] private CasinoService service;
        [SerializeField] private AnimatedCustomer cashier;

        public AnimatedCustomer Cashier => cashier;

        public void Configure(CasinoService casino, AnimatedCustomer npc)
        {
            service = casino;
            cashier = npc;
        }

        public string Prompt
        {
            get
            {
                if (service == null) return string.Empty;
                var s = service.Session;
                int buy = service.ChipsToBuy;
                string left = buy > 0 ? InputBindings.InteractLabel + " - Cambiar $" + buy * CasinoSession.ChipValue + " por " + buy + " fichas" : InputBindings.InteractLabel + " - Sin pesos para fichas";
                string right = s.Chips > 0 ? InputBindings.SecondaryLabel + " - Canjear " + s.Chips + " fichas por $" + s.ChipsValue : InputBindings.SecondaryLabel + " - Canjear (sin fichas)";
                return "Cajera FOX · " + left + " · " + right + " · 1 ficha = $" + CasinoSession.ChipValue;
            }
        }

        public void Interact()
        {
            if (service == null) return;
            if (cashier != null) cashier.Gesture();
            service.BuyChips();
        }

        public void InteractSecondary()
        {
            if (service == null) return;
            if (cashier != null) cashier.Gesture();
            service.CashOutChips();
        }
    }
}
