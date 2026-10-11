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

// ---- ruleta europea de un cero: probabilidades y pagos comprobables con RNG inyectable ----------------------------
{
    Check(Enumerable.Range(1, 36).Count(RouletteLogic.IsRed) == 18, "Ruleta: 18 números rojos");
    Check(Enumerable.Range(1, 36).Count(RouletteLogic.IsBlack) == 18, "Ruleta: 18 números negros");
    Check(!RouletteLogic.IsRed(0) && !RouletteLogic.IsBlack(0), "Ruleta: cero verde, no rojo ni negro");
    Check(!RouletteLogic.Wins(new RouletteBet(RouletteBetKind.Red, 25), 0), "Ruleta: cero pierde apuesta roja");
    Check(!RouletteLogic.Wins(new RouletteBet(RouletteBetKind.Odd, 25), 0), "Ruleta: cero pierde apuesta impar");
    Check(!RouletteLogic.Wins(new RouletteBet(RouletteBetKind.Even, 25), 0), "Ruleta: cero pierde apuesta par");
    Check(RouletteLogic.Wins(new RouletteBet(RouletteBetKind.Low, 25), 18)
        && RouletteLogic.Wins(new RouletteBet(RouletteBetKind.High, 25), 19), "Ruleta: límites 1-18 y 19-36");
    var straight = RouletteLogic.Play(new RouletteBet(RouletteBetKind.StraightNumber, 25, 17), new Scripted(17));
    Check(straight.Won && straight.GrossPayout == 900 && straight.Net == 875, "Ruleta: pleno paga 35 a 1 más devolución");
    var color = RouletteLogic.Play(new RouletteBet(RouletteBetKind.Red, 100), new Scripted(1));
    Check(color.Won && color.GrossPayout == 200 && color.Net == 100, "Ruleta: rojo paga 1 a 1 más devolución");
    var miss = RouletteLogic.Play(new RouletteBet(RouletteBetKind.Black, 100), new Scripted(1));
    Check(!miss.Won && miss.GrossPayout == 0 && miss.Net == -100, "Ruleta: color perdido cobra una sola apuesta");
    Check(Throws(() => new RouletteBet(RouletteBetKind.StraightNumber, 25, 37)), "Ruleta: rechaza número fuera de mesa");
    Check(Throws(() => new RouletteBet(RouletteBetKind.Red, 0)), "Ruleta: rechaza apuesta sin valor");
    Check(Throws(() => new RouletteRound(new RouletteBet(RouletteBetKind.Red, 25), -1)), "Ruleta: rechaza resultado inválido");
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
