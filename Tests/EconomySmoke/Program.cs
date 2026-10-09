using System;
using System.Reflection;
using Tycoon3D.Economy;
using Tycoon3D.Restaurant;

var wallet = new Wallet();
typeof(Wallet).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(wallet, null);
var loop = new BurgerLoop();
typeof(BurgerLoop).GetField("wallet", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(loop, wallet);

Check(wallet.Pesos == 100, "Saldo inicial");
loop.Sell();
Check(wallet.Pesos == 100 && loop.Sales == 0, "No vende sin producto");
loop.BuyIngredients();
Check(wallet.Pesos == 85 && loop.Step == BurgerStep.NeedCooking, "Compra ingredientes");
loop.BuyIngredients();
Check(wallet.Pesos == 85, "No duplica compra");
loop.Cook();
Check(loop.Step == BurgerStep.ReadyToSell, "Cocción");
loop.Sell();
Check(wallet.Pesos == 135 && loop.Sales == 1, "Primera venta");
loop.Sell();
Check(wallet.Pesos == 135 && loop.Sales == 1, "No duplica venta");
loop.BuyIngredients();
loop.Cook();
loop.Sell();
Check(wallet.Pesos == 170 && loop.Sales == 2, "Segunda venta");
Check(!wallet.TrySpend(-1), "Rechaza gasto negativo");

Console.WriteLine("EconomySmoke: 8 comprobaciones aprobadas.");

static void Check(bool condition, string description)
{
    if (!condition) throw new Exception("Falló: " + description);
}
