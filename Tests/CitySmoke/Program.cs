using Tycoon3D.World;

var graph = new CityGraph();
var checks = 0;
void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); checks++; }

Check(graph.Zones.Count == 8, "Puerto Alba tiene ocho zonas estables");
Check(graph.Zones.Select(zone => zone.Id).Distinct().Count() == 8, "los IDs de zona son únicos");
foreach (var zone in graph.Zones)
{
    Check(graph.Neighbors(zone.Id).Count > 0, $"zona aislada: {zone.Id}");
    Check(!string.IsNullOrWhiteSpace(zone.Name) && !string.IsNullOrWhiteSpace(zone.Landmark), $"zona sin identidad visual: {zone.Id}");
    foreach (var neighbor in graph.Neighbors(zone.Id)) Check(graph.Neighbors(neighbor).Contains(zone.Id), $"enlace no bidireccional: {zone.Id}-{neighbor}");
}
foreach (var destination in graph.Zones)
{
    var route = graph.FindRoute(CityZoneId.LasAcacias, destination.Id);
    Check(route.Count > 0 && route[0] == CityZoneId.LasAcacias && route[^1] == destination.Id, $"no hay ruta a {destination.Id}");
    Check(graph.RouteDistanceMeters(route) > 0 || destination.Id == CityZoneId.LasAcacias, $"ruta sin distancia a {destination.Id}");
}
var casinoRoute = graph.FindRoute(CityZoneId.LasAcacias, CityZoneId.AvenidaCentral);
Check(graph.RouteDistanceMeters(casinoRoute) == 1790, "la ruta más corta inicial al casino suma 1790 m");
Check(graph.FindRoute(CityZoneId.AvenidaCentral, CityZoneId.AvenidaCentral).Count == 1, "ruta consigo misma");
Check(graph.TryGetZone(CityZoneId.Muelle, out var port) && port.Name == "Muelle", "metadatos de zona consultables");
Console.WriteLine($"CitySmoke: {checks} comprobaciones aprobadas. Ocho zonas con propósito y rutas por distancia; casino alcanzable desde Las Acacias.");
