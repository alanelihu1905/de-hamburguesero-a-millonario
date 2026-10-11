using Tycoon3D.City;
using Tycoon3D.World;

var checks = 0;
void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); checks++; }

Check(CityMap.Zones.Length == 8, "Puerto Alba tiene ocho zonas estables");
Check(CityMap.Zones.Select(zone => zone.Id).Distinct().Count() == 8, "los IDs de zona son únicos");
Check(CityMap.Validate().Count == 0, "grafo de zonas conectado y válido");
Check(CityMap.Start == ZoneId.CasaMarea && CityMap.CasinoZone == CityMap.Start, "casino permanece disponible en zona inicial");
foreach (var zone in CityMap.Zones)
{
    Check(!string.IsNullOrWhiteSpace(zone.Name) && !string.IsNullOrWhiteSpace(zone.Purpose), $"zona sin identidad: {zone.Id}");
    Check(CityMap.NeighborsOf(zone.Id).Count > 0, $"zona aislada: {zone.Id}");
    Check(CityMap.NeighborsOf(zone.Id).Distinct().Count() == CityMap.NeighborsOf(zone.Id).Count, $"vecinos repetidos: {zone.Id}");
    foreach (var neighbor in CityMap.NeighborsOf(zone.Id))
        Check(CityMap.NeighborsOf(neighbor).Contains(zone.Id), $"enlace no bidireccional: {zone.Id}-{neighbor}");
    var route = CityMap.Route(CityMap.Start, zone.Id);
    Check(route != null && route[0] == CityMap.Start && route[^1] == zone.Id, $"no hay ruta a {zone.Id}");
}
Check(CityMap.Route(CityMap.Start, CityMap.CasinoZone)?.Count == 1, "casino no requiere recorrido desde el inicio");
var grid = new CityGridLayout();
Check(grid.Blocks.Count == 441, "ciudad cubre 21 x 21 cuadras");
Check(grid.DiameterMeters == 1776f, "huella urbana mide 1,776 m por lado");
Check(grid.TryGetBlock(-10, -10, out _) && grid.TryGetBlock(10, 10, out _), "cuadrícula incluye radio de diez cuadras");
Check(!grid.TryGetBlock(-11, 0, out _), "cuadrícula rechaza exterior del radio");
Check(grid.Blocks.Select(block => block.Zone).Distinct().Count() == CityMap.Zones.Length, "las ocho zonas ocupan bloques");
Check(grid.Blocks.Count(block => block.Feature == CityBlockFeature.Casino) == 1, "casino siempre existe en el inicio");
Check(grid.Blocks.Count(block => block.Feature == CityBlockFeature.GasStation) == 2, "ciudad contiene dos gasolineras");
Check(grid.Blocks.Count(block => block.Feature == CityBlockFeature.OfficeTower) == 2, "centro financiero tiene dos torres de silueta distinta");
Check(grid.Blocks.Count(block => block.Feature == CityBlockFeature.TransitHub) == 1, "Estación Sur tiene terminal de transporte");
Check(grid.Blocks.Count(block => block.Feature == CityBlockFeature.Warehouse) >= 10, "puerto y zona industrial tienen bodegas");
Check(grid.Blocks.Count(block => block.Feature == CityBlockFeature.Apartment) >= 8, "Colina Cobalto tiene edificios de vivienda");
Check(grid.Blocks.Count(block => block.Feature == CityBlockFeature.Park) >= 6, "Paseo Arena conserva un parque de varias cuadras");
Check(grid.Blocks.Count(block => block.HasNpcDoor) >= 50, "hay puertas tocables en varios edificios");
Check(grid.Blocks.Where(block => block.HasNpcDoor).All(block => block.Feature == CityBlockFeature.Building), "puertas NPC solo aparecen en fachadas sencillas");
Check(grid.Blocks.Where(block => block.Feature == CityBlockFeature.GasStation).Select(block => block.Zone).Distinct().Count() == 2,
    "gasolineras cubren dos zonas distintas");
Check(new CityGridLayout().Blocks.Select(block => block.HasNpcDoor).SequenceEqual(grid.Blocks.Select(block => block.HasNpcDoor)),
    "las posiciones de puertas son reproducibles");
Check(grid.TryGetBlock(0, 0, out var center) && center.Feature == CityBlockFeature.Casino && center.Zone == CityMap.CasinoZone,
    "casino está en cuadra inicial Casa Marea");
Console.WriteLine($"CitySmoke: {checks} comprobaciones aprobadas. Puerto Alba 21x21 con 8 zonas, casino, torres, viviendas, estación, bodegas y parques.");
