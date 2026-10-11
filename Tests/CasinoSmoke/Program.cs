using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Tycoon3D.Casino;
using Tycoon3D.Economy;
using Tycoon3D.Restaurant;

int checks = 0;

// ---- tabla de premios ------------------------------------------------------------------------------------------
Check(SlotMachineLogic.Weights.Sum() == 100, "La tabla suma 100");
Check(SlotMachineLogic.ExpectedReturnPer100() == 247, "Retorno esperado 247 por cada 100 (x2.47)");
var porNivel = new Dictionary<SlotTier, int>();
for (int roll = 0; roll < 100; roll++)
{
    var r = SlotMachineLogic.PlayWithRoll(100, false, roll, new SystemRandomSource(roll));
    porNivel[r.Tier] = porNivel.GetValueOrDefault(r.Tier) + 1;
    Check(r.Reels.Length == 3, "Tres rodillos");
    int distintos = r.Reels.Distinct().Count();
    switch (r.Tier)
    {
        case SlotTier.Perdida: Check(distintos == 3, "Pérdida: tres símbolos distintos"); break;
        case SlotTier.Par: Check(distintos == 2, "Par: exactamente dos iguales"); break;
        default: Check(distintos == 1, "Trío: tres iguales"); break;
    }
    Check(r.Payout == 100 * SlotMachineLogic.MultiplierOf(r.Tier), "Premio = apuesta x multiplicador");
}
for (int i = 0; i < SlotMachineLogic.Tiers.Length; i++)
    Check(porNivel[SlotMachineLogic.Tiers[i]] == SlotMachineLogic.Weights[i], "Frecuencia exacta de " + SlotMachineLogic.Tiers[i]);
Check(SlotMachineLogic.PlayWithRoll(100, false, 99, new SystemRandomSource(1)).Reels.All(s => s == SlotSymbol.Fox),
    "La tirada 99 es el jackpot FOX");
Check(Throws(() => SlotMachineLogic.TierForRoll(100)), "Rechaza tiradas fuera de tabla");
Check(Throws(() => SlotMachineLogic.Play(0, false, new SystemRandomSource(1))), "Rechaza apuesta cero");

// ---- simulación grande: el retorno medido coincide con el diseño --------------------------------------------------
{
    var rng = new SystemRandomSource(12345);
    long apostado = 0, pagado = 0; int ganadas = 0; const int n = 400000;
    for (int i = 0; i < n; i++)
    {
        var r = SlotMachineLogic.Play(100, false, rng);
        apostado += 100; pagado += r.Payout; if (r.Payout > 0) ganadas++;
    }
    double retorno = pagado / (double)apostado, tasa = ganadas / (double)n;
    Console.WriteLine("  simulación {0:N0} giros: retorno x{1:F3} (diseño x2.47), aciertos {2:P1} (diseño 62%)", n, retorno, tasa);
    Check(Math.Abs(retorno - 2.47) < 0.06, "Retorno simulado cercano a x2.47");
    Check(Math.Abs(tasa - 0.62) < 0.01, "Tasa de aciertos cercana a 62%");
}

// ---- sesión sobre un monedero simulado ----------------------------------------------------------------------------
{
    int saldo = 100;
    var rng = new Scripted(38 /*Par? no: roll 38 = Par*/, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    var s = Nueva(() => saldo, v => { if (v < 0 || saldo < v) return false; saldo -= v; return true; }, v => saldo += v, rng);
    Check(s.CurrentBet == 25 && !s.NextSpinIsFree, "Apuesta inicial $25 y casino disponible");
    var estado = s.Begin(out var ronda);
    Check(estado == SpinStatus.Started && saldo == 75 && s.Busy, "Begin cobra la apuesta");
    Check(s.Begin(out _) == SpinStatus.Busy && saldo == 75, "No se puede girar dos veces a la vez");
    var fin = s.Complete();
    Check(fin.Payout == ronda.Payout && saldo == 75 + ronda.Payout && !s.Busy, "Complete acredita el premio");
    Check(s.TotalSpins == 1 && s.BestPayout == ronda.Payout, "Estadísticas de la sesión");
    s.CycleBet(); Check(s.CurrentBet == 100, "Q: de $25 a $100");
    s.CycleBet(); s.CycleBet(); s.CycleBet(); Check(s.CurrentBet == 25, "El ciclo de apuestas da la vuelta");
}
{
    // saldo insuficiente para la apuesta elegida: se ajusta a la mayor apuesta que alcanza
    int saldo = 120;
    var s = Nueva(() => saldo, v => { if (saldo < v) return false; saldo -= v; return true; }, v => saldo += v, new Scripted(0));
    s.CycleBet(); s.CycleBet(); s.CycleBet(); // $2000
    var estado = s.Begin(out var r);
    Check(estado == SpinStatus.AdjustedBet && s.CurrentBet == 100 && saldo == 20, "Ajusta $2000 a $100 con saldo $120");
    s.Complete();
    Check(saldo == 20, "Con roll de pérdida no hay premio");
}
{
    // giro de cortesía: nunca se queda sin poder jugar
    int saldo = 0;
    var s = Nueva(() => saldo, v => false, v => saldo += v, new Scripted(0, 0, 0, 0, 68)); // pérdida (tirada 0 + 3 símbolos al azar), luego tirada 68 = trío chile
    Check(s.NextSpinIsFree, "Con saldo $0 el siguiente giro es de cortesía");
    var estado = s.Begin(out var r);
    Check(estado == SpinStatus.FreeSpin && r.FreeSpin && saldo == 0 && r.Stake == 25, "Giro de cortesía: no cobra");
    var fin = s.Complete();
    Check(saldo == 0 && fin.Net == 0, "Cortesía perdida: el saldo no baja de $0");
    s.Begin(out var r2);
    var fin2 = s.Complete();
    Check(r2.Tier == SlotTier.TrioChile && saldo == 75 && fin2.Net == 75, "Cortesía ganada: trío chile paga $75");
    Check(!s.NextSpinIsFree, "Con $75 ya se puede apostar de nuevo");
}
{
    // el saldo nunca es negativo aunque se gire muchas veces con azar real
    int saldo = 30;
    var s = Nueva(() => saldo, v => { if (saldo < v) return false; saldo -= v; return true; }, v => saldo += v, new SystemRandomSource(7));
    int minimo = int.MaxValue;
    for (int i = 0; i < 20000; i++)
    {
        s.Begin(out _); s.Complete(); minimo = Math.Min(minimo, saldo);
        if (saldo > 1_000_000) saldo = 30; // evita desbordes en la prueba
    }
    Check(minimo >= 0, "El saldo nunca baja de cero");
}

// ---- integración con el Wallet real y sin romper compra-cocción-venta ------------------------------------------------
{
    var wallet = new Wallet();
    typeof(Wallet).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(wallet, null);
    var loop = new BurgerLoop();
    typeof(BurgerLoop).GetField("wallet", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(loop, wallet);
    var s = Nueva(() => wallet.Pesos, wallet.TrySpend, wallet.Earn, new Scripted(99, 0));
    loop.BuyIngredients(); loop.Cook(); loop.Sell();
    Check(wallet.Pesos == 135 && loop.Sales == 1, "El ciclo de hamburguesas sigue dando $135");
    loop.BuyIngredients();                                   // $120, ingredientes comprados a medias
    Check(wallet.Pesos == 120 && loop.Step == BurgerStep.NeedCooking, "Compra de ingredientes intacta");
    s.Begin(out _);                                          // casino a mitad del ciclo: $95
    Check(wallet.Pesos == 95 && loop.Step == BurgerStep.NeedCooking, "Apostar no altera el paso de la hamburguesa");
    var r = s.Complete();                                    // jackpot FOX: x40 = $1000
    Check(r.Tier == SlotTier.JackpotFox && wallet.Pesos == 95 + 1000, "Jackpot FOX acredita $1000 en el Wallet real");
    loop.Cook(); loop.Sell();
    Check(wallet.Pesos == 1145 && loop.Sales == 2, "La hamburguesa en curso se vende normal tras el casino");
}

// ---- el casino acelera el progreso frente a las hamburguesas ---------------------------------------------------------
{
    const int meta = 10_000;
    // hamburguesas: +$35 netos cada 3 acciones (comprar, cocinar, vender)
    int accionesHamburguesa = (int)Math.Ceiling((meta - 100) / 35.0) * 3;
    var resultados = new List<int>();
    for (int semilla = 0; semilla < 200; semilla++)
    {
        int saldo = 100, acciones = 0;
        var s = Nueva(() => saldo, v => { if (saldo < v) return false; saldo -= v; return true; }, v => saldo += v, new SystemRandomSource(semilla));
        while (saldo < meta && acciones < 100000)
        {
            // política simple: sube la apuesta mientras no supere un tercio del saldo
            int idx = 0; for (int i = 0; i < CasinoSession.BetTiers.Length; i++) if (CasinoSession.BetTiers[i] * 3 <= Math.Max(saldo, 75)) idx = i;
            while (s.BetIndex != idx) s.CycleBet();
            s.Begin(out _); s.Complete(); acciones++;
        }
        resultados.Add(acciones);
    }
    resultados.Sort();
    int mediana = resultados[resultados.Count / 2];
    Console.WriteLine("  llegar a ${0:N0}: hamburguesas {1} acciones; casino mediana {2} acciones (x{3:F0} más rápido)",
        meta, accionesHamburguesa, mediana, accionesHamburguesa / (double)mediana);
    Check(mediana * 10 < accionesHamburguesa, "El casino es al menos 10 veces más rápido que vender hamburguesas");
}

// ---- tablas de las tres máquinas ---------------------------------------------------------------------------------
{
    Check(SlotPaytables.All.Length == 3, "Hay tres tablas de premios");
    Check(SlotPaytables.Clasica.ExpectedReturnPer100() == 247 && SlotPaytables.Clasica.HitPercent == 62, "Clásica: x2.47 y 62%");
    Check(SlotPaytables.MangoRapido.ExpectedReturnPer100() == 219 && SlotPaytables.MangoRapido.HitPercent == 70, "Mango Rápido: x2.19 y 70%");
    Check(SlotPaytables.GranJackpot.ExpectedReturnPer100() == 229 && SlotPaytables.GranJackpot.HitPercent == 40, "Gran Jackpot: x2.29 y 40%");
    Check(SlotPaytables.GranJackpot.TopMultiplier == 60 && SlotPaytables.MangoRapido.TopMultiplier == 12, "Premios máximos de cada máquina");
    foreach (var t in SlotPaytables.All)
    {
        Check(t.ExpectedReturnPer100() > 200, t.Name + " acelera el progreso (retorno > x2)");
        var conteo = new Dictionary<SlotTier, int>();
        for (int roll = 0; roll < 100; roll++)
        {
            var r = SlotMachineLogic.PlayWithRoll(100, false, roll, new SystemRandomSource(roll), t);
            conteo[r.Tier] = conteo.GetValueOrDefault(r.Tier) + 1;
            Check(r.Payout == 100 * t.MultiplierOf(r.Tier), t.Name + ": premio = apuesta x multiplicador");
        }
        for (int i = 0; i < SlotMachineLogic.Tiers.Length; i++)
            Check(conteo.GetValueOrDefault(SlotMachineLogic.Tiers[i]) == t.Weights[i], t.Name + ": frecuencia exacta de " + SlotMachineLogic.Tiers[i]);
        var rng = new SystemRandomSource(2026); long ap = 0, pg = 0;
        for (int i = 0; i < 300000; i++) { var r = SlotMachineLogic.Play(100, false, rng, t); ap += 100; pg += r.Payout; }
        double ret = pg / (double)ap;
        Console.WriteLine("  {0}: retorno simulado x{1:F3} (diseño x{2:F2})", t.Name, ret, t.ExpectedReturnPer100() / 100.0);
        Check(Math.Abs(ret - t.ExpectedReturnPer100() / 100.0) < 0.07, t.Name + ": retorno simulado cercano al de diseño");
    }
    Check(Throws(() => new SlotPaytable("x", "x", new[] { 50, 50, 1, 0, 0, 0 }, new[] { 0, 1, 1, 1, 1, 1 })), "Rechaza tablas que no suman 100");
    // una sesión con la máquina Gran Jackpot: la tirada 99 cae en el jackpot x60
    int saldo = 100;
    var s = Nueva(() => saldo, v => { if (saldo < v) return false; saldo -= v; return true; }, v => saldo += v, new Scripted(99));
    s.Begin(out var rj, SlotPaytables.GranJackpot);
    var fin = s.Complete();
    Check(rj.Tier == SlotTier.JackpotFox && fin.Payout == 25 * 60 && saldo == 75 + 1500, "Gran Jackpot paga x60 en el jackpot");
    // SetBetIndex: elegir la apuesta por número
    Check(s.SetBetIndex(2) && s.CurrentBet == 500 && !s.SetBetIndex(4) && !s.SetBetIndex(-1) && s.CurrentBet == 500, "SetBetIndex elige y valida");
    s.Begin(out _);
    Check(!s.SetBetIndex(0), "No se cambia la apuesta con un giro en curso");
    s.Complete();
}

// RNG degenerado: siempre el mismo valor no debe colgar el sorteo y el par debe seguir siendo dos iguales + uno distinto
{
    foreach (var table in SlotPaytables.All)
    {
        foreach (int constant in new[] { 0, 1, 37, 99 })
        {
            var rng = new Constant(constant);
            var task = System.Threading.Tasks.Task.Run(() =>
            {
                for (int roll = 0; roll < 100; roll++) SlotMachineLogic.PlayWithRoll(100, false, roll, rng, table);
            });
            Check(task.Wait(2000), "RNG constante " + constant + " (" + table.Name + ") no cuelga el sorteo");
        }
    }
    var par = SlotMachineLogic.PlayWithRoll(100, false, 38, new Constant(2), SlotPaytables.Clasica);
    Check(par.Tier == SlotTier.Par && par.Reels.Distinct().Count() == 2, "par con RNG constante: dos iguales y uno distinto");
}

// Libro del casino: ronda registrada al aceptar la apuesta y liquidada una sola vez
{
    int saldo = 1000;
    var s = Nueva(() => saldo, v => { if (saldo < v) return false; saldo -= v; return true; }, v => saldo += v, new Scripted(99, 0));
    s.Begin(out var r1);
    Check(s.RoundsStarted == 1 && s.PendingRoundId == 1 && s.TotalWagered == 25 && saldo == 975, "libro: apuesta registrada y cobrada al aceptar");
    s.Complete();
    Check(saldo == 975 + r1.Payout && s.PendingRoundId == 0 && s.TotalWon == r1.Payout, "libro: premio liquidado una vez");
    Check(Throws(() => s.Complete()) && saldo == 975 + r1.Payout, "libro: una segunda liquidación falla y no paga de nuevo");
    Check(s.HouseNet == 25 - r1.Payout, "libro: resultado de la casa = apostado - premios");
    int saldo2 = 0;
    var libre = Nueva(() => saldo2, v => false, v => saldo2 += v, new Scripted(0));
    libre.Begin(out var rf); libre.Complete();
    Check(libre.FreeSpins == 1 && libre.TotalWagered == 0 && libre.RoundsStarted == 1, "libro: el giro de cortesía no cobra y se cuenta aparte");
}

// Caja del casino: fondo, tope por ronda y premios por cobrar (el tope reparte, no bloquea)
{
    var c = new CasinoCashbox();
    Check(c.Funds == 20000 && c.RoundCap == 5000 && c.ReserveFloor == 1000 && c.Owed == 0, "caja: valores por defecto");
    Check(Throws(() => new CasinoCashbox(-1, 5000, 1000)) && Throws(() => new CasinoCashbox(100, 0, 0)) && Throws(() => new CasinoCashbox(100, 500, 600)), "caja: reglas inválidas se rechazan");
    var pequeno = c.Pay(1000);
    Check(pequeno.Now == 1000 && pequeno.Deferred == 0 && pequeno.Installment == 0 && c.Funds == 19000 && c.Owed == 0, "caja: un premio dentro del tope se paga entero");
    var grande = c.Pay(80000);
    Check(grande.Now == 5000 && grande.Deferred == 75000 && c.Owed == 75000 && c.Funds == 14000, "caja: el jackpot de $80000 paga el tope y deja $75000 por cobrar");
    int rondas = 0, pagado = grande.Total;
    while (c.Owed > 0 && rondas < 100)
    {
        rondas++;
        var cuota = c.Pay(0);
        pagado += cuota.Total;
        if (cuota.Total > c.RoundCap) throw new Exception("la cuota excede el tope");
        c.EnsureReserve();
    }
    Check(c.Owed == 0 && pagado == 80000 && rondas < 100, "caja: con el fondo agotado la reserva sigue pagando cuotas y se entrega todo el premio (" + rondas + " rondas)");
    Check(c.Refilled > 0 && c.Funds >= 0, "caja: lo repuesto por la casa queda anotado");

    var vacia = new CasinoCashbox(0, 5000, 1000);
    vacia.TakeStake(100);
    int repuesto = vacia.EnsureReserve();
    Check(repuesto == 900 && vacia.Funds == 1000 && vacia.Refilled == 900, "caja: la casa repone hasta la reserva y lo anota");
    var corto = vacia.Pay(4000);
    Check(corto.Now == 1000 && corto.Deferred == 3000 && vacia.Funds == 0 && vacia.Owed == 3000, "caja: si el fondo no alcanza se paga lo que hay y el resto queda por cobrar");
    vacia.EnsureReserve();
    var siguiente = vacia.Pay(0);
    Check(siguiente.Installment == 1000 && vacia.Owed == 2000, "caja: la siguiente ronda paga al menos la reserva aunque no haya premio nuevo");
}
{
    // integración con la sesión: apuesta de $2000 y jackpot (x40 = $80000), con conservación del valor total
    int saldo = 5000;
    var s = Nueva(() => saldo, v => { if (saldo < v) return false; saldo -= v; return true; }, v => saldo += v, new Scripted(99, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
    s.CycleBet(); s.CycleBet(); s.CycleBet();
    Check(s.CurrentBet == 2000, "caja: apuesta de $2000");
    long Auditar() => saldo + s.Cashbox.Funds - s.Cashbox.Refilled;
    Check(Auditar() == 5000 + 20000, "caja: valor inicial (pesos + fondo)");
    s.Begin(out var r);
    Check(r.Payout == 80000 && saldo == 3000 && s.Cashbox.Funds == 22000 && Auditar() == 25000, "caja: la apuesta entra a la caja");
    s.Complete();
    Check(saldo == 8000 && s.LastPayment.Now == 5000 && s.LastPayment.Deferred == 75000 && s.Cashbox.Owed == 75000, "caja: el jackpot paga $5000 ahora y deja $75000 por cobrar");
    Check(s.TotalWon == 5000 && s.BestPayout == 80000 && Auditar() == 25000, "caja: el libro cuenta lo pagado y el mejor premio es el de la tabla");
    for (int i = 0; i < 3; i++)
    {
        s.Begin(out _); s.Complete();
        Check(s.LastPayment.Installment == 5000 && Auditar() == 25000, "caja: una ronda sin premio cobra una cuota de $5000 (ronda " + (i + 1) + ")");
    }
    Check(s.Cashbox.Owed == 60000, "caja: quedan $60000 por cobrar tras tres cuotas");
}
{
    // premio dentro del tope y fichas: se paga entero y en fichas
    int saldo = 5000;
    var s = Nueva(() => saldo, v => { if (saldo < v) return false; saldo -= v; return true; }, v => saldo += v, new Scripted(99, 99));
    s.SetBetIndex(1);
    s.Begin(out var r); s.Complete();
    Check(r.Payout == 4000 && s.LastPayment.Now == 4000 && s.LastPayment.Deferred == 0 && saldo == 5000 - 100 + 4000, "caja: jackpot de $100 (x40 = $4000) se paga entero");
    var t = Nueva(() => saldo, v => { if (saldo < v) return false; saldo -= v; return true; }, v => saldo += v, new Scripted(99));
    Check(t.BuyChips(80), "caja: compra 80 fichas");
    t.SetBetIndex(3);
    t.Begin(out _); t.Complete();
    Check(t.Chips == 200 && t.LastPaidWithChips && t.Cashbox.Owed == 75000, "caja: el premio recortado se acredita en fichas ($5000 = 200 fichas)");
}
{
    // persistencia de la caja y perfil anterior sin caja
    int saldo = 5000;
    var s = Nueva(() => saldo, v => { if (saldo < v) return false; saldo -= v; return true; }, v => saldo += v, new Scripted(99));
    s.SetBetIndex(3);
    s.Begin(out _); s.Complete();
    var estado = s.Snapshot();
    Check(estado.CashboxInit && estado.CashboxOwed == 75000 && estado.CashboxFunds == s.Cashbox.Funds, "caja: la instantánea trae fondo y deuda");
    var otra = Nueva(() => saldo, v => { if (saldo < v) return false; saldo -= v; return true; }, v => saldo += v, new Scripted(0));
    otra.Restore(estado);
    Check(otra.Cashbox.Owed == 75000 && otra.Cashbox.Funds == s.Cashbox.Funds, "caja: se restaura con su deuda");
    var viejo = estado; viejo.CashboxInit = false; viejo.CashboxFunds = 0; viejo.CashboxOwed = 0;
    var tercera = Nueva(() => saldo, v => { if (saldo < v) return false; saldo -= v; return true; }, v => saldo += v, new Scripted(0));
    tercera.Restore(viejo);
    Check(tercera.Cashbox.Funds == 20000 && tercera.Cashbox.Owed == 0, "caja: un perfil anterior sin caja empieza con el fondo inicial");
    // ronda pendiente al reabrir: se liquida contra la caja una sola vez
    int saldo4 = 3000;
    var cuarta = Nueva(() => saldo4, v => { if (saldo4 < v) return false; saldo4 -= v; return true; }, v => saldo4 += v, new Scripted(0));
    var pendiente = new CasinoState { CashboxInit = true, CashboxFunds = 20000, PendingActive = true, PendingStake = 2000, PendingPayout = 80000, PendingRoundId = 1, Rounds = 1 };
    int recuperado = cuarta.Restore(pendiente);
    Check(recuperado == 5000 && saldo4 == 8000 && cuarta.Cashbox.Owed == 75000, "caja: una ronda pendiente se liquida con el tope y deja lo demás por cobrar");
}

// HUD en otras proporciones de pantalla: todo cabe, nada se pisa
{
    var pantallas = new (int w, int h, string nombre)[]
    {
        (1280, 720, "16:9"), (1600, 900, "16:9"), (1920, 1080, "16:9"), (2560, 1440, "16:9"), (3840, 2160, "16:9 4K"),
        (1024, 768, "4:3"), (1280, 960, "4:3"), (1600, 1200, "4:3"), (1280, 1024, "5:4"),
        (1280, 800, "16:10"), (1440, 900, "16:10"), (1920, 1200, "16:10"),
        (2560, 1080, "21:9"), (3440, 1440, "21:9"), (5120, 1440, "32:9"), (1366, 768, "16:9 portátil"), (800, 600, "4:3 mínima"), (640, 480, "VGA")
    };
    int casos = 0;
    foreach (var (w, h, nombre) in pantallas)
        for (float escala = .7f; escala <= 2.21f; escala += .1f)
        {
            var l = HudLayout.Compute(w, h, escala);
            string quien = nombre + " " + w + "x" + h + " texto " + (int)Math.Round(escala * 100) + "%";
            Check(l.Scale >= HudLayout.MinScale && !float.IsNaN(l.Scale), "HUD " + quien + ": escala válida");
            Check(l.Panel.InsideScreen(w, h) && l.Tag.InsideScreen(w, h) && l.Toast.InsideScreen(w, h), "HUD " + quien + ": el panel, la etiqueta y el aviso caben en la pantalla");
            Check(!l.Panel.Overlaps(l.Tag) && !l.Panel.Overlaps(l.Toast) && !l.Tag.Overlaps(l.Toast), "HUD " + quien + ": panel, etiqueta y aviso no se pisan");
            casos++;
        }
    var normal = HudLayout.Compute(1920, 1080, 1f);
    Check(Math.Abs(normal.Scale - 1f) < .001f && !normal.ToastBelowTag && Math.Abs(normal.Panel.W - 780f) < .01f, "HUD: a 1920x1080 y 100 % se ve como el diseño original");
    var cuadrada = HudLayout.Compute(1024, 768, 1f);
    Check(cuadrada.ToastBelowTag, "HUD: en 4:3 el aviso baja debajo de la etiqueta en vez de pisarla");
    var ancha = HudLayout.Compute(3440, 1440, 1f);
    Check(!ancha.ToastBelowTag && Math.Abs((ancha.Panel.X + ancha.Panel.W / 2) - 1720) < .01f, "HUD: en pantalla ultra ancha el panel queda centrado y el aviso no baja");
    Check(HudLayout.Fit(1280, 720, 2.2f) < HudLayout.Fit(1920, 1080, 2.2f), "HUD: la escala pedida se recorta en pantallas pequeñas");
    Console.WriteLine("  HUD: {0} combinaciones de pantalla y tamaño de texto sin choques.", casos);
}

// Horario de la cajera: el turno nunca bloquea el casino
{
    Check(CashierSchedule.OnDuty(8f) && CashierSchedule.OnDuty(12f) && CashierSchedule.OnDuty(21.99f), "turno: de 08:00 a 22:00 está en turno");
    Check(!CashierSchedule.OnDuty(7.99f) && !CashierSchedule.OnDuty(22f) && !CashierSchedule.OnDuty(0f) && !CashierSchedule.OnDuty(3f), "turno: de 22:00 a 08:00 está en descanso");
    Check(CashierSchedule.OnDuty(32f) && !CashierSchedule.OnDuty(31f) && !CashierSchedule.OnDuty(-2f) && CashierSchedule.OnDuty(-12f), "turno: las horas fuera de 0..24 dan la vuelta");
    Check(CashierSchedule.ShiftText == "Turno 08:00–22:00", "turno: rótulo del horario");
    Check(CashierSchedule.StatusText(12f) == "En turno hasta las 22:00" && CashierSchedule.StatusText(23f) == "En descanso · vuelve a las 08:00", "turno: estado visible");
    Check(Math.Abs(CashierSchedule.HoursUntilChange(12f) - 10f) < .001f && Math.Abs(CashierSchedule.HoursUntilChange(23f) - 9f) < .001f && Math.Abs(CashierSchedule.HoursUntilChange(5f) - 3f) < .001f, "turno: horas hasta el próximo cambio");
    int horasEnTurno = 0;
    for (int minuto = 0; minuto < 1440; minuto++) if (CashierSchedule.OnDuty(minuto / 60f)) horasEnTurno++;
    Check(horasEnTurno == 14 * 60, "turno: son 14 horas en turno y 10 de descanso al día");
    // la sesión no depende del horario: con la cajera en descanso las máquinas siguen aceptando giros y el canje de fichas funciona
    int saldo = 500;
    var s = Nueva(() => saldo, v => { if (saldo < v) return false; saldo -= v; return true; }, v => saldo += v, new Scripted(0));
    Check(s.BuyChips(4), "turno: la sesión compra fichas (el horario lo aplica el servicio, no la sesión)");
    Check(s.Begin(out _) == SpinStatus.Started, "turno: se puede girar con fichas");
    s.Complete();
    Check(s.CashOutChips(s.Chips) > 0, "turno: las fichas se pueden canjear en cualquier momento");
}

// Destellos: nunca más de tres por segundo, y ninguno con las luces fijas
{
    Check(FlashPolicy.MaxFlashesPerSecond == 3f && FlashPolicy.CelebrationHz <= FlashPolicy.MaxFlashesPerSecond, "destellos: la celebración usa menos de tres por segundo");
    Check(FlashPolicy.EffectiveHz(10f, false) == 3f && FlashPolicy.EffectiveHz(2.5f, false) == 2.5f && FlashPolicy.EffectiveHz(10f, true) == 0f && FlashPolicy.EffectiveHz(-4f, false) == 0f,
        "destellos: el ritmo pedido se recorta a tres y con luces fijas es cero");
    foreach (float pedido in new[] { 1f, 2.5f, 3f, 5f, 20f, 1000f })
    {
        // se muestrea cada milisegundo durante 10 s y se cuentan los destellos (apagado a encendido) en cada ventana de un segundo
        var flancos = new List<double>();
        bool anterior = FlashPolicy.Lit(0f, pedido, false);
        for (int ms = 1; ms <= 10000; ms++)
        {
            bool ahora = FlashPolicy.Lit(ms / 1000f, pedido, false);
            if (ahora && !anterior) flancos.Add(ms / 1000.0);
            anterior = ahora;
        }
        int peor = 0;
        for (int i = 0; i < flancos.Count; i++)
        {
            int enVentana = 0;
            for (int j = i; j < flancos.Count && flancos[j] < flancos[i] + 1.0; j++) enVentana++;
            peor = Math.Max(peor, enVentana);
        }
        Check(peor <= 3, "destellos: pidiendo " + pedido + " Hz hay como mucho " + peor + " destellos por segundo");
        bool fija = true;
        for (int ms = 0; ms <= 10000; ms += 7) fija &= FlashPolicy.Lit(ms / 1000f, pedido, true);
        Check(fija, "destellos: con luces fijas siempre encendida (" + pedido + " Hz)");
    }
    Check(FlashPolicy.Pulse(.123f, 5f, true) == .6f && FlashPolicy.Pulse(0f, 2.5f, false) == .5f, "destellos: el pulso suave queda quieto con luces fijas");
    float minimo = 1f, maximo = 0f;
    for (int ms = 0; ms <= 4000; ms++) { float v = FlashPolicy.Pulse(ms / 1000f, 2.5f, false); minimo = Math.Min(minimo, v); maximo = Math.Max(maximo, v); }
    Check(minimo < .02f && maximo > .98f, "destellos: el pulso recorre toda su intensidad");
}

Console.WriteLine("CasinoSmoke: {0} comprobaciones aprobadas.", checks);

void Check(bool condition, string description)
{
    checks++;
    if (!condition) throw new Exception("Falló: " + description);
}

static bool Throws(Action action)
{
    try { action(); return false; } catch (Exception) { return true; }
}

static CasinoSession Nueva(Func<int> saldo, Func<int, bool> gasta, Action<int> gana, IRandomSource rng)
    => new CasinoSession(rng, saldo, gasta, gana);

sealed class Scripted : IRandomSource
{
    private readonly Queue<int> values;
    private readonly SystemRandomSource fallback = new SystemRandomSource(99);
    public Scripted(params int[] rolls) => values = new Queue<int>(rolls);
    public int Next(int maxExclusive) => values.Count > 0 ? Math.Min(values.Dequeue(), maxExclusive - 1) : fallback.Next(maxExclusive);
}

sealed class Constant : IRandomSource
{
    private readonly int value;
    public Constant(int value) => this.value = value;
    public int Next(int maxExclusive) => Math.Min(value, maxExclusive - 1);
}
