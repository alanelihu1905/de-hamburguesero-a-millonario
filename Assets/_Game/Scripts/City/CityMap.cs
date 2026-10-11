using System;
using System.Collections.Generic;
using System.Linq;

namespace Tycoon3D.City
{
    /// <summary>Zonas de Puerto Alba (ver Docs/PLAN_CIUDAD_PUERTO_ALBA.md). Sin Unity: se valida con dotnet.</summary>
    public enum ZoneId { CasaMarea, MercadoDelSol, MuelleFarol, CentroDelFaro, TalleresBrisa, PaseoArena, ColinaCobalto, EstacionSur }

    public sealed class ZoneInfo
    {
        public ZoneId Id;
        public string Name;
        public string Purpose;
        public int BusinessSlots;   // lugares comprables o arrendables en la zona
    }

    /// <summary>
    /// Mapa de conexiones entre zonas. Es un grafo no dirigido: si A conecta con B, B conecta con A.
    /// Las rutas se calculan en número de cruces (BFS), suficiente para la fase 1.
    /// </summary>
    public static class CityMap
    {
        public static readonly ZoneId Start = ZoneId.CasaMarea;
        public static readonly ZoneId CasinoZone = ZoneId.CasaMarea;

        public static readonly ZoneInfo[] Zones =
        {
            new ZoneInfo { Id = ZoneId.CasaMarea,     Name = "Casa Marea",     Purpose = "Inicio, tutorial práctico y retorno seguro.",         BusinessSlots = 2 },
            new ZoneInfo { Id = ZoneId.MercadoDelSol, Name = "Mercado del Sol", Purpose = "Abastecimiento y comparación de precios.",          BusinessSlots = 3 },
            new ZoneInfo { Id = ZoneId.MuelleFarol,   Name = "Muelle Farol",    Purpose = "Recepción de mercancía y reparto de mayor volumen.", BusinessSlots = 2 },
            new ZoneInfo { Id = ZoneId.CentroDelFaro, Name = "Centro del Faro", Purpose = "Administración, contratos y clientela de oficina.",  BusinessSlots = 3 },
            new ZoneInfo { Id = ZoneId.TalleresBrisa, Name = "Talleres Brisa",  Purpose = "Mejoras, mantenimiento y logística.",                BusinessSlots = 2 },
            new ZoneInfo { Id = ZoneId.PaseoArena,    Name = "Paseo Arena",     Purpose = "Demanda nocturna y entretenimiento.",                BusinessSlots = 2 },
            new ZoneInfo { Id = ZoneId.ColinaCobalto, Name = "Colina Cobalto",  Purpose = "Vivienda, propiedades y servicios de proximidad.",   BusinessSlots = 2 },
            new ZoneInfo { Id = ZoneId.EstacionSur,   Name = "Estación Sur",    Purpose = "Intercambio de rutas y tráfico.",                    BusinessSlots = 2 },
        };

        // Avenida central: centro, mercado, Casa Marea y estación. Anillo secundario: muelle, talleres, paseo y colina.
        public static readonly (ZoneId, ZoneId)[] Links =
        {
            (ZoneId.CasaMarea, ZoneId.MercadoDelSol),
            (ZoneId.CasaMarea, ZoneId.CentroDelFaro),
            (ZoneId.CentroDelFaro, ZoneId.EstacionSur),
            (ZoneId.MercadoDelSol, ZoneId.MuelleFarol),
            (ZoneId.MuelleFarol, ZoneId.TalleresBrisa),
            (ZoneId.TalleresBrisa, ZoneId.EstacionSur),
            (ZoneId.CentroDelFaro, ZoneId.PaseoArena),
            (ZoneId.PaseoArena, ZoneId.ColinaCobalto),
            (ZoneId.ColinaCobalto, ZoneId.EstacionSur),
        };

        public static ZoneInfo Get(ZoneId id) => Zones.First(z => z.Id == id);

        public static IReadOnlyList<ZoneId> NeighborsOf(ZoneId id)
        {
            var result = new List<ZoneId>();
            foreach (var (a, b) in Links)
            {
                if (a == id) result.Add(b);
                else if (b == id) result.Add(a);
            }
            return result;
        }

        /// <summary>Camino más corto por número de cruces, o null si no hay ruta.</summary>
        public static IReadOnlyList<ZoneId> Route(ZoneId from, ZoneId to)
        {
            var previous = new Dictionary<ZoneId, ZoneId>();
            var queue = new Queue<ZoneId>();
            var seen = new HashSet<ZoneId> { from };
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == to) break;
                foreach (var next in NeighborsOf(current))
                {
                    if (!seen.Add(next)) continue;
                    previous[next] = current;
                    queue.Enqueue(next);
                }
            }
            if (!seen.Contains(to)) return null;
            var path = new List<ZoneId> { to };
            while (path[0] != from) path.Insert(0, previous[path[0]]);
            return path;
        }

        /// <summary>Vecinos de una zona según una lista de enlaces dada (para validar copias del mapa).</summary>
        public static List<ZoneId> NeighborsIn(IReadOnlyList<(ZoneId, ZoneId)> links, ZoneId id)
        {
            var result = new List<ZoneId>();
            foreach (var (a, b) in links)
            {
                if (a == id) result.Add(b);
                else if (b == id) result.Add(a);
            }
            return result;
        }

        /// <summary>Lista de problemas del mapa. Vacía significa válido.</summary>
        public static List<string> Validate() => Validate(Zones, Links);

        /// <summary>Valida una combinación de zonas y enlaces; las pruebas la usan con datos rotos a propósito.</summary>
        public static List<string> Validate(IReadOnlyList<ZoneInfo> zones, IReadOnlyList<(ZoneId, ZoneId)> links)
        {
            var problems = new List<string>();
            if (zones.Count != 8) problems.Add("Deben existir exactamente 8 zonas; hay " + zones.Count + ".");
            if (zones.Select(z => z.Id).Distinct().Count() != zones.Count) problems.Add("Hay zonas repetidas.");
            foreach (var z in zones)
            {
                if (string.IsNullOrWhiteSpace(z.Name) || string.IsNullOrWhiteSpace(z.Purpose)) problems.Add(z.Id + ": falta nombre o propósito.");
                if (z.BusinessSlots < 1) problems.Add(z.Id + ": sin lugar para negocios.");
            }
            var known = new HashSet<ZoneId>(zones.Select(z => z.Id));
            for (int i = 0; i < links.Count; i++)
            {
                var (a, b) = links[i];
                if (!known.Contains(a) || !known.Contains(b)) problems.Add("Enlace hacia una zona inexistente: " + a + "–" + b + ".");
                if (a == b) problems.Add("Enlace de una zona consigo misma: " + a + ".");
                for (int j = i + 1; j < links.Count; j++)
                {
                    var (c, d) = links[j];
                    if ((a == c && b == d) || (a == d && b == c)) problems.Add("Enlace duplicado: " + a + "–" + b + ".");
                }
            }
            if (zones.Count == 0) return problems;
            var start = known.Contains(Start) ? Start : zones[0].Id;
            var reached = Reachable(links, start);
            foreach (var z in zones)
            {
                if (NeighborsIn(links, z.Id).Count == 0) problems.Add(z.Id + ": aislada, sin conexiones.");
                else if (!reached.Contains(z.Id)) problems.Add(z.Id + ": no alcanzable desde el inicio.");
            }
            if (!known.Contains(Start)) problems.Add("Falta la zona de inicio.");
            if (CasinoZone != Start) problems.Add("El casino debe estar en la zona de inicio: no puede exigir recorrido ni compra previa.");
            if (!reached.Contains(CasinoZone)) problems.Add("El casino no es alcanzable desde el inicio.");
            return problems;
        }

        private static HashSet<ZoneId> Reachable(IReadOnlyList<(ZoneId, ZoneId)> links, ZoneId from)
        {
            var seen = new HashSet<ZoneId> { from };
            var queue = new Queue<ZoneId>();
            queue.Enqueue(from);
            while (queue.Count > 0)
                foreach (var next in NeighborsIn(links, queue.Dequeue()))
                    if (seen.Add(next)) queue.Enqueue(next);
            return seen;
        }
    }
}
