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

Console.WriteLine("CitySmoke: {0} comprobaciones aprobadas.", checks);
