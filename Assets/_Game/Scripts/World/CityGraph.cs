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

    /// <summary>Grafo estable de Puerto Alba. No depende de la escena ni de NavMesh.</summary>
    public sealed class CityGraph
    {
        private readonly Dictionary<CityZoneId, CityZoneId[]> links = new()
        {
            [CityZoneId.LasAcacias] = new[] { CityZoneId.Mercado, CityZoneId.Parque },
            [CityZoneId.Mercado] = new[] { CityZoneId.LasAcacias, CityZoneId.Muelle, CityZoneId.Restaurantes },
            [CityZoneId.Muelle] = new[] { CityZoneId.Mercado, CityZoneId.Industrial },
            [CityZoneId.Restaurantes] = new[] { CityZoneId.Mercado, CityZoneId.CentroFinanciero, CityZoneId.AvenidaCentral },
            [CityZoneId.Industrial] = new[] { CityZoneId.Muelle, CityZoneId.CentroFinanciero },
            [CityZoneId.Parque] = new[] { CityZoneId.LasAcacias, CityZoneId.CentroFinanciero },
            [CityZoneId.CentroFinanciero] = new[] { CityZoneId.Parque, CityZoneId.Restaurantes, CityZoneId.Industrial, CityZoneId.AvenidaCentral },
            [CityZoneId.AvenidaCentral] = new[] { CityZoneId.Restaurantes, CityZoneId.CentroFinanciero }
        };

        public IReadOnlyList<CityZoneId> Zones => new[]
        {
            CityZoneId.LasAcacias, CityZoneId.Mercado, CityZoneId.Muelle, CityZoneId.Restaurantes,
            CityZoneId.Industrial, CityZoneId.Parque, CityZoneId.CentroFinanciero, CityZoneId.AvenidaCentral
        };

        public IReadOnlyList<CityZoneId> Neighbors(CityZoneId zone) => links.TryGetValue(zone, out var result)
            ? result
            : Array.Empty<CityZoneId>();

        public IReadOnlyList<CityZoneId> FindRoute(CityZoneId start, CityZoneId destination)
        {
            if (!links.ContainsKey(start) || !links.ContainsKey(destination)) return Array.Empty<CityZoneId>();
            var previous = new Dictionary<CityZoneId, CityZoneId?> { [start] = null };
            var queue = new Queue<CityZoneId>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == destination) break;
                foreach (var next in links[current])
                    if (!previous.ContainsKey(next)) { previous[next] = current; queue.Enqueue(next); }
            }
            if (!previous.ContainsKey(destination)) return Array.Empty<CityZoneId>();
            var route = new List<CityZoneId>();
            for (CityZoneId? cursor = destination; cursor.HasValue; cursor = previous[cursor.Value]) route.Add(cursor.Value);
            route.Reverse();
            return route;
        }
    }
}
