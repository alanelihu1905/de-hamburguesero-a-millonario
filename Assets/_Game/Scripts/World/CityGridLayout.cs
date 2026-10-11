using System;
using System.Collections.Generic;
using Tycoon3D.City;

namespace Tycoon3D.World
{
    public enum CityBlockFeature
    {
        Building, Park, GasStation, Casino, Market, Port, Restaurant, IndustrialYard,
        OfficeTower, Apartment, Warehouse, TransitHub, Plaza
    }

    /// <summary>Deterministic 21 by 21 block layout. Coordinate (0,0) is city center.</summary>
    public sealed class CityGridLayout
    {
        public const int BlocksFromCenter = 10;
        public const int SideLength = BlocksFromCenter * 2 + 1;
        public const float BlockPitchMeters = 84f;
        public const float RoadWidthMeters = 12f;

        private static readonly (ZoneId zone, int x, int z)[] anchors =
        {
            (ZoneId.CasaMarea, 0, 0), (ZoneId.MercadoDelSol, -6, 1),
            (ZoneId.MuelleFarol, 0, 9), (ZoneId.CentroDelFaro, 3, -4),
            (ZoneId.TalleresBrisa, 8, -5), (ZoneId.PaseoArena, -2, 6),
            (ZoneId.ColinaCobalto, 6, 2), (ZoneId.EstacionSur, 0, -9)
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

        /// <summary>Finds the block whose center is nearest to a world-space X/Z position.</summary>
        public bool TryGetBlockAtWorldPosition(float worldX, float worldZ, out CityBlockDefinition block)
        {
            float halfExtent = SideLength * BlockPitchMeters * .5f;
            if (float.IsNaN(worldX) || float.IsInfinity(worldX) ||
                float.IsNaN(worldZ) || float.IsInfinity(worldZ) ||
                Math.Abs(worldX) > halfExtent || Math.Abs(worldZ) > halfExtent)
            {
                block = default;
                return false;
            }

            int x = (int)Math.Floor(worldX / BlockPitchMeters + .5f);
            int z = (int)Math.Floor(worldZ / BlockPitchMeters + .5f);
            x = Math.Max(-BlocksFromCenter, Math.Min(BlocksFromCenter, x));
            z = Math.Max(-BlocksFromCenter, Math.Min(BlocksFromCenter, z));
            return TryGetBlock(x, z, out block);
        }

        private static CityBlockDefinition CreateBlock(int x, int z)
        {
            ZoneId zone = NearestZone(x, z);
            CityBlockFeature feature = CityBlockFeature.Building;
            if (x == 0 && z == 0) feature = CityBlockFeature.Casino;
            else if ((x == 0 && z == -8) || (x == 8 && z == -4)) feature = CityBlockFeature.GasStation;
            else if (x == -6 && z == 1) feature = CityBlockFeature.Market;
            else if (x == 0 && z == 9) feature = CityBlockFeature.Port;
            else if (x == 6 && z == 2) feature = CityBlockFeature.Restaurant;
            else if (x == 8 && z == -5) feature = CityBlockFeature.IndustrialYard;
            else if (x == 0 && z == -9) feature = CityBlockFeature.TransitHub;
            else if ((x == 2 || x == 4) && z == -4) feature = CityBlockFeature.OfficeTower;
            else if ((x == -1 || x == 1) && z == 9 || (x == 0 && z == 8)) feature = CityBlockFeature.Warehouse;
            else if (zone == ZoneId.TalleresBrisa && x >= 7 && x <= 9 && z >= -7 && z <= -3)
                feature = CityBlockFeature.Warehouse;
            else if (zone == ZoneId.ColinaCobalto && StableHash(x, z) % 4 == 0) feature = CityBlockFeature.Apartment;
            else if ((Math.Abs(x + 2) <= 1 && Math.Abs(z - 6) <= 1) ||
                     (Math.Abs(x - 1) <= 1 && Math.Abs(z - 6) <= 1)) feature = CityBlockFeature.Park;
            else if (zone == ZoneId.CentroDelFaro && Math.Abs(x - 3) <= 1 && Math.Abs(z + 4) <= 1)
                feature = CityBlockFeature.Plaza;

            // A few front doors are usable. Most buildings remain exterior-only blockout.
            bool hasDoor = feature == CityBlockFeature.Building && StableHash(x, z) % 5 == 0;
            return new CityBlockDefinition(x, z, zone, feature, hasDoor);
        }

        private static ZoneId NearestZone(int x, int z)
        {
            int bestDistance = int.MaxValue;
            ZoneId best = ZoneId.CasaMarea;
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
        public ZoneId Zone { get; }
        public CityBlockFeature Feature { get; }
        public bool HasNpcDoor { get; }

        public CityBlockDefinition(int x, int z, ZoneId zone, CityBlockFeature feature, bool hasNpcDoor)
        { X = x; Z = z; Zone = zone; Feature = feature; HasNpcDoor = hasNpcDoor; }
    }
}
