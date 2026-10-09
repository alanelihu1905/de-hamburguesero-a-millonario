using Tycoon3D.Economy;
using UnityEngine;

namespace Tycoon3D.Restaurant
{
    public enum BurgerStep { NeedIngredients, NeedCooking, ReadyToSell }

    public sealed class BurgerLoop : MonoBehaviour
    {
        [SerializeField] private Wallet wallet;
        [SerializeField] private int ingredientCost = 15;
        [SerializeField] private int salePrice = 50;

        public BurgerStep Step { get; private set; }
        public int Sales { get; private set; }
        public bool CanBuyIngredients => wallet != null && wallet.Pesos >= ingredientCost;
        public string Message { get; private set; } = "Compra ingredientes para empezar.";

        private void Awake()
        {
            if (wallet == null) wallet = FindFirstObjectByType<Wallet>();
        }

        public void BuyIngredients()
        {
            if (Step != BurgerStep.NeedIngredients) { Message = "Termina la hamburguesa actual."; return; }
            if (wallet == null || !wallet.TrySpend(ingredientCost)) { Message = "No tienes dinero suficiente."; return; }
            Step = BurgerStep.NeedCooking;
            Message = "Ingredientes comprados. Usa la parrilla.";
        }

        public void Cook()
        {
            if (Step != BurgerStep.NeedCooking) { Message = "Primero compra ingredientes."; return; }
            Step = BurgerStep.ReadyToSell;
            Message = "Hamburguesa lista. Llévala al mostrador.";
        }

        public void Sell()
        {
            if (Step != BurgerStep.ReadyToSell) { Message = "Prepara una hamburguesa primero."; return; }
            if (wallet == null) { Message = "Falta el monedero de la escena."; return; }
            wallet.Earn(salePrice);
            Sales++;
            Step = BurgerStep.NeedIngredients;
            Message = "¡Venta completada! +$" + salePrice + ". Compra más ingredientes.";
        }
    }
}
