using System;
using System.Reflection;
using Tycoon3D.Economy;
using Tycoon3D.Restaurant;
using Tycoon3D.Save;

var wallet = new Wallet();
typeof(Wallet).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(wallet, null);
var loop = new BurgerLoop();
typeof(BurgerLoop).GetField("wallet", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(loop, wallet);
var ledger = new MovementLedger();
ledger.Restore(new SaveData(), wallet.Pesos, 0);
loop.MovementRecorded += (kind, amount, from, to) => ledger.Record(kind, amount, from, to);

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
Check(ledger.Matches(wallet.Pesos, 0) && ledger.Entries.Count == 5, "Libro registra dos compras y dos ventas");

// ---- precios y receta como datos editables ---------------------------------------------------------------------
{
    var defaults = EconomyConfig.Default;
    Check(defaults.IngredientCost == 15 && defaults.SalePrice == 50 && defaults.ChipLot == 20, "datos: los valores por defecto son los de siempre");
    Check(EconomyConfig.TryParse(defaults.ToJson(), out var again, out _) && again.IngredientCost == 15 && again.SalePrice == 50 && again.ChipLot == 20,
        "datos: el JSON por defecto se lee igual");
    Check(EconomyConfig.TryParse("{ \"version\": 1, \"ingredientCost\": 20, \"salePrice\": 80, \"chipLot\": 10 }", out var custom, out _) &&
        custom.IngredientCost == 20 && custom.SalePrice == 80 && custom.ChipLot == 10, "datos: un JSON con otros precios se lee");
    Check(EconomyConfig.TryParse("{\"salePrice\": 70}", out var partial, out _) && partial.SalePrice == 70 && partial.IngredientCost == 15 && partial.ChipLot == 20,
        "datos: una clave ausente toma el valor por defecto");
    Check(EconomyConfig.TryParse("{\"salePrice\": 70, \"colorDelLetrero\": 3}", out var extra, out _) && extra.SalePrice == 70, "datos: una clave desconocida se ignora");

    foreach (var bad in new[] { "", "   ", "[]", "{", "{\"salePrice\": }", "{\"salePrice\": 5.5}", "{\"salePrice\": \"50\"}", "{\"salePrice\": 50} basura",
                 "{\"salePrice\": 50, \"salePrice\": 60}", "{\"salePrice\": 0}", "{\"salePrice\": -3}", "{\"ingredientCost\": 2000000}",
                 "{\"chipLot\": 0}", "{\"chipLot\": 5000}", "{\"version\": 0}", "{\"version\": 2, \"salePrice\": 99}", "{\"salePrice\": 99999999999}" })
    {
        bool ok = EconomyConfig.TryParse(bad, out var fallback, out string error);
        Check(!ok && error != null && fallback.SalePrice == 50 && fallback.IngredientCost == 15 && fallback.ChipLot == 20, "datos: se rechaza y deja los valores por defecto: " + bad);
    }

    // un cambio de datos se refleja en el ciclo sin tocar código
    var w2 = new Wallet();
    typeof(Wallet).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(w2, null);
    var loop2 = new BurgerLoop();
    typeof(BurgerLoop).GetField("wallet", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(loop2, w2);
    var ledger2 = new MovementLedger();
    ledger2.Restore(new SaveData(), w2.Pesos, 0);
    loop2.MovementRecorded += (kind, amount, from, to) => ledger2.Record(kind, amount, from, to);
    loop2.ApplyConfig(custom);
    Check(loop2.IngredientCost == 20 && loop2.SalePrice == 80, "datos: la hamburguesería usa los precios nuevos");
    loop2.BuyIngredients();
    Check(w2.Pesos == 80, "datos: los ingredientes cuestan $20 con el archivo nuevo");
    loop2.Cook(); loop2.Sell();
    Check(w2.Pesos == 160 && loop2.Sales == 1, "datos: la venta paga $80 con el archivo nuevo");
    Check(ledger2.Matches(w2.Pesos, 0), "datos: el libro cuadra con los precios nuevos");
    loop2.ApplyConfig(null);
    Check(loop2.IngredientCost == 20, "datos: una configuración nula no cambia nada");
    loop2.ApplyConfig(defaults);
    Check(loop2.IngredientCost == 15 && loop2.SalePrice == 50, "datos: volver a los valores por defecto");
}

Console.WriteLine("EconomySmoke: {0} comprobaciones aprobadas.", Counter.Checks);

static void Check(bool condition, string description)
{
    Counter.Checks++;
    if (!condition) throw new Exception("Falló: " + description);
}

static class Counter { public static int Checks; }
