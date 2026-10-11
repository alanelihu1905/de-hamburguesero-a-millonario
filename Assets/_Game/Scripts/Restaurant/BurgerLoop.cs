using System;
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
        /// <summary>Se dispara cuando cambia el paso o las ventas (para guardar el perfil).</summary>
        public event Action Changed;
        public event Action<MovementKind, int, MovementAccount, MovementAccount> MovementRecorded;

        public bool CanBuyIngredients => wallet != null && wallet.Pesos >= ingredientCost;
        public string Message { get; private set; } = "Compra ingredientes para empezar.";

        private void Awake()
        {
            if (wallet == null) wallet = FindFirstObjectByType<Wallet>();
        }

        /// <summary>Restaura el paso de la hamburguesa y las ventas desde el perfil guardado.</summary>
        public void Restore(int step, int sales)
        {
            Step = step >= 0 && step <= (int)BurgerStep.ReadyToSell ? (BurgerStep)step : BurgerStep.NeedIngredients;
            Sales = Math.Max(0, sales);
            Message = Step == BurgerStep.NeedIngredients ? "Compra ingredientes para empezar."
                    : Step == BurgerStep.NeedCooking ? "Ingredientes comprados. Usa la parrilla."
                    : "Hamburguesa lista. Llévala al mostrador.";
        }

        public void BuyIngredients()
        {
            if (Step != BurgerStep.NeedIngredients) { Message = "Termina la hamburguesa actual."; return; }
            if (wallet == null || !wallet.TrySpend(ingredientCost)) { Message = "No tienes dinero suficiente."; return; }
            MovementRecorded?.Invoke(MovementKind.Ingredients, ingredientCost, MovementAccount.Wallet, MovementAccount.Outside);
            Step = BurgerStep.NeedCooking;
            Message = "Ingredientes comprados. Usa la parrilla.";
            Changed?.Invoke();
        }

        public void Cook()
        {
            if (Step != BurgerStep.NeedCooking) { Message = "Primero compra ingredientes."; return; }
            Step = BurgerStep.ReadyToSell;
            Message = "Hamburguesa lista. Llévala al mostrador.";
            Changed?.Invoke();
        }

        public void Sell()
        {
            if (Step != BurgerStep.ReadyToSell) { Message = "Prepara una hamburguesa primero."; return; }
            if (wallet == null) { Message = "Falta el monedero de la escena."; return; }
            wallet.Earn(salePrice);
            MovementRecorded?.Invoke(MovementKind.Sale, salePrice, MovementAccount.Outside, MovementAccount.Wallet);
            Sales++;
            Step = BurgerStep.NeedIngredients;
            Message = "¡Venta completada! +$" + salePrice + ". Compra más ingredientes.";
            Changed?.Invoke();
        }
    }
}
