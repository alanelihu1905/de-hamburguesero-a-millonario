using System;
using System.Collections.Generic;

namespace Tycoon3D.World
{
    public enum CityBlockFeature { Building, Park, GasStation, Casino, Market, Port, Restaurant, IndustrialYard }

    /// <summary>Deterministic 21 by 21 block layout. Coordinate (0,0) is city center.</summary>
    public sealed class CityGridLayout
    {
        public const int BlocksFromCenter = 10;
        public const int SideLength = BlocksFromCenter * 2 + 1;
        public const float BlockPitchMeters = 84f;
        public const float RoadWidthMeters = 12f;

        private static readonly (CityZoneId zone, int x, int z)[] anchors =
        {
            (CityZoneId.LasAcacias, -7, -7), (CityZoneId.Mercado, -6, 1),
            (CityZoneId.Muelle, 0, 9), (CityZoneId.Restaurantes, 6, 2),
            (CityZoneId.Industrial, 8, -5), (CityZoneId.Parque, -2, 6),
            (CityZoneId.CentroFinanciero, 3, -4), (CityZoneId.AvenidaCentral, 0, 0)
        };

        private readonly CityBlockDefinition[] blocks;
        public IReadOnlyList<CityBlockDefinition> Blocks => blocks;
        public float DiameterMeters => SideLength * BlockPitchMeters + RoadWidthMeters;

        public CityGridLayout()
        {
            blocks = new CityBlockDefinition[SideLength * SideLength];
            int i = 0;
            for (int z = -BlocksFromCenter; z <= BlocksFromCenter; z++)
                for (int x = -BlocksFromCenter; x <= BlocksFromCenter; x++)
                    blocks[i++] = CreateBlock(x, z);
        }

        public bool TryGetBlock(int x, int z, out CityBlockDefinition block)
        {
            if (x < -BlocksFromCenter || x > BlocksFromCenter || z < -BlocksFromCenter || z > BlocksFromCenter)
            { block = default; return false; }
            block = blocks[(z + BlocksFromCenter) * SideLength + x + BlocksFromCenter];
            return true;
        }

        private static CityBlockDefinition CreateBlock(int x, int z)
        {
            CityZoneId zone = NearestZone(x, z);
            CityBlockFeature feature = CityBlockFeature.Building;
            if (x == 0 && z == 0) feature = CityBlockFeature.Casino;
            else if ((x == -5 && z == -7) || (x == 8 && z == -4)) feature = CityBlockFeature.GasStation;
            else if (x == -6 && z == 1) feature = CityBlockFeature.Market;
            else if (x == 0 && z == 9) feature = CityBlockFeature.Port;
            else if (x == 6 && z == 2) feature = CityBlockFeature.Restaurant;
            else if (x == 8 && z == -5) feature = CityBlockFeature.IndustrialYard;
            else if ((Math.Abs(x + 2) <= 1 && Math.Abs(z - 6) <= 1) ||
                     (Math.Abs(x - 1) <= 1 && Math.Abs(z - 6) <= 1)) feature = CityBlockFeature.Park;

            // A few front doors are usable. Most buildings remain exterior-only blockout.
            bool hasDoor = feature == CityBlockFeature.Building && StableHash(x, z) % 5 == 0;
            return new CityBlockDefinition(x, z, zone, feature, hasDoor);
        }

        private static CityZoneId NearestZone(int x, int z)
        {
            int bestDistance = int.MaxValue;
            CityZoneId best = CityZoneId.LasAcacias;
            for (int i = 0; i < anchors.Length; i++)
            {
                int distance = Math.Abs(x - anchors[i].x) + Math.Abs(z - anchors[i].z);
                if (distance < bestDistance) { bestDistance = distance; best = anchors[i].zone; }
            }
            return best;
        }

        private static int StableHash(int x, int z) => Math.Abs(unchecked(x * 73856093 ^ z * 19349663)) % 100;
    }

    public struct CityBlockDefinition
    {
        public int X { get; }
        public int Z { get; }
        public CityZoneId Zone { get; }
        public CityBlockFeature Feature { get; }
        public bool HasNpcDoor { get; }

        public CityBlockDefinition(int x, int z, CityZoneId zone, CityBlockFeature feature, bool hasNpcDoor)
        { X = x; Z = z; Zone = zone; Feature = feature; HasNpcDoor = hasNpcDoor; }
    }
}
