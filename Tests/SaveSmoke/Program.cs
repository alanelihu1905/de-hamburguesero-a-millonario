using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tycoon3D.Casino;
using Tycoon3D.Economy;
using Tycoon3D.Save;

int checks = 0;
void Check(bool ok, string what)
{
    checks++;
    if (!ok) { Console.WriteLine("FALLO: " + what); Environment.Exit(1); }
}
LoadOutcome Reload(string dir, out SaveData data)
{
    using var r = new SaveRepository(dir);                              // abrir y cerrar la app: el candado se toma y se suelta
    return r.Load(out data);
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
    Check(SaveCodec.TryParse(Forge(new(), version: 0), out var mig, migrations) == ParseStatus.Ok && mig.Chips == 4 && mig.Pesos == 100 &&
        mig.LedgerOpeningPesos == 100 && mig.LedgerOpeningChips == 4, "migración: v0 pasa por v1 y conserva ambos saldos");
    Check(SaveCodec.TryParse(Forge(new() { ["chips"] = "17", ["burgerSales"] = "3", ["pendingActive"] = "1",
        ["pendingStake"] = "25", ["pendingPayout"] = "1000" }, version: 1), out var legacy) == ParseStatus.Ok &&
        legacy.Pesos == 50 && legacy.Chips == 17 && legacy.BurgerSales == 3 && legacy.PendingPayout == 1000 &&
        legacy.LedgerOpeningPesos == 50 && legacy.LedgerOpeningChips == 17, "migración: perfil v1 conserva saldo, ventas, fichas y ronda pendiente");
}

// ---------------------------------------------------------------- libro común: tipos, persistencia, recorte y conservación
{
    var ledger = new MovementLedger();
    var data = new SaveData { Pesos = 100 };
    ledger.Restore(data, 100, 0);
    Check(ledger.Matches(100, 0) && ledger.Entries.Count == 1 && ledger.Entries[0].Kind == MovementKind.Opening,
        "libro: perfil nuevo abre con saldo inicial");
    ledger.Record(MovementKind.Ingredients, 15, MovementAccount.Wallet, MovementAccount.Outside);
    ledger.Record(MovementKind.Sale, 50, MovementAccount.Outside, MovementAccount.Wallet);
    ledger.Record(MovementKind.BuyChips, 100, MovementAccount.Wallet, MovementAccount.Chips);
    ledger.Record(MovementKind.Stake, 25, MovementAccount.Chips, MovementAccount.Casino, 7);
    ledger.Record(MovementKind.Payout, 1000, MovementAccount.Casino, MovementAccount.Chips, 7);
    ledger.Record(MovementKind.CashOut, 100, MovementAccount.Chips, MovementAccount.Wallet);
    Check(ledger.Matches(135, 39) && 135 + 39 * 25 == 100 - 15 + 50 - 25 + 1000,
        "libro: compra y canje conservan valor; apuesta y premio lo ajustan");
    ledger.Write(data);
    Check(SaveCodec.TryParse(SaveCodec.Serialize(data), out var loaded) == ParseStatus.Ok && loaded.LedgerEntries.Length == 7,
        "libro: todos los tipos persisten en v2");
    var again = new MovementLedger(); again.Restore(loaded, 135, 39);
    Check(again.Matches(135, 39) && again.Entries[4].RoundId == 7 && again.Sequence == ledger.Sequence,
        "libro: reinicio conserva saldo, orden e ID de ronda");
    loaded.LedgerEntries[0] = default;
    Check(data.LedgerEntries[0].Id == 1, "libro: copia de datos no comparte entradas");
    var clone = data.Clone(); clone.LedgerEntries[0] = default;
    Check(data.LedgerEntries[0].Id == 1, "libro: Clone copia el historial");
    for (int i = 0; i < 50; i++) ledger.Record(MovementKind.Courtesy, 0, MovementAccount.Wallet, MovementAccount.Casino, i + 8);
    Check(ledger.Entries.Count == MovementLedger.Capacity && ledger.Matches(135, 39) && ledger.Sequence == 57,
        "libro: recorte a 32 conserva saldos e IDs");
    ledger.Write(data);
    Check(SaveCodec.TryParse(SaveCodec.Serialize(data), out var capped) == ParseStatus.Ok, "libro: historial recortado se carga");
    again.Restore(capped, 135, 39);
    Check(again.Matches(135, 39), "libro: saldos auditables tras recorte y reinicio");
    Check(SaveCodec.TryParse(Forge(new() { ["ledgerCount"] = "33" }), out _) == ParseStatus.Corrupt,
        "libro: rechaza historial sobre límite");
    Check(SaveCodec.TryParse(Forge(new() { ["ledgerCount"] = "1" }), out _) == ParseStatus.Corrupt,
        "libro: rechaza entrada incompleta");
}

// ---------------------------------------------------------------- caja del casino en el perfil (formato v3)
{
    var data = Sample();
    data.CashboxInit = 1; data.CashboxFunds = 14000; data.CashboxOwed = 75000; data.CashboxRefilled = 900;
    Check(SaveCodec.CurrentVersion >= 3, "caja: el formato es v3 o posterior");
    Check(SaveCodec.TryParse(SaveCodec.Serialize(data), out var back) == ParseStatus.Ok && back.CashboxInit == 1 && back.CashboxFunds == 14000 &&
        back.CashboxOwed == 75000 && back.CashboxRefilled == 900, "caja: fondo, deuda y reposición sobreviven al guardado");
    Check(SaveCodec.TryParse(Forge(new() { ["cashboxFunds"] = "-5" }), out _) == ParseStatus.Corrupt, "caja: fondo negativo = dañado");
    Check(SaveCodec.TryParse(Forge(new() { ["cashboxOwed"] = "-1" }), out _) == ParseStatus.Corrupt, "caja: deuda negativa = dañado");
    Check(SaveCodec.TryParse(Forge(new() { ["cashboxInit"] = "2" }), out _) == ParseStatus.Corrupt, "caja: marca de inicio fuera de rango = dañado");
    var viejo = Forge(new() { ["chips"] = "4", ["burgerSales"] = "2", ["ledgerOpeningPesos"] = "50", ["ledgerOpeningChips"] = "4", ["ledgerCount"] = "0" }, version: 2);
    Check(SaveCodec.TryParse(viejo, out var v2) == ParseStatus.Ok && v2.CashboxInit == 0 && v2.CashboxFunds == 0 && v2.Chips == 4 && v2.BurgerSales == 2,
        "migración: un perfil v2 se abre sin caja y conserva fichas y ventas");
    // el movimiento de reposición de la casa viaja en el libro
    var ledger = new MovementLedger();
    var d2 = new SaveData { Pesos = 100 };
    ledger.Restore(d2, 100, 0);
    ledger.Record(MovementKind.Refill, 900, MovementAccount.Outside, MovementAccount.Casino, 3);
    ledger.Write(d2);
    Check(SaveCodec.TryParse(SaveCodec.Serialize(d2), out var l2) == ParseStatus.Ok && l2.LedgerEntries[1].Kind == MovementKind.Refill && ledger.Matches(100, 0),
        "caja: la reposición se guarda en el libro sin tocar pesos ni fichas del jugador");
}

// ---------------------------------------------------------------- reloj del juego en el perfil (formato v4)
{
    var data = Sample();
    Check(new SaveData().ClockMinutes == -1, "reloj: sin guardar vale -1");
    data.ClockMinutes = 3 * 1440 + 21 * 60;
    Check(SaveCodec.CurrentVersion >= 4, "reloj: el formato es v4 o posterior");
    Check(SaveCodec.TryParse(SaveCodec.Serialize(data), out var back) == ParseStatus.Ok && back.ClockMinutes == 3 * 1440 + 21 * 60, "reloj: la hora sobrevive al guardado");
    Check(SaveCodec.TryParse(Forge(new() { ["clockMinutes"] = "-2" }), out _) == ParseStatus.Corrupt, "reloj: valor menor que -1 = dañado");
    var viejo = Forge(new() { ["chips"] = "4", ["cashboxInit"] = "1", ["cashboxFunds"] = "14000" }, version: 3);
    Check(SaveCodec.TryParse(viejo, out var v3) == ParseStatus.Ok && v3.ClockMinutes == -1 && v3.CashboxFunds == 14000 && v3.Chips == 4,
        "migración: un perfil v3 se abre sin hora y conserva su caja");
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
    repo.Dispose();                                                  // cerrar la app suelta el candado del perfil
    Check(Reload(repo.Directory, out var l2) == LoadOutcome.Loaded && l2.Pesos == 2000, "repo: otra instancia (otro arranque) ve $2000");

    // principal dañado -> recupera la copia ($1075) y deja el principal válido
    File.WriteAllText(repo.MainPath, "TYCOON3D-SAVE\nversion=1\npesos=1\nchecksum=deadbeef\n");
    var r2 = new SaveRepository(repo.Directory);
    Check(r2.Load(out var rec) == LoadOutcome.RecoveredFromBackup && rec.Pesos == 1075, "repo: principal dañado se recupera desde la copia");
    Check(Directory.GetFiles(repo.Directory, "profile.corrupt-*").Length == 1, "repo: el archivo dañado se aparta, no se borra");
    r2.Dispose();
    Check(Reload(repo.Directory, out var after) == LoadOutcome.Loaded && after.Pesos == 1075, "repo: tras recuperar, el principal vuelve a ser válido");

    // ambos dañados -> perfil nuevo, sin excepción
    File.WriteAllText(repo.MainPath, "roto");
    File.WriteAllText(repo.BackupPath, "roto también");
    Check(Reload(repo.Directory, out var none) == LoadOutcome.ResetCorrupt && !none.HasProfile, "repo: ambos dañados = perfil nuevo");
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
    repo.Dispose();
    Check(Reload(repo.Directory, out var l) == LoadOutcome.Loaded && l.Pesos == 1075, "repo: un temporal a medias se ignora");
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

// ---------------------------------------------------------------- dos copias de la app con el mismo perfil
{
    string dir = NewDir();
    var primera = new SaveRepository(dir);
    Check(primera.Load(out _) == LoadOutcome.Fresh && primera.HoldsLock && !primera.LockedByOtherInstance, "candado: la primera copia toma el perfil");
    var datos = Sample();
    Check(primera.Save(datos), "candado: la primera copia guarda");
    string antes = File.ReadAllText(primera.MainPath);
    string tmpAjeno = Path.Combine(dir, SaveRepository.TempFile);
    File.WriteAllText(tmpAjeno, "TYCOON3D-SAVE" + (char)10 + "version=3" + (char)10 + "pes");  // la primera copia está a mitad de un guardado

    var segunda = new SaveRepository(dir);
    Check(segunda.Load(out var visto) == LoadOutcome.LockedByOtherInstance && segunda.LockedByOtherInstance && segunda.ReadOnly && !segunda.HoldsLock,
        "candado: la segunda copia lo detecta y queda en solo lectura");
    Check(visto.Pesos == 1075 && visto.Chips == 17, "candado: la segunda copia ve lo último que guardó la primera");
    datos.Pesos = 5;
    Check(!segunda.Save(datos) && segunda.LastError != null, "candado: la segunda copia no puede guardar");
    Check(File.ReadAllText(primera.MainPath) == antes && File.Exists(tmpAjeno) && !File.Exists(segunda.BackupPath) && Directory.GetFiles(dir, "profile.corrupt-*").Length == 0,
        "candado: la segunda copia no toca el disco (ni borra el temporal de la primera)");
    Check(primera.Save(datos) && SaveCodec.TryParse(File.ReadAllText(primera.MainPath), out var guardado) == ParseStatus.Ok && guardado.Pesos == 5,
        "candado: la primera copia sigue guardando con normalidad");

    // si el principal de la primera está a medias y la copia es válida, la segunda lee la copia sin moverla ni repararla
    File.WriteAllText(primera.MainPath, "a medias");
    var tercera = new SaveRepository(dir);
    Check(tercera.Load(out var viaCopia) == LoadOutcome.LockedByOtherInstance && viaCopia.HasProfile && File.ReadAllText(primera.MainPath) == "a medias",
        "candado: con el principal ilegible la segunda copia lee la copia de seguridad sin repararla");

    // al cerrar la primera copia (o caerse: el sistema suelta el archivo) el perfil queda libre
    primera.Dispose();
    primera.Dispose();
    var cuarta = new SaveRepository(dir);
    Check(cuarta.Load(out _) != LoadOutcome.LockedByOtherInstance && cuarta.HoldsLock && !cuarta.ReadOnly, "candado: tras cerrar la primera, otra copia toma el perfil");
    Check(cuarta.Save(Sample()), "candado: y puede guardar");
    cuarta.Dispose();
    // perfiles distintos no se estorban
    using (var a = new SaveRepository(NewDir())) using (var b = new SaveRepository(NewDir()))
        Check(a.Load(out _) == LoadOutcome.Fresh && b.Load(out _) == LoadOutcome.Fresh, "candado: dos perfiles en carpetas distintas no se estorban");
    // volver a leer el disco en la misma app (equivale a reabrir) libera y retoma el candado
    string d2 = NewDir();
    var uno = new SaveRepository(d2); uno.Load(out _);
    uno.Dispose();
    var dos = new SaveRepository(d2);
    Check(dos.Load(out _) == LoadOutcome.Fresh && dos.HoldsLock, "candado: soltar y volver a tomar el perfil en la misma app funciona");
    dos.Dispose();
    // sin Load previo, guardar también respeta el candado
    string d3 = NewDir();
    using (var duenio = new SaveRepository(d3))
    {
        duenio.Load(out _);
        var intruso = new SaveRepository(d3);
        Check(!intruso.Save(Sample()) && !File.Exists(Path.Combine(d3, SaveRepository.BackupFile)), "candado: guardar sin haber cargado tampoco pisa un perfil ocupado");
    }
}

// ---------------------------------------------------------------- casino: fichas, ronda pendiente y reinicio
CasinoSession NewSession(Func<int> pesos, Func<int, bool> spend, Action<int> earn, int tier = 99) => new CasinoSession(new Roll(tier), pesos, spend, earn);
{
    int pesos = 1000;
    var s = NewSession(() => pesos, v => { if (pesos < v) return false; pesos -= v; return true; }, v => pesos += v);
    var ledger = new MovementLedger(); ledger.Restore(new SaveData(), pesos, 0);
    s.MovementRecorded += (kind, amount, from, to, round) => ledger.Record(kind, amount, from, to, round);
    int worth = s.NetWorth;
    Check(s.BuyChips(20) && pesos == 500 && s.Chips == 20 && s.NetWorth == worth, "fichas: comprar 20 cuesta $500 y el valor total no cambia");
    Check(s.CashOutChips(20) == 500 && pesos == 1000 && s.Chips == 0 && s.NetWorth == worth, "fichas: canjear devuelve exactamente los pesos (sin comisión)");
    Check(!s.BuyChips(41) && pesos == 1000 && s.Chips == 0, "fichas: comprar de más falla y no cambia nada");
    Check(!s.BuyChips(0) && !s.BuyChips(-5), "fichas: cantidades no positivas se rechazan");
    Check(s.CashOutChips(1) == 0 && pesos == 1000, "fichas: canjear sin tener no paga");
    s.BuyChips(4);
    Check(s.CashOutChips(5) == 0 && s.Chips == 4, "fichas: no se canjea más de lo que se tiene");
    Check(s.ChipsBought == 24 && s.ChipsCashed == 20 && s.CashierVisits == 3, "fichas: el libro cuenta compradas, canjeadas y visitas");
    Check(ledger.Matches(pesos, s.Chips) && ledger.Entries.Count == 4, "libro común: la cajera registra solo canjes completados");
}
{
    int pesos = 25;
    var s = NewSession(() => pesos, v => { if (pesos < v) return false; pesos -= v; return true; },
        v => { if (pesos <= int.MaxValue - v) pesos += v; });
    Check(s.BuyChips(1), "cajera: prepara una ficha para probar el límite del monedero");
    int visits = s.CashierVisits;
    pesos = int.MaxValue - 24;
    Check(s.CashOutChips(1) == 0 && s.Chips == 1 && pesos == int.MaxValue - 24 && s.CashierVisits == visits,
        "cajera: saldo al límite no consume ficha ni registra visita");
    pesos = int.MaxValue - 25;
    Check(s.CashOutChips(1) == 25 && s.Chips == 0 && pesos == int.MaxValue,
        "cajera: canje exacto hasta el máximo acredita los pesos");
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
    var ledger = new MovementLedger(); ledger.Restore(fromDisk, pesos2, 0);
    s2.MovementRecorded += (kind, amount, from, to, id) => ledger.Record(kind, amount, from, to, id);
    int recovered = s2.Restore(CasinoProfileMapping.Read(fromDisk));
    Check(recovered == 1000 && pesos2 == 1075 && !s2.Busy, "pendiente: al volver se acredita el premio una sola vez ($1075)");
    Check(ledger.Matches(1075, 0) && ledger.Entries.Count == 2 && ledger.Entries[1].Kind == MovementKind.Payout,
        "pendiente: el libro común registra el premio recuperado una vez");
    Check(s2.TotalSpins == 1 && s2.TotalWon == 1000 && s2.TotalWagered == 25 && s2.RoundsStarted == 1, "pendiente: el libro queda coherente tras la liquidación");
    var snap2 = s2.Snapshot();
    Check(!snap2.PendingActive, "pendiente: la liquidación limpia la ronda pendiente");
    var data2 = new SaveData { Pesos = pesos2 }; CasinoProfileMapping.Write(data2, snap2); ledger.Write(data2);
    int pesos3 = (int)data2.Pesos;
    var s3 = NewSession(() => pesos3, v => true, v => pesos3 += v, 0);
    var ledger3 = new MovementLedger(); ledger3.Restore(data2, pesos3, 0);
    s3.MovementRecorded += (kind, amount, from, to, id) => ledger3.Record(kind, amount, from, to, id);
    Check(s3.Restore(CasinoProfileMapping.Read(data2)) == 0 && pesos3 == 1075, "pendiente: un segundo reinicio no paga otra vez");
    Check(ledger3.Entries.Count == 2 && ledger3.Matches(1075, 0), "pendiente: un segundo reinicio no duplica el premio en el libro");
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
