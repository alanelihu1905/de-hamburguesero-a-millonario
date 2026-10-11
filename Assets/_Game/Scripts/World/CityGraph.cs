using System;
using System.Collections.Generic;

namespace Tycoon3D.World
{
    public enum CityZoneId
    {
        LasAcacias,
        Mercado,
        Muelle,
        Restaurantes,
        Industrial,
        Parque,
        CentroFinanciero,
        AvenidaCentral
    }

    /// <summary>Datos de presentación y función económica para HUD, mapa y encargos.</summary>
    public struct CityZoneInfo
    {
        public CityZoneId Id { get; }
        public string Name { get; }
        public string Description { get; }
        public string Landmark { get; }
        public string Role { get; }

        public CityZoneInfo(CityZoneId id, string name, string description, string landmark, string role)
        {
            Id = id;
            Name = name;
            Description = description;
            Landmark = landmark;
            Role = role;
        }
    }

    /// <summary>Tramo de calle entre zonas. Distancia inicial de diseño, pendiente de calibrar al construir el mapa.</summary>
    public struct CityRoad
    {
        public CityZoneId Destination { get; }
        public int DistanceMeters { get; }

        public CityRoad(CityZoneId destination, int distanceMeters)
        {
            if (distanceMeters <= 0) throw new ArgumentOutOfRangeException(nameof(distanceMeters));
            Destination = destination;
            DistanceMeters = distanceMeters;
        }
    }

    /// <summary>Grafo estable de Puerto Alba. No depende de la escena ni de NavMesh.</summary>
    public sealed class CityGraph
    {
        private static readonly CityZoneInfo[] zoneInfo =
        {
            new CityZoneInfo(CityZoneId.LasAcacias, "Las Acacias", "Barrio inicial y comunidad", "Barrio Burger", "Puesto, vivienda y tutorial"),
            new CityZoneInfo(CityZoneId.Mercado, "Calle Mercado", "Proveedores y comercio diario", "Mercado de Rosa", "Ingredientes, almacenes y contratos"),
            new CityZoneInfo(CityZoneId.Muelle, "Muelle", "Carga, pesca y paseo marítimo", "Terminal del Puerto Alba", "Entregas, importaciones y turismo"),
            new CityZoneInfo(CityZoneId.Restaurantes, "Distrito Gastronómico", "Comida, ocio y competencia", "Calle de los Sabores", "Restaurantes y reputación culinaria"),
            new CityZoneInfo(CityZoneId.Industrial, "Zona Industrial", "Producción y logística", "Centro de Distribución", "Cocinas centrales y almacenes"),
            new CityZoneInfo(CityZoneId.Parque, "Parque del Río", "Espacio público y eventos", "Anfiteatro del Río", "Festivales, clientes y rutas tranquilas"),
            new CityZoneInfo(CityZoneId.CentroFinanciero, "Centro Financiero", "Oficinas, bancos y propiedades", "Torre Puerto Alba", "Préstamos ficticios, contratos y sedes"),
            new CityZoneInfo(CityZoneId.AvenidaCentral, "Avenida Central", "Ocio nocturno y lujo", "Casino Gran Fortuna", "Casino siempre abierto y negocios premium")
        };

        private readonly Dictionary<CityZoneId, CityRoad[]> roads = new Dictionary<CityZoneId, CityRoad[]>
        {
            [CityZoneId.LasAcacias] = new[] { new CityRoad(CityZoneId.Mercado, 650), new CityRoad(CityZoneId.Parque, 520) },
            [CityZoneId.Mercado] = new[] { new CityRoad(CityZoneId.LasAcacias, 650), new CityRoad(CityZoneId.Muelle, 880), new CityRoad(CityZoneId.Restaurantes, 720) },
            [CityZoneId.Muelle] = new[] { new CityRoad(CityZoneId.Mercado, 880), new CityRoad(CityZoneId.Industrial, 600) },
            [CityZoneId.Restaurantes] = new[] { new CityRoad(CityZoneId.Mercado, 720), new CityRoad(CityZoneId.CentroFinanciero, 850), new CityRoad(CityZoneId.AvenidaCentral, 420) },
            [CityZoneId.Industrial] = new[] { new CityRoad(CityZoneId.Muelle, 600), new CityRoad(CityZoneId.CentroFinanciero, 900) },
            [CityZoneId.Parque] = new[] { new CityRoad(CityZoneId.LasAcacias, 520), new CityRoad(CityZoneId.CentroFinanciero, 780) },
            [CityZoneId.CentroFinanciero] = new[] { new CityRoad(CityZoneId.Parque, 780), new CityRoad(CityZoneId.Restaurantes, 850), new CityRoad(CityZoneId.Industrial, 900), new CityRoad(CityZoneId.AvenidaCentral, 560) },
            [CityZoneId.AvenidaCentral] = new[] { new CityRoad(CityZoneId.Restaurantes, 420), new CityRoad(CityZoneId.CentroFinanciero, 560) }
        };

        public IReadOnlyList<CityZoneInfo> Zones => Array.AsReadOnly(zoneInfo);

        public bool TryGetZone(CityZoneId id, out CityZoneInfo info)
        {
            for (int i = 0; i < zoneInfo.Length; i++)
                if (zoneInfo[i].Id == id) { info = zoneInfo[i]; return true; }
            info = default;
            return false;
        }

        public IReadOnlyList<CityRoad> RoadsFrom(CityZoneId zone) => roads.TryGetValue(zone, out var result)
            ? result
            : Array.Empty<CityRoad>();

        public IReadOnlyList<CityZoneId> Neighbors(CityZoneId zone)
        {
            var result = RoadsFrom(zone);
            var neighbors = new CityZoneId[result.Count];
            for (int i = 0; i < result.Count; i++) neighbors[i] = result[i].Destination;
            return neighbors;
        }

        /// <summary>Ruta de menor distancia; en empates conserva el orden estable de los tramos.</summary>
        public IReadOnlyList<CityZoneId> FindRoute(CityZoneId start, CityZoneId destination)
        {
            if (!roads.ContainsKey(start) || !roads.ContainsKey(destination)) return Array.Empty<CityZoneId>();
            var distance = new Dictionary<CityZoneId, int>();
            var previous = new Dictionary<CityZoneId, CityZoneId>();
            var unvisited = new List<CityZoneId>(zoneInfo.Length);
            foreach (var zone in zoneInfo) { distance[zone.Id] = int.MaxValue; unvisited.Add(zone.Id); }
            distance[start] = 0;

            while (unvisited.Count > 0)
            {
                int bestIndex = 0;
                for (int i = 1; i < unvisited.Count; i++)
                    if (distance[unvisited[i]] < distance[unvisited[bestIndex]]) bestIndex = i;
                CityZoneId current = unvisited[bestIndex];
                unvisited.RemoveAt(bestIndex);
                if (distance[current] == int.MaxValue || current == destination) break;

                foreach (var road in roads[current])
                {
                    int candidate = distance[current] + road.DistanceMeters;
                    if (candidate < distance[road.Destination])
                    {
                        distance[road.Destination] = candidate;
                        previous[road.Destination] = current;
                    }
                }
            }

            if (distance[destination] == int.MaxValue) return Array.Empty<CityZoneId>();
            var route = new List<CityZoneId>();
            CityZoneId cursor = destination;
            route.Add(cursor);
            while (cursor != start) { cursor = previous[cursor]; route.Add(cursor); }
            route.Reverse();
            return route;
        }

        public int RouteDistanceMeters(IReadOnlyList<CityZoneId> route)
        {
            if (route == null || route.Count == 0) return 0;
            int total = 0;
            for (int i = 1; i < route.Count; i++)
            {
                bool found = false;
                foreach (var road in RoadsFrom(route[i - 1]))
                    if (road.Destination == route[i]) { total = checked(total + road.DistanceMeters); found = true; break; }
                if (!found) return 0;
            }
            return total;
        }
    }
}
