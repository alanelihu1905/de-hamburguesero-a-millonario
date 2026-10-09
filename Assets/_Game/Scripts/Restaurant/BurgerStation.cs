using Tycoon3D.Interaction;
using UnityEngine;

namespace Tycoon3D.Restaurant
{
    public enum StationKind { Ingredients, Grill, Counter }

    public sealed class BurgerStation : MonoBehaviour, IInteractable
    {
        [SerializeField] private BurgerLoop burgerLoop;
        [SerializeField] private StationKind kind;

        public string Prompt
        {
            get
            {
                if (burgerLoop == null) return "Estación no disponible";
                switch (kind)
                {
                    case StationKind.Ingredients:
                        if (burgerLoop.Step != BurgerStep.NeedIngredients) return "Termina la hamburguesa actual";
                        return burgerLoop.CanBuyIngredients ? "E - Comprar ingredientes ($15)" : "Dinero insuficiente";
                    case StationKind.Grill:
                        return burgerLoop.Step == BurgerStep.NeedCooking
                            ? "E - Cocinar hamburguesa" : "Primero compra ingredientes";
                    default:
                        return burgerLoop.Step == BurgerStep.ReadyToSell
                            ? "E - Vender hamburguesa ($50)" : "Prepara una hamburguesa primero";
                }
            }
        }

        public void Configure(BurgerLoop loop, StationKind stationKind)
        {
            burgerLoop = loop;
            kind = stationKind;
        }

        public void Interact()
        {
            if (burgerLoop == null) { Debug.LogError("BurgerStation sin BurgerLoop", this); return; }
            switch (kind)
            {
                case StationKind.Ingredients: burgerLoop.BuyIngredients(); break;
                case StationKind.Grill: burgerLoop.Cook(); break;
                case StationKind.Counter: burgerLoop.Sell(); break;
            }
        }
    }
}
