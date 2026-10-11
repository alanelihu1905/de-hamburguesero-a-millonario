using System;
using System.Linq;
using Tycoon3D.City;

int checks = 0;
void Check(bool ok, string what)
{
    checks++;
    if (!ok) { Console.WriteLine("FALLO: " + what); Environment.Exit(1); }
}

// ---- mapa válido
Check(CityMap.Zones.Length == 8, "hay 8 zonas");
var problems = CityMap.Validate();
Check(problems.Count == 0, "el mapa es válido: " + string.Join(" | ", problems));
Check(CityMap.Zones.All(z => !string.IsNullOrWhiteSpace(z.Name) && !string.IsNullOrWhiteSpace(z.Purpose)), "cada zona tiene nombre y propósito");
Check(CityMap.Zones.All(z => z.BusinessSlots >= 1), "cada zona tiene lugar para negocios");
Check(CityMap.Zones.Select(z => z.Name).Distinct().Count() == 8, "nombres de zona distintos");

// ---- grafo no dirigido y sin repeticiones
foreach (var (a, b) in CityMap.Links)
    Check(CityMap.NeighborsOf(a).Contains(b) && CityMap.NeighborsOf(b).Contains(a), "enlace simétrico " + a + "–" + b);
Check(CityMap.Links.Select(l => (Math.Min((int)l.Item1, (int)l.Item2), Math.Max((int)l.Item1, (int)l.Item2))).Distinct().Count() == CityMap.Links.Length,
    "sin enlaces duplicados");
Check(CityMap.Zones.All(z => CityMap.NeighborsOf(z.Id).Count >= 1), "ninguna zona aislada");

// ---- alcanzabilidad: toda zona es alcanzable desde el inicio
foreach (var z in CityMap.Zones)
    Check(CityMap.Route(CityMap.Start, z.Id) != null, "alcanzable desde Casa Marea: " + z.Name);

// ---- casino: abierto desde el inicio, sin requisito de zona
Check(CityMap.CasinoZone == ZoneId.CasaMarea && CityMap.Start == ZoneId.CasaMarea, "el casino está en la zona de inicio");
Check(CityMap.Route(CityMap.Start, CityMap.CasinoZone).Count == 1, "el casino no exige ruta ni compra previa");

// ---- rutas mínimas conocidas
var r1 = CityMap.Route(ZoneId.CasaMarea, ZoneId.MercadoDelSol);
Check(r1.Count == 2 && r1[0] == ZoneId.CasaMarea && r1[1] == ZoneId.MercadoDelSol, "Casa Marea → Mercado directo");
var r2 = CityMap.Route(ZoneId.CasaMarea, ZoneId.MuelleFarol);
Check(r2.Count == 3 && r2[1] == ZoneId.MercadoDelSol && r2[2] == ZoneId.MuelleFarol, "Casa Marea → Muelle pasa por el mercado (3 zonas)");
var r3 = CityMap.Route(ZoneId.ColinaCobalto, ZoneId.TalleresBrisa);
Check(r3 != null && r3.First() == ZoneId.ColinaCobalto && r3.Last() == ZoneId.TalleresBrisa, "ruta de la colina a los talleres existe");
Check(r3.Count == r3.Distinct().Count(), "la ruta no repite zonas");
for (int i = 0; i + 1 < r3.Count; i++)
    Check(CityMap.NeighborsOf(r3[i]).Contains(r3[i + 1]), "cada paso de la ruta es un enlace real");
Check(CityMap.Route(ZoneId.MuelleFarol, ZoneId.MuelleFarol).Count == 1, "ruta a la misma zona = una zona");

Check(CityMap.Links.Length == 9, "nueve enlaces en el diseño de fase 1");

// ---- el validador detecta cada fallo con datos rotos a propósito
var zs = CityMap.Zones.ToList();
var ls = CityMap.Links.ToList();
bool Flags(System.Collections.Generic.IReadOnlyList<ZoneInfo> z, System.Collections.Generic.IReadOnlyList<(ZoneId, ZoneId)> l, string frag) =>
    CityMap.Validate(z, l).Any(p => p.Contains(frag));

Check(Flags(zs, ls.Where(l => !(l.Item1 == ZoneId.ColinaCobalto || l.Item2 == ZoneId.ColinaCobalto)).ToList(), "aislada"), "detecta zona aislada");
Check(Flags(zs, ls.Append((ZoneId.CasaMarea, ZoneId.CasaMarea)).ToList(), "consigo misma"), "detecta enlace a sí misma");
Check(Flags(zs, ls.Append((ZoneId.MercadoDelSol, ZoneId.CasaMarea)).ToList(), "duplicado"), "detecta enlace duplicado");
Check(Flags(zs.Take(7).ToList(), ls.Where(l => l.Item1 != ZoneId.EstacionSur && l.Item2 != ZoneId.EstacionSur).ToList(), "8 zonas"), "detecta menos de 8 zonas");
Check(Flags(zs, ls.Append((ZoneId.CasaMarea, (ZoneId)99)).ToList(), "inexistente"), "detecta enlace a zona inexistente");
var zsSinNombre = zs.Select(z => new ZoneInfo { Id = z.Id, Name = "", Purpose = z.Purpose, BusinessSlots = z.BusinessSlots }).ToList();
Check(Flags(zsSinNombre, ls, "falta nombre"), "detecta zona sin nombre");
var zsSinNegocio = zs.Select(z => new ZoneInfo { Id = z.Id, Name = z.Name, Purpose = z.Purpose, BusinessSlots = z.Id == ZoneId.MuelleFarol ? 0 : z.BusinessSlots }).ToList();
Check(Flags(zsSinNegocio, ls, "sin lugar para negocios"), "detecta zona sin lugar para negocios");
// zona de inicio (con el casino) cortada del resto: debe reportarse como aislada
var sinSalida = ls.Where(l => l.Item1 != ZoneId.CasaMarea && l.Item2 != ZoneId.CasaMarea).ToList();
Check(Flags(zs, sinSalida, "aislada"), "detecta que la zona del casino queda aislada");
Check(Flags(zs, sinSalida, "no alcanzable desde el inicio"), "detecta zonas sin ruta desde el inicio");
Check(CityMap.Validate(zs, ls).Count == 0, "el mapa de fase 1 sigue siendo válido");

// ---- geometría de las zonas construidas
Check(CityLayout.Validate().Count == 0, "la geometría es válida: " + string.Join(" | ", CityLayout.Validate()));
Check(CityLayout.Built.Length == 2, "la escena de fase 1 construye dos zonas");
foreach (var box in CityLayout.Built)
    Check(CityLayout.Locate(box.CenterX, box.CenterZ) == box.Zone, "el centro de " + box.Zone + " cae en esa zona");
Check(CityLayout.Locate(CityLayout.Street.CenterX, CityLayout.Street.CenterZ) == null, "la calle no es zona");
Check(CityLayout.Locate(0f, 0f) == ZoneId.CasaMarea && CityLayout.Locate(30f, 0f) == ZoneId.MercadoDelSol, "puntos de prueba del smoke de Play Mode");
Check(CityLayout.Locate(10f, 0f) == null && CityLayout.Locate(22f, 0f) != null, "la calle separa las dos zonas");
Check(CityMap.Links.Any(l => (l.Item1 == ZoneId.CasaMarea && l.Item2 == ZoneId.MercadoDelSol) || (l.Item1 == ZoneId.MercadoDelSol && l.Item2 == ZoneId.CasaMarea)),
    "Casa Marea y el mercado están conectados en el mapa");
Check(!CityLayout.Built[0].Overlaps(CityLayout.Built[1]) && !CityLayout.Built[0].Overlaps(CityLayout.Street), "las cajas no se solapan con la calle");

// ---- HUD: disposición en las tres resoluciones objetivo y con los tamaños de texto extremos
var resoluciones = new (int w, int h)[] { (1280, 720), (1600, 900), (2560, 1080) };
foreach (var (w, h) in resoluciones)
foreach (var texto in new[] { CityAjustes.TextoMin, CityAjustes.TextoPorDefecto, CityAjustes.TextoMax })
{
    string id = w + "x" + h + " texto " + texto;
    var f = CityHudLayout.Compute(w, h, texto);
    var pantalla = new HudRect(0, 0, w, h);
    Check(f.Banner.Inside(pantalla), "banner dentro de " + id);
    Check(f.Minimap.Inside(pantalla), "minimapa dentro de " + id);
    Check(f.Hint.Inside(pantalla), "pista dentro de " + id);
    Check(f.Settings.Inside(pantalla), "ajustes dentro de " + id);
    Check(f.MinimapMap.Inside(f.Minimap), "mapa dentro de su panel en " + id);
    Check(!f.Banner.Overlaps(f.Minimap), "banner y minimapa no se solapan en " + id);
    Check(!f.Hint.Overlaps(f.Minimap), "pista y minimapa no se solapan en " + id);
    Check(f.FontSize >= 12, "texto legible (" + f.FontSize + " px) en " + id);
    // el jugador en cualquier punto de la ciudad cae dentro del mapa
    foreach (var (x, z) in new[] { (0f, 0f), (16f, 0f), (30f, 0f), (-10f, -10f), (40f, 10f) })
    {
        var (px, py) = CityHudLayout.ToMinimap(x, z, f.MinimapMap);
        Check(f.MinimapMap.Contains(px, py), "punto (" + x + "," + z + ") dentro del mapa en " + id);
    }
}
var f1280 = CityHudLayout.Compute(1280, 720, 1f);
Check(f1280.Scale == 1f && f1280.FontSize == 14, "1280×720 con texto normal es la escala de diseño");
Check(CityHudLayout.Compute(2560, 1080, 1f).Scale == 1.5f, "2560×1080 escala 1,5");
Check(CityHudLayout.Compute(3840, 2160, 1f).Scale == 2f, "la escala se limita a 2 en 4K");
Check(CityHudLayout.Compute(1280, 720, 9f).Scale == CityAjustes.TextoMax, "el texto grande se limita a su máximo");
// el norte (z+) queda arriba: un punto más al norte tiene y menor
var (_, yNorte) = CityHudLayout.ToMinimap(0f, 5f, f1280.MinimapMap);
var (_, ySur) = CityHudLayout.ToMinimap(0f, -5f, f1280.MinimapMap);
Check(yNorte < ySur, "el norte del minimapa queda arriba");
// la caja de Casa Marea cae a la izquierda de la del mercado
var casa = CityHudLayout.BoxToMinimap(CityLayout.Built[0], f1280.MinimapMap);
var mercado = CityHudLayout.BoxToMinimap(CityLayout.Built[1], f1280.MinimapMap);
Check(casa.X < mercado.X && casa.Inside(f1280.MinimapMap), "Casa Marea queda a la izquierda del mercado en el minimapa");

// ---- ajustes: límites, valores ilegibles y guardado ida y vuelta
var ajustes = new CityAjustes();
ajustes.Set(99f, 500f, 9f);
Check(ajustes.Sensibilidad == CityAjustes.SensibilidadMax && ajustes.Fov == CityAjustes.FovMax && ajustes.EscalaTexto == CityAjustes.TextoMax, "los ajustes altos se limitan al máximo");
ajustes.Set(-3f, -1f, 0f);
Check(ajustes.Sensibilidad == CityAjustes.SensibilidadMin && ajustes.Fov == CityAjustes.FovMin && ajustes.EscalaTexto == CityAjustes.TextoMin, "los ajustes bajos se limitan al mínimo");
ajustes.Set(float.NaN, float.PositiveInfinity, float.NegativeInfinity);
Check(ajustes.Sensibilidad == CityAjustes.SensibilidadPorDefecto && ajustes.Fov == CityAjustes.FovPorDefecto && ajustes.EscalaTexto == CityAjustes.TextoPorDefecto, "NaN e infinitos vuelven al valor por defecto");
var almacen = new MemoriaAjustes();
almacen.SetFloat(CityAjustes.KeyFov, 75f);
almacen.SetFloat(CityAjustes.KeySensibilidad, float.NaN);
var cargado = new CityAjustes();
cargado.Load(almacen);
Check(cargado.Fov == 75f && cargado.Sensibilidad == CityAjustes.SensibilidadPorDefecto && cargado.EscalaTexto == CityAjustes.TextoPorDefecto,
    "al cargar, un valor ilegible usa su defecto y el resto se conserva");
var guardado = new MemoriaAjustes();
cargado.Set(3.5f, 80f, 1.25f);
cargado.Save(guardado);
var recargado = new CityAjustes();
recargado.Load(guardado);
Check(recargado.Sensibilidad == 3.5f && recargado.Fov == 80f && recargado.EscalaTexto == 1.25f, "los ajustes sobreviven a guardar y cargar");
Check(new CityAjustes().Sensibilidad == CityAjustes.SensibilidadPorDefecto, "sin datos guardados, los ajustes usan su defecto");
new CityAjustes().Load(null);
Check(true, "cargar sin almacén no falla");

Console.WriteLine("CitySmoke: {0} comprobaciones aprobadas.", checks);

sealed class MemoriaAjustes : IAjustesStore
{
    private readonly System.Collections.Generic.Dictionary<string, float> datos = new();
    public bool TryGetFloat(string key, out float value) => datos.TryGetValue(key, out value);
    public void SetFloat(string key, float value) => datos[key] = value;
}
