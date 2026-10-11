using System.Collections.Generic;
using System.Linq;

namespace Tycoon3D.City
{
    /// <summary>Rectángulo en el plano XZ que ocupa una zona construida. Mínimo incluido, máximo excluido.</summary>
    public struct ZoneBox
    {
        public ZoneId Zone;
        public float MinX, MinZ, MaxX, MaxZ;

        public bool Contains(float x, float z) => x >= MinX && x < MaxX && z >= MinZ && z < MaxZ;
        public bool Overlaps(ZoneBox o) => MinX < o.MaxX && o.MinX < MaxX && MinZ < o.MaxZ && o.MinZ < MaxZ;
        public float CenterX => (MinX + MaxX) * .5f;
        public float CenterZ => (MinZ + MaxZ) * .5f;
    }

    /// <summary>
    /// Geometría de Puerto Alba para las zonas ya construidas. Entre dos zonas hay una calle (fuera de cualquier caja):
    /// ahí no hay zona y el seguidor la reporta como «en la calle». Sin Unity: se prueba con dotnet.
    /// </summary>
    public static class CityLayout
    {
        public static readonly ZoneBox[] Built =
        {
            new ZoneBox { Zone = ZoneId.CasaMarea,     MinX = -10f, MinZ = -10f, MaxX = 10f, MaxZ = 10f },
            new ZoneBox { Zone = ZoneId.MercadoDelSol, MinX = 22f,  MinZ = -10f, MaxX = 40f, MaxZ = 10f },
        };

        /// <summary>Calle entre Casa Marea y el mercado (no es zona).</summary>
        public static readonly ZoneBox Street = new ZoneBox { Zone = ZoneId.CasaMarea, MinX = 10f, MinZ = -2f, MaxX = 22f, MaxZ = 2f };

        /// <summary>Zona en la que está un punto, o null si está en la calle o fuera de las zonas construidas.</summary>
        public static ZoneId? Locate(float x, float z)
        {
            foreach (var box in Built)
                if (box.Contains(x, z)) return box.Zone;
            return null;
        }

        /// <summary>Lista de problemas de la geometría. Vacía significa válida.</summary>
        public static List<string> Validate()
        {
            var problems = new List<string>();
            for (int i = 0; i < Built.Length; i++)
            {
                if (Built[i].MinX >= Built[i].MaxX || Built[i].MinZ >= Built[i].MaxZ) problems.Add(Built[i].Zone + ": caja vacía o invertida.");
                if (!CityMap.Zones.Any(z => z.Id == Built[i].Zone)) problems.Add(Built[i].Zone + ": zona inexistente en el mapa.");
                for (int j = i + 1; j < Built.Length; j++)
                    if (Built[i].Overlaps(Built[j])) problems.Add("Cajas solapadas: " + Built[i].Zone + " y " + Built[j].Zone + ".");
                if (Built[i].Overlaps(Street)) problems.Add("La calle invade " + Built[i].Zone + ".");
            }
            return problems;
        }
    }
}
