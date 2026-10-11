using System;
using System.Collections.Generic;
using System.Linq;
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

// ---- reloj del juego y luz del día ---------------------------------------------------------------------------------
{
    var clock = new Tycoon3D.Core.GameClock();
    Check(clock.Day == 1 && clock.MinuteOfDay == 600 && clock.Text == "Día 1 · 10:00", "reloj: empieza el día 1 a las 10:00");
    clock.Advance(Tycoon3D.Core.GameClock.DefaultRealSecondsPerDay / 24f);
    Check(clock.MinuteOfDay == 660 && clock.Text == "Día 1 · 11:00", "reloj: un día dura 480 s reales, una hora 20 s");
    clock.Advance(-5f); clock.Advance(float.NaN); clock.Advance(float.PositiveInfinity);
    Check(clock.MinuteOfDay == 660, "reloj: un tiempo negativo o inválido no mueve la hora");
    clock.Advance(Tycoon3D.Core.GameClock.DefaultRealSecondsPerDay);
    Check(clock.Day == 2 && clock.MinuteOfDay == 660, "reloj: pasadas 24 horas es el día 2 a la misma hora");
    clock.RealSecondsPerDay = 1f;
    Check(clock.RealSecondsPerDay == Tycoon3D.Core.GameClock.MinRealSecondsPerDay, "reloj: la velocidad máxima está acotada");
    clock.SetHour(25.5f, 3);
    Check(clock.Day == 3 && Math.Abs(clock.Hour - 1.5f) < 0.001f, "reloj: SetHour da la vuelta a las 24 horas");
    clock.Set(-100); Check(clock.MinuteOfDay == 600 && clock.Day == 1, "reloj: un valor guardado inválido vuelve al inicio");

    Check(Tycoon3D.Core.GameClock.PeriodOf(3f) == Tycoon3D.Core.DayPeriod.Madrugada && Tycoon3D.Core.GameClock.PeriodOf(9f) == Tycoon3D.Core.DayPeriod.Manana &&
          Tycoon3D.Core.GameClock.PeriodOf(15f) == Tycoon3D.Core.DayPeriod.Tarde && Tycoon3D.Core.GameClock.PeriodOf(22f) == Tycoon3D.Core.DayPeriod.Noche, "reloj: periodos del día");
    Check(Tycoon3D.Core.GameClock.Daylight(2f) == 0f && Tycoon3D.Core.GameClock.Daylight(12f) == 1f && Tycoon3D.Core.GameClock.Daylight(23f) == 0f, "luz: noche cerrada a las 02:00 y 23:00, pleno a las 12:00");
    Check(Tycoon3D.Core.GameClock.Daylight(6.5f) > 0.1f && Tycoon3D.Core.GameClock.Daylight(6.5f) < 0.9f && Tycoon3D.Core.GameClock.Daylight(18.5f) > 0.1f && Tycoon3D.Core.GameClock.Daylight(18.5f) < 0.9f, "luz: amanecer y atardecer son intermedios");
    float previous = Tycoon3D.Core.GameClock.Daylight(0f), biggestStep = 0f;
    for (float h = 0.05f; h <= 24f; h += 0.05f)
    {
        float now = Tycoon3D.Core.GameClock.Daylight(h);
        biggestStep = Math.Max(biggestStep, Math.Abs(now - previous));
        if (now < 0f || now > 1f) throw new Exception("la luz sale de 0..1 a las " + h);
        previous = now;
    }
    Check(biggestStep < 0.04f, "luz: la curva no da saltos (mayor paso por 3 minutos: " + biggestStep.ToString("F3") + ")");
    Check(Tycoon3D.Core.GameClock.Daylight(26f) == Tycoon3D.Core.GameClock.Daylight(2f) && Tycoon3D.Core.GameClock.Daylight(-12f) == Tycoon3D.Core.GameClock.Daylight(12f), "luz: horas fuera de 0..24 dan la vuelta");
    Check(Tycoon3D.Core.GameClock.SunHeight(12f) > 0.99f && Tycoon3D.Core.GameClock.SunHeight(0f) < -0.99f && Math.Abs(Tycoon3D.Core.GameClock.SunHeight(6f)) < 0.001f, "luz: el sol cruza el horizonte a las 06:00");
}

// ---- clientes de la fila: pedido, paciencia, pago y regreso (el dinero lo mueve solo BurgerLoop) -------------------------------
{
    var f = new Tycoon3D.Restaurant.CustomerOrderFlow(1f);
    int empezo = 0, pago = 0, seCanso = 0;
    f.OrderStarted += () => empezo++;
    f.Paid += () => pago++;
    f.GaveUpWaiting += () => seCanso++;
    Check(f.State == Tycoon3D.Restaurant.OrderState.Idle && !f.HasOrder && !f.TryPay(), "pedidos: un cliente sin pedido no puede pagar");
    int pasos = 0;
    while (f.State == Tycoon3D.Restaurant.OrderState.Idle && pasos++ < 2000) f.Update(.1f, false);
    Check(f.State == Tycoon3D.Restaurant.OrderState.Ordering && empezo == 1 && pasos * .1f >= Tycoon3D.Restaurant.CustomerOrderFlow.FirstOrderMinSeconds - .15f && pasos * .1f <= Tycoon3D.Restaurant.CustomerOrderFlow.FirstOrderMaxSeconds + .15f,
        "pedidos: pide por primera vez entre 3 y 9 segundos (" + pasos * .1f + " s)");
    f.Update(22.5f, false);
    Check(f.HasOrder && Math.Abs(f.PatienceLeft - .5f) < .01f && Math.Abs(f.WaitingSeconds - 22.5f) < .01f, "pedidos: a mitad de la paciencia le queda la mitad");
    Check(f.TryPay() && f.State == Tycoon3D.Restaurant.OrderState.Leaving && f.Served == 1 && pago == 1 && f.LeftHappy, "pedidos: al servirle paga y se va contento");
    Check(!f.TryPay() && f.Served == 1 && pago == 1, "pedidos: no se puede cobrar dos veces el mismo pedido");
    f.Update(1f, false);
    Check(f.State == Tycoon3D.Restaurant.OrderState.Leaving, "pedidos: sigue yéndose hasta llegar a la calle");
    f.Update(.1f, true);
    Check(f.State == Tycoon3D.Restaurant.OrderState.Away, "pedidos: al llegar a la calle desaparece");
    f.Update(Tycoon3D.Restaurant.CustomerOrderFlow.AwayAfterPaidSeconds - .5f, false);
    Check(f.State == Tycoon3D.Restaurant.OrderState.Away, "pedidos: sigue ausente antes de tiempo");
    f.Update(1f, false);
    Check(f.State == Tycoon3D.Restaurant.OrderState.Returning, "pedidos: tras 8 segundos vuelve");
    f.Update(5f, false);
    Check(f.State == Tycoon3D.Restaurant.OrderState.Returning, "pedidos: vuelve caminando");
    f.Update(.1f, true);
    Check(f.State == Tycoon3D.Restaurant.OrderState.Idle && !f.HasOrder, "pedidos: al llegar a su sitio espera un pedido nuevo");
    pasos = 0;
    while (f.State == Tycoon3D.Restaurant.OrderState.Idle && pasos++ < 2000) f.Update(.1f, false);
    Check(f.State == Tycoon3D.Restaurant.OrderState.Ordering && empezo == 2 && pasos * .1f >= Tycoon3D.Restaurant.CustomerOrderFlow.NextOrderMinSeconds - .15f, "pedidos: vuelve a pedir tras 5 a 12 segundos");

    // se cansa de esperar: se va sin pagar y no pasa nada más
    f.Update(Tycoon3D.Restaurant.CustomerOrderFlow.PatienceSeconds - 1f, false);
    Check(f.HasOrder && seCanso == 0, "pedidos: con un segundo de paciencia todavía espera");
    f.Update(1.5f, false);
    Check(f.State == Tycoon3D.Restaurant.OrderState.Leaving && f.GaveUp == 1 && seCanso == 1 && !f.LeftHappy && f.Served == 1 && pago == 1, "pedidos: sin paciencia se va sin pagar");
    f.Update(.1f, true);
    f.Update(Tycoon3D.Restaurant.CustomerOrderFlow.AwayAfterGivingUpSeconds + .1f, false);
    Check(f.State == Tycoon3D.Restaurant.OrderState.Returning, "pedidos: el que se cansó tarda más en volver");

    // atascado al caminar: el flujo sigue solo
    var g = new Tycoon3D.Restaurant.CustomerOrderFlow(2f);
    g.ForceOrder();
    Check(g.HasOrder && g.TryPay(), "pedidos: ForceOrder abre un pedido ya");
    g.Update(Tycoon3D.Restaurant.CustomerOrderFlow.WalkTimeoutSeconds + .1f, false);
    Check(g.State == Tycoon3D.Restaurant.OrderState.Away, "pedidos: si no llega a la calle, pasados 40 s sigue su ciclo");
    g.Update(-5f, false); g.Update(float.NaN, false); g.Update(float.PositiveInfinity, false);
    Check(g.State == Tycoon3D.Restaurant.OrderState.Away, "pedidos: un tiempo inválido no mueve el flujo");

    // dos clientes con otra semilla no piden al mismo tiempo
    var uno = new Tycoon3D.Restaurant.CustomerOrderFlow(1f);
    var dos = new Tycoon3D.Restaurant.CustomerOrderFlow(2f);
    float t1 = 0, t2 = 0;
    while (!uno.HasOrder) { uno.Update(.05f, false); t1 += .05f; }
    while (!dos.HasOrder) { dos.Update(.05f, false); t2 += .05f; }
    Check(Math.Abs(t1 - t2) > .2f, "pedidos: dos clientes piden en momentos distintos (" + t1 + " s y " + t2 + " s)");

    // el dinero no depende de los clientes: la venta de paso y la venta con cliente pagan lo mismo y el libro cuadra
    var cartera = new Wallet();
    typeof(Wallet).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(cartera, null);
    var hamburguesas = new BurgerLoop();
    typeof(BurgerLoop).GetField("wallet", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(hamburguesas, cartera);
    var libro = new MovementLedger();
    libro.Restore(new SaveData(), cartera.Pesos, 0);
    var cliente = new Tycoon3D.Restaurant.CustomerOrderFlow(3f);
    int ventas = 0;
    hamburguesas.MovementRecorded += (kind, amount, from, to) =>
    {
        libro.Record(kind, amount, from, to);
        if (kind == MovementKind.Sale) { ventas++; cliente.TryPay(); }       // la conexión: una venta le da el pedido al cliente que espera
    };
    for (int i = 0; i < 2; i++) { hamburguesas.BuyIngredients(); hamburguesas.Cook(); hamburguesas.Sell(); }
    Check(cartera.Pesos == 100 + 2 * 35 && ventas == 2 && cliente.Served == 0, "pedidos: sin cliente esperando la venta paga igual ($70 de ganancia)");
    cliente.ForceOrder();
    hamburguesas.BuyIngredients(); hamburguesas.Cook(); hamburguesas.Sell();
    Check(cartera.Pesos == 100 + 3 * 35 && cliente.Served == 1 && !cliente.HasOrder, "pedidos: con cliente esperando la venta paga lo mismo y el cliente se va");
    hamburguesas.Sell();
    Check(cartera.Pesos == 100 + 3 * 35 && cliente.Served == 1 && ventas == 3, "pedidos: no hay cobro doble si se repite la venta");
    Check(libro.Matches(cartera.Pesos, 0), "pedidos: el libro común cuadra");
}

// ---- variantes de ropa de los clientes con la paleta única ---------------------------------------------------------------------
{
    var P = Tycoon3D.Restaurant.PaletteVariants.Palette;
    Check(P.Length == 26 && P.Select(x => x.Name).Distinct().Count() == 26, "variantes: la paleta tiene 26 colores con nombres distintos");
    Check(P.Select(x => (x.Col, x.Row)).Distinct().Count() == 26 && P.All(x => x.Col >= 0 && x.Col < 8 && x.Row >= 0 && x.Row < 4), "variantes: cada color ocupa una celda propia de la rejilla de 8 × 4");
    var original = Tycoon3D.Restaurant.PaletteVariants.BuildPixels(0);
    Check(original.Length == 128 * 64 * 4, "variantes: la textura es de 128 × 64 RGBA");
    Check(P.All(x => Tycoon3D.Restaurant.PaletteVariants.CellColor(original, x.Col, x.Row) == (x.R, x.G, x.B)), "variantes: la variante original reproduce los 26 colores de la paleta");
    Check(Tycoon3D.Restaurant.PaletteVariants.Find("M_Turquesa").R == 0x2B && Tycoon3D.Restaurant.PaletteVariants.Find("M_Chile").G == 0x57, "variantes: valores de ejemplo de la paleta (turquesa y chile)");

    var All = Tycoon3D.Restaurant.PaletteVariants.All;
    Check(All.Length >= 4 && All[0].Swaps.Length == 0, "variantes: hay al menos tres variantes además de la original");
    for (int v = 1; v < All.Length; v++)
    {
        var var_ = All[v];
        Check(var_.Swaps.Length >= 3, "variantes: " + var_.Name + " cambia al menos tres colores");
        foreach (var (from, to) in var_.Swaps)
        {
            Check(Tycoon3D.Restaurant.PaletteVariants.Has(from) && Tycoon3D.Restaurant.PaletteVariants.Has(to), "variantes: " + var_.Name + " solo usa colores de la paleta (" + from + " -> " + to + ")");
            Check(from != to, "variantes: " + var_.Name + " no cambia un color por sí mismo");
            Check(!Tycoon3D.Restaurant.PaletteVariants.Protected.Contains(from) && !Tycoon3D.Restaurant.PaletteVariants.Protected.Contains(to), "variantes: " + var_.Name + " no toca piel ni pelo (" + from + " -> " + to + ")");
        }
        var px = Tycoon3D.Restaurant.PaletteVariants.BuildPixels(v);
        Check(px.Length == original.Length, "variantes: " + var_.Name + " tiene el mismo tamaño de textura");
        foreach (var name in Tycoon3D.Restaurant.PaletteVariants.Protected)
        {
            var sw = Tycoon3D.Restaurant.PaletteVariants.Find(name);
            Check(Tycoon3D.Restaurant.PaletteVariants.CellColor(px, sw.Col, sw.Row) == (sw.R, sw.G, sw.B), "variantes: " + var_.Name + " deja intacto " + name);
        }
        int cambiadas = P.Count(x => Tycoon3D.Restaurant.PaletteVariants.CellColor(px, x.Col, x.Row) != (x.R, x.G, x.B));
        Check(cambiadas >= 3, "variantes: " + var_.Name + " cambia " + cambiadas + " celdas de ropa");
        // cada celda cambiada muestra exactamente el color destino
        foreach (var (from, to) in var_.Swaps)
        {
            var f = Tycoon3D.Restaurant.PaletteVariants.Find(from); var t = Tycoon3D.Restaurant.PaletteVariants.Find(to);
            Check(Tycoon3D.Restaurant.PaletteVariants.CellColor(px, f.Col, f.Row) == (t.R, t.G, t.B), "variantes: " + var_.Name + ": " + from + " se ve como " + to);
        }
    }
    for (int a = 0; a < All.Length; a++)
        for (int b = a + 1; b < All.Length; b++)
            Check(!Tycoon3D.Restaurant.PaletteVariants.BuildPixels(a).SequenceEqual(Tycoon3D.Restaurant.PaletteVariants.BuildPixels(b)), "variantes: " + All[a].Name + " y " + All[b].Name + " se ven distintas");
    Check(Throws(() => Tycoon3D.Restaurant.PaletteVariants.BuildPixels(-1)) && Throws(() => Tycoon3D.Restaurant.PaletteVariants.BuildPixels(All.Length)), "variantes: un índice fuera de rango se rechaza");
}

// ---- estadísticas de la medición de rendimiento ------------------------------------------------------------------------------
{
    var constantes = Enumerable.Repeat(1f / 60f, 600).ToList();
    var r = Tycoon3D.Dev.PerfStats.Compute(constantes);
    Check(r.Frames == 600 && Math.Abs(r.Seconds - 10.0) < .001, "rendimiento: 600 fotogramas a 60 fps duran 10 s");
    Check(Math.Abs(r.AverageFps - 60.0) < .01 && Math.Abs(r.OnePercentLowFps - 60.0) < .01 && Math.Abs(r.MinFps - 60.0) < .01, "rendimiento: fotogramas parejos dan 60 fps en todas las cifras");
    Check(Math.Abs(r.MedianFrameMs - 16.67) < .01 && Math.Abs(r.MaxFrameMs - 16.67) < .01, "rendimiento: 16.67 ms por fotograma");

    // 990 fotogramas a 100 fps y 10 tirones de 100 ms: la media se ve bien, el peor 1 % delata los tirones
    var tirones = Enumerable.Repeat(.01f, 990).Concat(Enumerable.Repeat(.1f, 10)).ToList();
    var t = Tycoon3D.Dev.PerfStats.Compute(tirones);
    Check(t.Frames == 1000 && Math.Abs(t.AverageFps - 1000.0 / 10.9) < .01, "rendimiento: la media de fotogramas por segundo (" + t.AverageFps.ToString("F1") + ")");
    Check(Math.Abs(t.OnePercentLowFps - 10.0) < .01, "rendimiento: el peor 1 % son los diez tirones de 100 ms = 10 fps (" + t.OnePercentLowFps.ToString("F1") + ")");
    Check(Math.Abs(t.MinFps - 10.0) < .01 && Math.Abs(t.MaxFrameMs - 100.0) < .01 && Math.Abs(t.MedianFrameMs - 10.0) < .01, "rendimiento: mínimo, máximo y mediana");

    var invalidos = new List<float> { 0f, -1f, float.NaN, float.PositiveInfinity, .02f, .02f };
    var v = Tycoon3D.Dev.PerfStats.Compute(invalidos);
    Check(v.Frames == 2 && Math.Abs(v.AverageFps - 50.0) < .01, "rendimiento: se ignoran los tiempos inválidos");
    Check(Tycoon3D.Dev.PerfStats.Compute(new List<float>()).Frames == 0 && Tycoon3D.Dev.PerfStats.Compute(null).Frames == 0 && Tycoon3D.Dev.PerfStats.Compute(new List<float> { 0f, -2f }).AverageFps == 0.0,
        "rendimiento: sin datos no hay cifras ni errores");
    Check(r.ToString().Contains("fps_medio=60.0") && r.ToString().Contains("fps_peor1pct=60.0") && r.ToString().Contains("frames=600"), "rendimiento: el texto del resultado");
}

Console.WriteLine("EconomySmoke: {0} comprobaciones aprobadas.", Counter.Checks);

static void Check(bool condition, string description)
{
    Counter.Checks++;
    if (!condition) throw new Exception("Falló: " + description);
}

static bool Throws(Action action)
{
    try { action(); return false; } catch (Exception) { return true; }
}

static class Counter { public static int Checks; }
