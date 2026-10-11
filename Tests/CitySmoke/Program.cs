using Tycoon3D.World;

var graph = new CityGraph();
var checks = 0;
void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); checks++; }

Check(graph.Zones.Count == 8, "Puerto Alba tiene ocho zonas estables");
Check(graph.Zones.Distinct().Count() == 8, "los IDs de zona son únicos");
foreach (var zone in graph.Zones)
{
    Check(graph.Neighbors(zone).Count > 0, $"zona aislada: {zone}");
    foreach (var neighbor in graph.Neighbors(zone)) Check(graph.Neighbors(neighbor).Contains(zone), $"enlace no bidireccional: {zone}-{neighbor}");
}
foreach (var destination in graph.Zones)
{
    var route = graph.FindRoute(CityZoneId.LasAcacias, destination);
    Check(route.Count > 0 && route[0] == CityZoneId.LasAcacias && route[^1] == destination, $"no hay ruta a {destination}");
}
var casinoRoute = graph.FindRoute(CityZoneId.LasAcacias, CityZoneId.AvenidaCentral);
Check(casinoRoute.Count == 4, "la ruta inicial al casino es determinista");
Check(graph.FindRoute(CityZoneId.AvenidaCentral, CityZoneId.AvenidaCentral).Count == 1, "ruta consigo misma");
Console.WriteLine($"CitySmoke: {checks} comprobaciones aprobadas. Puerto Alba conectado; casino alcanzable desde Las Acacias.");
