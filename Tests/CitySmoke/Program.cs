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
var grid = new CityGridLayout();
Check(grid.Blocks.Count == 441, "ciudad cubre 21 x 21 cuadras");
Check(grid.DiameterMeters == 1776f, "huella urbana mide 1,776 m por lado");
Check(grid.TryGetBlock(-10, -10, out _) && grid.TryGetBlock(10, 10, out _), "cuadrícula incluye radio de diez cuadras");
Check(!grid.TryGetBlock(-11, 0, out _), "cuadrícula rechaza exterior del radio");
Check(grid.Blocks.Select(block => block.Zone).Distinct().Count() == 8, "las ocho zonas ocupan bloques");
Check(grid.Blocks.Count(block => block.Feature == CityBlockFeature.Casino) == 1, "casino siempre existe en centro");
Check(grid.Blocks.Count(block => block.Feature == CityBlockFeature.GasStation) == 2, "ciudad contiene dos gasolineras");
Check(grid.Blocks.Count(block => block.HasNpcDoor) >= 50, "hay puertas tocables en varios edificios");
Check(grid.TryGetBlock(0, 0, out var center) && center.Feature == CityBlockFeature.Casino, "casino está en bloque central");
Console.WriteLine($"CitySmoke: {checks} comprobaciones aprobadas. Cuadrícula 21x21, casino central, gasolineras y puertas NPC.");
