using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tycoon3D.Casino;
using Tycoon3D.Save;

int checks = 0;
void Check(bool ok, string what)
{
    checks++;
    if (!ok) { Console.WriteLine("FALLO: " + what); Environment.Exit(1); }
}
string NewDir() => Path.Combine(Path.GetTempPath(), "tycoon-save-" + Guid.NewGuid().ToString("N"));
SaveData Sample() => new SaveData
{
    Pesos = 1075, BurgerSales = 3, BurgerStep = 1, BetIndex = 2, Chips = 17, ChipsBought = 40, ChipsCashed = 23, CashierVisits = 5,
    CasinoRounds = 12, CasinoSpins = 12, CasinoWagered = 900, CasinoWon = 1300, CasinoFreeSpins = 2, CasinoBest = 1000
};
bool SameData(SaveData a, SaveData b) => SaveCodec.Serialize(a.Clone()) == SaveCodec.Serialize(b.Clone());

// ---------------------------------------------------------------- códec
{
    var s = Sample();
    string text = SaveCodec.Serialize(s);
    Check(SaveCodec.TryParse(text, out var back) == ParseStatus.Ok && SameData(s, back), "códec: ida y vuelta de todos los campos");
    Check(back.Pesos == 1075 && back.Chips == 17 && back.CasinoWon == 1300, "códec: valores concretos");
    Check(SaveCodec.TryParse(text.Replace("pesos=1075", "pesos=9999"), out _) == ParseStatus.Corrupt, "códec: el checksum detecta un valor alterado");
    Check(SaveCodec.TryParse(text.Substring(0, text.Length / 2), out _) == ParseStatus.Corrupt, "códec: un archivo truncado está dañado");
    Check(SaveCodec.TryParse("", out _) == ParseStatus.Corrupt && SaveCodec.TryParse(null, out _) == ParseStatus.Corrupt, "códec: vacío o nulo está dañado");
    Check(SaveCodec.TryParse("basura\nchecksum=00000000\n", out _) == ParseStatus.Corrupt, "códec: basura con checksum falso");
    Check(SaveCodec.TryParse(text.Replace("\n", "\r\n"), out _) == ParseStatus.Ok, "códec: acepta saltos de línea de Windows");
}

string Forge(Dictionary<string, string> extra, int version = SaveCodec.CurrentVersion, bool skipPesos = false)
{
    var lines = new List<string> { SaveCodec.Header, "version=" + version };
    if (!skipPesos) lines.Add("pesos=50");
    foreach (var kv in extra) lines.Add(kv.Key + "=" + kv.Value);
    string body = string.Join("\n", lines) + "\n";
    uint h = 2166136261; foreach (char c in body) { h ^= c; h *= 16777619; }
    return body + "checksum=" + h.ToString("x8") + "\n";
}
{
    Check(SaveCodec.TryParse(Forge(new() { ["chips"] = "-3" }), out _) == ParseStatus.Corrupt, "códec: fichas negativas = dañado");
    Check(SaveCodec.TryParse(Forge(new() { ["betIndex"] = "9" }), out _) == ParseStatus.Corrupt, "códec: apuesta fuera de rango = dañado");
    Check(SaveCodec.TryParse(Forge(new() { ["pesos"] = "x" }), out _) == ParseStatus.Corrupt, "códec: número ilegible = dañado");
    Check(SaveCodec.TryParse(Forge(new() { ["pendingActive"] = "1", ["pendingStake"] = "0" }), out _) == ParseStatus.Corrupt, "códec: ronda pendiente sin apuesta = dañado");
    Check(SaveCodec.TryParse(Forge(new() { ["claveNueva"] = "7" }), out var u) == ParseStatus.Ok && u.Pesos == 50, "códec: clave desconocida se ignora");
    Check(SaveCodec.TryParse(Forge(new(), skipPesos: true), out var d0) == ParseStatus.Ok && !d0.HasProfile, "códec: clave ausente toma el valor por defecto (sin perfil)");
    Check(SaveCodec.TryParse(Forge(new(), version: SaveCodec.CurrentVersion + 1), out _) == ParseStatus.FutureVersion, "códec: versión futura no se interpreta");
    Check(SaveCodec.TryParse(Forge(new(), version: 0), out _) == ParseStatus.Corrupt, "códec: versión antigua sin migración = dañado, no se adivina");
    var migrations = new Dictionary<int, SaveMigration> { [0] = v => { v["chips"] = 4; v["pesos"] = v["pesos"] * 2; } };
    Check(SaveCodec.TryParse(Forge(new(), version: 0), out var mig, migrations) == ParseStatus.Ok && mig.Chips == 4 && mig.Pesos == 100, "migración: v0 se actualiza con la migración registrada");
}

// ---------------------------------------------------------------- repositorio y recuperación
{
    var repo = new SaveRepository(NewDir());
    Check(repo.Load(out var fresh) == LoadOutcome.Fresh && !fresh.HasProfile, "repo: directorio nuevo = perfil nuevo");
    var s = Sample();
    Check(repo.Save(s) && File.Exists(repo.MainPath) && !File.Exists(repo.BackupPath), "repo: primer guardado crea el principal sin copia");
    Check(repo.Load(out var l1) == LoadOutcome.Loaded && l1.Pesos == 1075 && l1.SaveCount == 1, "repo: carga lo guardado");
    s.Pesos = 2000;
    Check(repo.Save(s) && File.Exists(repo.BackupPath), "repo: el segundo guardado crea la copia de seguridad");
    Check(new SaveRepository(repo.Directory).Load(out var l2) == LoadOutcome.Loaded && l2.Pesos == 2000, "repo: otra instancia (otro arranque) ve $2000");

    // principal dañado -> recupera la copia ($1075) y deja el principal válido
    File.WriteAllText(repo.MainPath, "TYCOON3D-SAVE\nversion=1\npesos=1\nchecksum=deadbeef\n");
    var r2 = new SaveRepository(repo.Directory);
    Check(r2.Load(out var rec) == LoadOutcome.RecoveredFromBackup && rec.Pesos == 1075, "repo: principal dañado se recupera desde la copia");
    Check(Directory.GetFiles(repo.Directory, "profile.corrupt-*").Length == 1, "repo: el archivo dañado se aparta, no se borra");
    Check(new SaveRepository(repo.Directory).Load(out var after) == LoadOutcome.Loaded && after.Pesos == 1075, "repo: tras recuperar, el principal vuelve a ser válido");

    // ambos dañados -> perfil nuevo, sin excepción
    File.WriteAllText(repo.MainPath, "roto");
    File.WriteAllText(repo.BackupPath, "roto también");
    Check(new SaveRepository(repo.Directory).Load(out var none) == LoadOutcome.ResetCorrupt && !none.HasProfile, "repo: ambos dañados = perfil nuevo");
    Check(Directory.GetFiles(repo.Directory, "profile.corrupt-*").Length == 2, "repo: el segundo dañado también se aparta con otro nombre");
}
{
    // un principal dañado nunca pisa una copia buena
    var repo = new SaveRepository(NewDir());
    var s = Sample(); repo.Save(s); s.Pesos = 3000; repo.Save(s);
    File.WriteAllText(repo.MainPath, "dañado");
    s.Pesos = 4000;
    repo.Save(s);                                                    // guarda sin haber cargado: la copia ($1075) debe sobrevivir
    var probe = SaveCodec.TryParse(File.ReadAllText(repo.BackupPath), out var bak);
    Check(probe == ParseStatus.Ok && bak.Pesos == 1075, "repo: la copia buena no se sobrescribe con un principal dañado");
}
{
    // escritura interrumpida: queda un .tmp a medias y el principal sigue intacto
    var repo = new SaveRepository(NewDir());
    repo.Save(Sample());
    File.WriteAllText(Path.Combine(repo.Directory, SaveRepository.TempFile), "TYCOON3D-SAVE\nversion=1\npes");
    Check(new SaveRepository(repo.Directory).Load(out var l) == LoadOutcome.Loaded && l.Pesos == 1075, "repo: un temporal a medias se ignora");
    Check(!File.Exists(Path.Combine(repo.Directory, SaveRepository.TempFile)), "repo: el temporal a medias se elimina");
}
{
    // versión futura: se juega en memoria y no se sobrescribe
    var repo = new SaveRepository(NewDir());
    Directory.CreateDirectory(repo.Directory);
    string future = Forge(new(), version: SaveCodec.CurrentVersion + 3);
    File.WriteAllText(repo.MainPath, future);
    var again = new SaveRepository(repo.Directory);
    Check(again.Load(out _) == LoadOutcome.FutureVersionReadOnly && again.ReadOnly, "repo: versión futura = solo lectura");
    Check(!again.Save(Sample()) && File.ReadAllText(repo.MainPath) == future, "repo: no se sobrescribe un perfil de versión futura");
}
{
    // directorio imposible: no lanza, informa
    string file = Path.Combine(Path.GetTempPath(), "tycoon-file-" + Guid.NewGuid().ToString("N"));
    File.WriteAllText(file, "x");
    var repo = new SaveRepository(Path.Combine(file, "dentro"));
    Check(repo.Load(out _) == LoadOutcome.ResetCorrupt && repo.LastError != null && !repo.Save(Sample()), "repo: un directorio imposible no lanza excepción");
}

// ---------------------------------------------------------------- casino: fichas, ronda pendiente y reinicio
CasinoSession NewSession(Func<int> pesos, Func<int, bool> spend, Action<int> earn, int tier = 99) => new CasinoSession(new Roll(tier), pesos, spend, earn);
{
    int pesos = 1000;
    var s = NewSession(() => pesos, v => { if (pesos < v) return false; pesos -= v; return true; }, v => pesos += v);
    int worth = s.NetWorth;
    Check(s.BuyChips(20) && pesos == 500 && s.Chips == 20 && s.NetWorth == worth, "fichas: comprar 20 cuesta $500 y el valor total no cambia");
    Check(s.CashOutChips(20) == 500 && pesos == 1000 && s.Chips == 0 && s.NetWorth == worth, "fichas: canjear devuelve exactamente los pesos (sin comisión)");
    Check(!s.BuyChips(41) && pesos == 1000 && s.Chips == 0, "fichas: comprar de más falla y no cambia nada");
    Check(!s.BuyChips(0) && !s.BuyChips(-5), "fichas: cantidades no positivas se rechazan");
    Check(s.CashOutChips(1) == 0 && pesos == 1000, "fichas: canjear sin tener no paga");
    s.BuyChips(4);
    Check(s.CashOutChips(5) == 0 && s.Chips == 4, "fichas: no se canjea más de lo que se tiene");
    Check(s.ChipsBought == 24 && s.ChipsCashed == 20 && s.CashierVisits == 3, "fichas: el libro cuenta compradas, canjeadas y visitas");
}
{
    // la apuesta se paga con fichas si alcanzan, el premio vuelve en fichas, y el jackpot de $25 deja 40 fichas
    int pesos = 100;
    var s = NewSession(() => pesos, v => { if (pesos < v) return false; pesos -= v; return true; }, v => pesos += v, 99);
    s.BuyChips(2);                                                   // 50 pesos -> 2 fichas
    Check(pesos == 50 && s.Chips == 2, "casino: base con 2 fichas y $50");
    s.Begin(out var r);
    Check(pesos == 50 && s.Chips == 1, "casino: apuesta de $25 paga con una ficha y no toca los pesos");
    s.Complete();
    Check(pesos == 50 && s.Chips == 1 + (r.Payout / 25) && s.LastPaidWithChips, "casino: el premio vuelve en fichas, no en pesos");
    Check(s.TotalWagered == 25 && s.TotalWon == r.Payout, "casino: el libro cuenta el equivalente en pesos");
    // sin fichas suficientes, paga con pesos
    var t = NewSession(() => pesos, v => { if (pesos < v) return false; pesos -= v; return true; }, v => pesos += v, 0);
    pesos = 100; t.Begin(out _); t.Complete();
    Check(pesos == 75 && t.Chips == 0 && !t.LastPaidWithChips, "casino: sin fichas, la apuesta sale de los pesos");
    // cortesía solo sin pesos ni fichas
    pesos = 10; var f = NewSession(() => pesos, v => false, v => pesos += v, 0);
    Check(f.NextSpinIsFree, "casino: con $10 y sin fichas, el giro es de cortesía");
    pesos = 10; var g = NewSession(() => pesos, v => { if (pesos < v) return false; pesos -= v; return true; }, v => pesos += v, 0);
    g.BuyChips(1);                                                   // falla: $10 < $25
    Check(g.Chips == 0 && g.NextSpinIsFree, "casino: sin saldo no se puede comprar la primera ficha");
}
{
    // ronda pendiente: se cierra la app girando; al volver se liquida una sola vez
    int pesos = 100;
    var s = NewSession(() => pesos, v => { if (pesos < v) return false; pesos -= v; return true; }, v => pesos += v, 99);
    s.Begin(out var round);                                          // jackpot FOX x40 sobre $25
    Check(pesos == 75 && s.Busy, "pendiente: la apuesta se cobró al aceptar");
    var snap = s.Snapshot();
    Check(snap.PendingActive && snap.PendingStake == 25 && snap.PendingPayout == 1000 && snap.PendingRoundId == 1, "pendiente: la instantánea guarda la ronda aceptada");
    var data = new SaveData { Pesos = pesos };
    CasinoProfileMapping.Write(data, snap);
    SaveCodec.TryParse(SaveCodec.Serialize(data), out var fromDisk);

    int pesos2 = (int)fromDisk.Pesos;                                // otra sesión tras el reinicio
    var s2 = NewSession(() => pesos2, v => { pesos2 -= v; return true; }, v => pesos2 += v, 0);
    int recovered = s2.Restore(CasinoProfileMapping.Read(fromDisk));
    Check(recovered == 1000 && pesos2 == 1075 && !s2.Busy, "pendiente: al volver se acredita el premio una sola vez ($1075)");
    Check(s2.TotalSpins == 1 && s2.TotalWon == 1000 && s2.TotalWagered == 25 && s2.RoundsStarted == 1, "pendiente: el libro queda coherente tras la liquidación");
    var snap2 = s2.Snapshot();
    Check(!snap2.PendingActive, "pendiente: la liquidación limpia la ronda pendiente");
    var data2 = new SaveData { Pesos = pesos2 }; CasinoProfileMapping.Write(data2, snap2);
    int pesos3 = (int)data2.Pesos;
    var s3 = NewSession(() => pesos3, v => true, v => pesos3 += v, 0);
    Check(s3.Restore(CasinoProfileMapping.Read(data2)) == 0 && pesos3 == 1075, "pendiente: un segundo reinicio no paga otra vez");
    Check(Throws(() => s3.Complete()), "pendiente: Complete sin ronda sigue fallando");
}
{
    // pendiente apostada con fichas: el premio vuelve en fichas; pendiente de cortesía: en pesos
    int pesos = 0;
    var s = NewSession(() => pesos, v => false, v => pesos += v, 99);
    var st = new CasinoState { Chips = 3, Rounds = 4, Wagered = 100, PendingActive = true, PendingStake = 25, PendingPayout = 100, PendingChips = true, PendingRoundId = 4 };
    Check(s.Restore(st) == 100 && s.Chips == 3 + 4 && pesos == 0, "pendiente con fichas: el premio vuelve en fichas");
    int pesos2 = 0;
    var s2 = NewSession(() => pesos2, v => false, v => pesos2 += v, 99);
    var st2 = new CasinoState { Rounds = 1, FreeSpins = 1, PendingActive = true, PendingStake = 25, PendingPayout = 75, PendingFree = true, PendingRoundId = 1 };
    Check(s2.Restore(st2) == 75 && pesos2 == 75 && s2.Chips == 0, "pendiente de cortesía: el premio vuelve en pesos");
}
{
    // el estado de otro tamaño o dañado no rompe la restauración
    int pesos = 0;
    var s = NewSession(() => pesos, v => false, v => pesos += v);
    s.Restore(new CasinoState { BetIndex = 99, Chips = -5, Rounds = -1 });
    Check(s.BetIndex == 3 && s.Chips == 0 && s.RoundsStarted == 0, "restaurar: valores fuera de rango se acotan");
}
{
    // simulación de muchas rondas con reinicios aleatorios: el valor total solo cambia por apuestas y premios
    var rng = new Random(7);
    int pesos = 500; int worthStart = pesos; long staked = 0, paid = 0;
    CasinoState saved = default;
    var s = NewSession(() => pesos, v => { if (pesos < v) return false; pesos -= v; return true; }, v => pesos += v, 0);
    for (int i = 0; i < 400; i++)
    {
        int action = rng.Next(5);
        if (action == 0) s.BuyChips(rng.Next(1, 10));
        else if (action == 1) s.CashOutChips(rng.Next(1, 10));
        else if (action == 2 && !s.Busy) s.SetBetIndex(rng.Next(4));
        else if (!s.Busy) { s.Begin(out var rd); if (!rd.FreeSpin) staked += rd.Stake; paid += rd.Payout; }
        if (s.Busy && rng.Next(3) == 0)
        {
            // reinicio a mitad de giro: se guarda, se pierde la sesión y se restaura en una nueva
            saved = s.Snapshot();
            var fresh = NewSession(() => pesos, v => { if (pesos < v) return false; pesos -= v; return true; }, v => pesos += v, 0);
            fresh.Restore(saved);
            s = fresh;
        }
        else if (s.Busy) s.Complete();
        Check(pesos >= 0 && s.Chips >= 0, "simulación: nunca hay saldo ni fichas negativas");
    }
    if (s.Busy) s.Complete();
    Check(s.NetWorth == worthStart - (int)staked + (int)paid, "simulación con reinicios a mitad de giro: valor total = inicial - apostado + premios (" + s.NetWorth + ")");
}
{
    // conservación exacta en la simulación sin reinicios: valor final = inicial - apostado + premios (cortesías pagan premio sin apuesta)
    var rng = new Random(11);
    int pesos = 800; int start = pesos; long staked = 0, paid = 0;
    var s = NewSession(() => pesos, v => { if (pesos < v) return false; pesos -= v; return true; }, v => pesos += v, 0);
    for (int i = 0; i < 300; i++)
    {
        int a = rng.Next(4);
        if (a == 0) s.BuyChips(rng.Next(1, 6)); else if (a == 1) s.CashOutChips(rng.Next(1, 6));
        else { if (s.Begin(out var rd) != SpinStatus.Busy) { if (!rd.FreeSpin) staked += rd.Stake; paid += rd.Payout; s.Complete(); } }
    }
    Check(s.NetWorth == start - staked + paid, "conservación: valor total = inicial - apostado + premios (" + s.NetWorth + ")");
    Check(s.TotalWagered == staked && s.TotalWon == paid, "conservación: el libro coincide con lo apostado y lo pagado");
}
static bool Throws(Action action) { try { action(); return false; } catch (Exception) { return true; } }

Console.WriteLine("SaveSmoke: {0} comprobaciones aprobadas.", checks);

sealed class Roll : IRandomSource
{
    private readonly int tier; private int calls;
    public Roll(int tier) { this.tier = tier; }
    public int Next(int max) => max == 100 ? Math.Min(tier, 99) : (calls++ * 7 + 3) % max;
}

