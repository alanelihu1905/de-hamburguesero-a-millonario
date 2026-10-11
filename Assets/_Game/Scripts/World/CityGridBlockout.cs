using Tycoon3D.Interaction;
using Tycoon3D.City;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon3D.World
{
    /// <summary>Deterministic city blockout with district landmarks, street furniture and looping traffic.</summary>
    public sealed class CityGridBlockout : MonoBehaviour
    {
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private int seed = 42017;
        private static readonly Color asphalt = new Color(.20f, .23f, .23f);
        private static readonly Color sidewalk = new Color(.68f, .62f, .52f);
        private static readonly Color stucco = new Color(.85f, .75f, .61f);
        private static readonly Color roof = new Color(.52f, .28f, .19f);
        private static readonly Color grass = new Color(.24f, .42f, .25f);
        private static readonly Color glass = new Color(.12f, .30f, .38f);
        private static readonly Color cream = new Color(.91f, .82f, .65f);
        private static readonly Color roadMark = new Color(.88f, .81f, .60f);
        private static readonly Color palmGreen = new Color(.16f, .38f, .23f);
        private static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

        private void Start() { if (buildOnStart) Build(); }

        [ContextMenu("Build Puerto Alba Grid")]
        public void Build()
        {
            if (transform.childCount > 0) return;
            var layout = new CityGridLayout();
            var roadMat = MakeMaterial("City Asphalt", asphalt);
            var sidewalkMat = MakeMaterial("City Sidewalk", sidewalk);
            var greenMat = MakeMaterial("City Park Green", grass);
            var markMat = MakeMaterial("Street Markings", roadMark);
            float pitch = CityGridLayout.BlockPitchMeters;
            float road = CityGridLayout.RoadWidthMeters;
            float extent = CityGridLayout.SideLength * pitch;
            Cube("City Ground", new Vector3(0, -.35f, 0), new Vector3(extent + road, .6f, extent + road), greenMat, true);

            for (int i = -CityGridLayout.BlocksFromCenter; i <= CityGridLayout.BlocksFromCenter + 1; i++)
            {
                float edge = (i - .5f) * pitch;
                bool arterial = i == 0 || i == 1;
                float width = arterial ? 22f : road;
                Cube(arterial ? "Boulevard X" : "Street X", new Vector3(0, -.015f, edge), new Vector3(extent, .08f, width), roadMat, false);
                Cube(arterial ? "Boulevard Z" : "Street Z", new Vector3(edge, -.014f, 0), new Vector3(width, .08f, extent), roadMat, false);
                float walkOffset = width * .5f + 1.5f;
                Cube("Sidewalk X north", new Vector3(0, .02f, edge + walkOffset), new Vector3(extent, .16f, 3f), sidewalkMat, false);
                Cube("Sidewalk X south", new Vector3(0, .02f, edge - walkOffset), new Vector3(extent, .16f, 3f), sidewalkMat, false);
                Cube("Sidewalk Z east", new Vector3(edge + walkOffset, .02f, 0), new Vector3(3f, .16f, extent), sidewalkMat, false);
                Cube("Sidewalk Z west", new Vector3(edge - walkOffset, .02f, 0), new Vector3(3f, .16f, extent), sidewalkMat, false);
                if (arterial)
                {
                    for (int span = -9; span <= 9; span += 2)
                    {
                        float along = span * pitch;
                        Cube("Avenida center dash", new Vector3(along, .04f, edge), new Vector3(7f, .04f, .22f), markMat, false);
                        Cube("Avenida center dash", new Vector3(edge, .04f, along), new Vector3(.22f, .04f, 7f), markMat, false);
                    }
                }
            }

            foreach (var block in layout.Blocks)
                BuildBlock(block, pitch, road, WallForZone(block.Zone), RoofForZone(block.Zone), greenMat);
            BuildCrosswalks(markMat);
            BuildStreetLife(pitch);
            BuildDistrictGateways(pitch);
            BuildTraffic(pitch);
            Debug.Log($"TYCOON_CITY_GRID_BUILT blocks={layout.Blocks.Count} diameter={layout.DiameterMeters:0}m zones=8 arterials=2 traffic=16 seed={seed}");
        }

        private void BuildBlock(CityBlockDefinition block, float pitch, float road,
            Material wall, Material roofMaterial, Material green)
        {
            float x = block.X * pitch, z = block.Z * pitch;
            float lot = pitch - road - 12f;
            if (block.Feature == CityBlockFeature.Park)
            {
                BuildPark(x, z, lot, green);
                return;
            }

            if (block.Feature == CityBlockFeature.GasStation)
            {
                BuildGasStation(x, z, lot, wall, roofMaterial);
                return;
            }

            if (block.Feature == CityBlockFeature.TransitHub)
            {
                BuildTransitHub(x, z, lot, wall, roofMaterial);
                return;
            }

            if (block.Feature == CityBlockFeature.Plaza)
            {
                BuildPlaza(x, z, lot, green);
                return;
            }

            float height = BuildingHeight(block);
            float footprint = lot * (block.Feature == CityBlockFeature.Warehouse ? .92f : .78f);
            Material facade = FacadeFor(block, wall);
            Material top = RoofFor(block, roofMaterial);
            Cube(BuildingName(block), new Vector3(x, height * .5f, z), new Vector3(footprint, height, footprint), facade, true, block.HasNpcDoor);
            BuildRoofline(block, x, z, footprint, height, top);
            if (block.Feature == CityBlockFeature.OfficeTower)
                BuildTowerWindows(x, z, footprint, height);
            else if (block.Feature == CityBlockFeature.Apartment || block.Feature == CityBlockFeature.Building)
                BuildFacadeDetails(block, x, z, footprint, height, block.HasNpcDoor);

            if (block.Feature == CityBlockFeature.Casino)
            {
                Cube("Casino entrance canopy", new Vector3(x, 4.2f, z - footprint * .5f - 3f), new Vector3(20, .8f, 5), top, false);
                Cube("Casino lit sign", new Vector3(x, 9f, z - footprint * .5f - 2.6f), new Vector3(18, 1.6f, .4f), MakeMaterial("Casino Marquee", new Color(1f, .65f, .1f)), false);
                TextSign("CASINO FOX 24H", new Vector3(x, 9f, z - footprint * .5f - 2.9f), .24f, Color.white);
            }
            else if (block.Feature == CityBlockFeature.Market)
            {
                Cube("Market awning", new Vector3(x, 7.2f, z - footprint * .5f - 2f), new Vector3(34, .7f, 5), MakeMaterial("Market Awning", new Color(.91f, .51f, .13f)), false);
                TextSign("MERCADO DEL SOL", new Vector3(x, height + 1.4f, z - footprint * .5f - .3f), .20f, Color.white);
            }
            else if (block.Feature == CityBlockFeature.Port)
            {
                Cube("Harbor pier", new Vector3(x, .2f, z + lot * .3f), new Vector3(lot * .72f, .45f, 24), MakeMaterial("Pier Timber", new Color(.43f, .30f, .19f)), false);
                BuildHarborCrane(x + lot * .22f, z + lot * .2f);
                TextSign("MUELLE FAROL", new Vector3(x, height + 1f, z - footprint * .5f - .2f), .20f, Color.white);
            }
            else if (block.Feature == CityBlockFeature.Restaurant)
            {
                BuildRestaurantTerrace(x, z, lot);
                TextSign("LOS SABORES", new Vector3(x, height + 1f, z - footprint * .5f - .2f), .20f, Color.white);
            }
            else if (block.Feature == CityBlockFeature.IndustrialYard)
            {
                BuildIndustrialDetails(x, z, lot);
                TextSign("TALLERES BRISA", new Vector3(x, height + 1.2f, z - footprint * .5f - .2f), .18f, Color.white);
            }
            else if (block.Feature == CityBlockFeature.OfficeTower)
                TextSign(block.X == 2 ? "TORRE FARO" : "CENTRO DEL FARO", new Vector3(x, height + 2f, z - footprint * .5f - .2f), .18f, Color.white);
            else if (block.Feature == CityBlockFeature.Warehouse)
                BuildWarehouseDetails(x, z, footprint, height);
        }

        private float BuildingHeight(CityBlockDefinition block)
        {
            int variation = Mathf.Abs(block.X * 31 + block.Z * 17 + seed);
            switch (block.Feature)
            {
                case CityBlockFeature.Casino: return 22f;
                case CityBlockFeature.OfficeTower: return block.X == 2 ? 58f : 43f;
                case CityBlockFeature.Apartment: return 17f + variation % 7;
                case CityBlockFeature.Warehouse: return 8f + variation % 3;
                case CityBlockFeature.Market: return 8f;
                case CityBlockFeature.Port: return 9f;
                case CityBlockFeature.Restaurant: return 7f;
                case CityBlockFeature.IndustrialYard: return 10f;
                default: return 7f + variation % 12;
            }
        }

        private string BuildingName(CityBlockDefinition block)
        {
            if (block.Feature != CityBlockFeature.Building) return block.Feature.ToString();
            int type = Mathf.Abs(block.X * 17 + block.Z * 29 + seed) % 3;
            return block.Zone == ZoneId.CasaMarea ? new[] { "Casa Marea House", "Corner Shop", "Courtyard House" }[type]
                : new[] { "Mixed Use Block", "Apartment Block", "Street Shop" }[type];
        }

        private static Material FacadeFor(CityBlockDefinition block, Material district)
        {
            switch (block.Feature)
            {
                case CityBlockFeature.Casino: return MakeMaterial("Casino Gold", new Color(.66f, .39f, .1f));
                case CityBlockFeature.OfficeTower: return MakeMaterial("Faro Tower Glass", glass);
                case CityBlockFeature.Warehouse: return MakeMaterial("Warehouse Sheet Metal", new Color(.44f, .47f, .45f));
                case CityBlockFeature.Apartment: return MakeMaterial("Apartment Stucco", new Color(.82f, .73f, .60f));
                default: return district;
            }
        }

        private static Material RoofFor(CityBlockDefinition block, Material district)
        {
            return block.Feature == CityBlockFeature.Casino
                ? MakeMaterial("Casino Night", new Color(.08f, .08f, .1f)) : district;
        }

        private void BuildRoofline(CityBlockDefinition block, float x, float z, float width, float height, Material material)
        {
            if (block.Feature == CityBlockFeature.OfficeTower)
            {
                Cube("Tower crown", new Vector3(x, height + 1.1f, z), new Vector3(width * .44f, 2.2f, width * .44f), material, false);
                return;
            }

            Cube("Roof parapet", new Vector3(x, height + .22f, z), new Vector3(width + .6f, .44f, width + .6f), material, false);
            if (block.Zone == ZoneId.CasaMarea && block.Feature == CityBlockFeature.Building)
            {
                Cube("Terracotta roof ridge", new Vector3(x, height + 1.1f, z), new Vector3(width * .8f, .35f, 2.8f),
                    MakeMaterial("Terracotta Roof", new Color(.67f, .27f, .15f)), false);
            }
        }

        private void BuildFacadeDetails(CityBlockDefinition block, float x, float z, float width, float height, bool hasDoor)
        {
            if (Mathf.Abs(block.X * 31 + block.Z * 19 + seed) % 5 == 0)
            {
                var windowMat = MakeMaterial("Sea Glass Windows", glass);
                int floors = Mathf.Min(3, Mathf.FloorToInt(height / 3.8f));
                for (int floor = 0; floor < floors; floor++)
                    for (int side = -1; side <= 1; side += 2)
                        Cube("Facade window", new Vector3(x + side * width * .24f, 2.2f + floor * 3.7f, z - width * .5f - .08f),
                            new Vector3(4.6f, 2.1f, .16f), windowMat, false);
            }

            if (block.Feature == CityBlockFeature.Building &&
                Mathf.Abs(block.X * 23 + block.Z * 37 + seed) % 4 == 0)
            {
                var awning = MakeMaterial("Shop Awning", ZoneAwningColor(block.Zone));
                Cube("Street shop awning", new Vector3(x, 3.4f, z - width * .5f - .8f), new Vector3(13f, .55f, 2.2f), awning, false);
                Cube("Shopfront glass", new Vector3(x, 1.75f, z - width * .5f - .09f), new Vector3(11f, 2.5f, .18f),
                    MakeMaterial("Store Glass", new Color(.16f, .33f, .37f)), false);
                if (!hasDoor)
                    Cube("Shop door", new Vector3(x + 7.4f, 1.25f, z - width * .5f - .16f), new Vector3(1.6f, 2.5f, .22f),
                        MakeMaterial("Shop Door", new Color(.28f, .19f, .14f)), false);
            }
        }

        private void BuildTowerWindows(float x, float z, float width, float height)
        {
            var window = MakeMaterial("Tower Blue Glass", new Color(.18f, .43f, .52f));
            for (int floor = 1; floor < height / 5f; floor++)
                for (int side = -1; side <= 1; side++)
                    Cube("Tower window band", new Vector3(x + side * width * .25f, floor * 5f, z - width * .5f - .1f),
                        new Vector3(width * .16f, 1.3f, .2f), window, false);
        }

        private static Color ZoneAwningColor(ZoneId zone)
        {
            switch (zone)
            {
                case ZoneId.MercadoDelSol: return new Color(.92f, .56f, .12f);
                case ZoneId.MuelleFarol: return new Color(.17f, .43f, .58f);
                case ZoneId.TalleresBrisa: return new Color(.72f, .26f, .17f);
                case ZoneId.CentroDelFaro: return new Color(.16f, .38f, .56f);
                case ZoneId.PaseoArena: return new Color(.70f, .30f, .47f);
                default: return new Color(.24f, .49f, .37f);
            }
        }

        private void BuildPark(float x, float z, float lot, Material green)
        {
            Cube("Park lawn", new Vector3(x, .04f, z), new Vector3(lot, .18f, lot), green, false);
            var path = MakeMaterial("Park Sand Paths", new Color(.79f, .71f, .56f));
            Cube("Park cross path", new Vector3(x, .15f, z), new Vector3(5f, .12f, lot), path, false);
            Cube("Park cross path", new Vector3(x, .16f, z), new Vector3(lot, .12f, 5f), path, false);
            Tree(x - lot * .30f, z - lot * .25f, MakeMaterial("Park Palm Green", palmGreen));
            Tree(x + lot * .30f, z + lot * .25f, MakeMaterial("Park Palm Green", palmGreen));
            Cube("Park bench seat", new Vector3(x + 9f, .75f, z + 11f), new Vector3(4f, .28f, 1.2f),
                MakeMaterial("Bench Wood", new Color(.39f, .25f, .15f)), false);
            Cube("Park kiosk", new Vector3(x - 10f, 2f, z - 8f), new Vector3(7f, 4f, 6f),
                MakeMaterial("Kiosk Cream", cream), true);
            TextSign("PASEO ARENA", new Vector3(x, 5f, z - lot * .5f - 2f), .16f, Color.white);
        }

        private void BuildPlaza(float x, float z, float lot, Material green)
        {
            var stone = MakeMaterial("Faro Civic Stone", new Color(.64f, .68f, .66f));
            Cube("Faro plaza", new Vector3(x, .05f, z), new Vector3(lot, .16f, lot), stone, false);
            Cube("Plaza planter", new Vector3(x, .55f, z), new Vector3(9f, .9f, 9f), green, false);
            Cube("Faro monument base", new Vector3(x, 1.5f, z), new Vector3(2.8f, 3f, 2.8f),
                MakeMaterial("Faro Monument", new Color(.83f, .63f, .27f)), false);
        }

        private void BuildTransitHub(float x, float z, float lot, Material wall, Material roofMaterial)
        {
            Cube("Estacion Sur terminal", new Vector3(x, 4f, z + 9f), new Vector3(lot * .70f, 8f, 25f), wall, true);
            Cube("Bus platform canopy", new Vector3(x, 8.5f, z - 17f), new Vector3(lot * .86f, .65f, 22f), roofMaterial, false);
            for (int side = -1; side <= 1; side += 2)
                Cube("Bus canopy column", new Vector3(x + side * lot * .38f, 4f, z - 17f), new Vector3(.7f, 8f, .7f), wall, false);
            for (int bay = -1; bay <= 1; bay++)
            {
                float bayZ = z - 25f + bay * 12f;
                Cube("Bus bay", new Vector3(x + lot * .27f, .08f, bayZ), new Vector3(12f, .06f, 8f),
                    MakeMaterial("Bus Bay Paint", roadMark), false);
                TextSign("BUS", new Vector3(x + lot * .27f, 2.2f, bayZ - 4f), .12f, Color.white);
            }
            TextSign("ESTACION SUR", new Vector3(x, 10f, z - 4f), .24f, Color.white);
        }

        private void BuildHarborCrane(float x, float z)
        {
            var steel = MakeMaterial("Harbor Crane Steel", new Color(.80f, .31f, .16f));
            Cube("Crane mast", new Vector3(x, 13f, z), new Vector3(1.8f, 26f, 1.8f), steel, false);
            Cube("Crane boom", new Vector3(x + 9f, 25f, z), new Vector3(20f, 1.8f, 1.8f), steel, false);
            Cube("Crane counterweight", new Vector3(x - 5f, 24f, z), new Vector3(5f, 3f, 3.4f), steel, false);
            Cube("Crane hanging cable", new Vector3(x + 14f, 17f, z), new Vector3(.35f, 14f, .35f), steel, false);
            Cube("Crane hook", new Vector3(x + 14f, 9.8f, z), new Vector3(1.6f, 1.1f, 1.6f), steel, false);
            for (int crate = 0; crate < 3; crate++)
                Cube("Harbor cargo container", new Vector3(x - 13f + crate * 11f, 2f, z + 16f), new Vector3(10f, 4f, 7f),
                    MakeMaterial(crate % 2 == 0 ? "Port Container Blue" : "Port Container Rust",
                        crate % 2 == 0 ? new Color(.20f, .37f, .47f) : new Color(.63f, .31f, .19f)), false);
        }

        private void BuildRestaurantTerrace(float x, float z, float lot)
        {
            var timber = MakeMaterial("Terrace Timber", new Color(.39f, .25f, .15f));
            var canopy = MakeMaterial("Terrace Canopy", new Color(.71f, .28f, .39f));
            Cube("Terrace deck", new Vector3(x, .16f, z - lot * .5f - 12f), new Vector3(lot * .76f, .3f, 18f), timber, false);
            Cube("Terrace pergola roof", new Vector3(x, 4.1f, z - lot * .5f - 12f), new Vector3(lot * .78f, .45f, 16f), canopy, false);
            for (int side = -1; side <= 1; side += 2)
                Cube("Pergola post", new Vector3(x + side * lot * .36f, 2f, z - lot * .5f - 12f), new Vector3(.45f, 4f, .45f), timber, false);
            for (int table = -1; table <= 1; table++)
            {
                Cube("Cafe table", new Vector3(x + table * 9f, 1.2f, z - lot * .5f - 12f), new Vector3(2.4f, .3f, 2.4f),
                    MakeMaterial("Cafe Table Stone", cream), false);
                Cube("Cafe umbrella", new Vector3(x + table * 9f, 3.2f, z - lot * .5f - 12f), new Vector3(4.2f, .18f, 4.2f), canopy, false);
            }
        }

        private void BuildIndustrialDetails(float x, float z, float lot)
        {
            var steel = MakeMaterial("Industrial Pipe", new Color(.34f, .38f, .38f));
            Cube("Workshop rollup door", new Vector3(x, 2.3f, z - lot * .5f - .13f), new Vector3(18f, 4.6f, .28f), steel, false);
            Cube("Workshop vent stack", new Vector3(x + lot * .27f, 13f, z + lot * .25f), new Vector3(3.2f, 20f, 3.2f),
                MakeMaterial("Workshop Brick", new Color(.54f, .27f, .19f)), false);
            Cube("Workshop stack cap", new Vector3(x + lot * .27f, 23f, z + lot * .25f), new Vector3(5.2f, 1f, 5.2f), steel, false);
            for (int pipe = 0; pipe < 3; pipe++)
                Cube("Factory pipe", new Vector3(x - lot * .25f + pipe * 4f, 6f, z - lot * .25f), new Vector3(1.1f, 12f, 1.1f), steel, false);
        }

        private void BuildWarehouseDetails(float x, float z, float width, float height)
        {
            Cube("Warehouse roller door", new Vector3(x, 2.6f, z - width * .5f - .12f), new Vector3(13f, 5.2f, .24f),
                MakeMaterial("Warehouse Door", new Color(.27f, .31f, .32f)), false);
            Cube("Warehouse roof vent", new Vector3(x + width * .28f, height + 1f, z), new Vector3(5f, 1.8f, 5f),
                MakeMaterial("Warehouse Vent", new Color(.32f, .35f, .35f)), false);
        }

        private void BuildCrosswalks(Material paint)
        {
            float[] intersections = { -42f, 42f };
            foreach (float x in intersections)
                foreach (float z in intersections)
                    for (int stripe = -4; stripe <= 4; stripe++)
                    {
                        Cube("Crosswalk stripe", new Vector3(x + stripe * 1.9f, .06f, z - 14f), new Vector3(1f, .05f, 5f), paint, false);
                        Cube("Crosswalk stripe", new Vector3(x - 14f, .06f, z + stripe * 1.9f), new Vector3(5f, .05f, 1f), paint, false);
                    }
        }

        private void BuildStreetLife(float pitch)
        {
            var pole = MakeMaterial("Streetlight Metal", new Color(.19f, .24f, .25f));
            var lamp = MakeMaterial("Streetlight Amber", new Color(1f, .72f, .34f));
            for (int block = -8; block <= 8; block += 2)
            {
                float along = block * pitch;
                foreach (float arterial in new[] { -42f, 42f })
                {
                    for (int side = -1; side <= 1; side += 2)
                    {
                        float sidewalkX = arterial + side * 13.5f;
                        Cube("Boulevard lamp pole", new Vector3(sidewalkX, 4f, along), new Vector3(.35f, 8f, .35f), pole, false);
                        Cube("Boulevard lamp arm", new Vector3(sidewalkX - side * 1.4f, 7.5f, along), new Vector3(3.2f, .22f, .22f), pole, false);
                        Cube("Boulevard lamp", new Vector3(sidewalkX - side * 2.7f, 7.1f, along), new Vector3(1.2f, .35f, .7f), lamp, false);
                    }
                }
                foreach (float arterial in new[] { -42f, 42f })
                {
                    for (int side = -1; side <= 1; side += 2)
                    {
                        float sidewalkZ = arterial + side * 13.5f;
                        Cube("Boulevard tree trunk", new Vector3(along, 2.2f, sidewalkZ), new Vector3(.65f, 4.4f, .65f),
                            MakeMaterial("Palm Trunk", new Color(.42f, .29f, .18f)), false);
                        GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        crown.name = "Boulevard palm crown";
                        crown.transform.SetParent(transform, false);
                        crown.transform.localPosition = new Vector3(along, 5.4f, sidewalkZ);
                        crown.transform.localScale = new Vector3(4.8f, 2.1f, 4.8f);
                        crown.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Boulevard Palm Fronds", palmGreen);
                        Object.Destroy(crown.GetComponent<Collider>());
                    }
                }
            }
        }

        private void BuildDistrictGateways(float pitch)
        {
            var signs = new (ZoneId zone, float x, float z, string label)[]
            {
                (ZoneId.CasaMarea, -20f, -28f, "CASA MAREA"),
                (ZoneId.MercadoDelSol, -6 * pitch, pitch, "MERCADO DEL SOL"),
                (ZoneId.MuelleFarol, 0f, 9 * pitch, "MUELLE FAROL"),
                (ZoneId.CentroDelFaro, 3 * pitch, -4 * pitch, "CENTRO DEL FARO"),
                (ZoneId.TalleresBrisa, 8 * pitch, -5 * pitch, "TALLERES BRISA"),
                (ZoneId.PaseoArena, -2 * pitch, 6 * pitch, "PASEO ARENA"),
                (ZoneId.ColinaCobalto, 6 * pitch, 2 * pitch, "COLINA COBALTO"),
                (ZoneId.EstacionSur, 0f, -9 * pitch, "ESTACION SUR")
            };
            foreach (var sign in signs)
            {
                var board = MakeMaterial("District Signboard", new Color(.12f, .22f, .25f));
                Cube(sign.label + " wayfinding board", new Vector3(sign.x + 16f, 3.4f, sign.z - 25f), new Vector3(10f, 3.2f, .55f), board, false);
                TextSign(sign.label, new Vector3(sign.x + 16f, 3.4f, sign.z - 25.35f), .10f, Color.white);
            }
        }

        private void BuildTraffic(float pitch)
        {
            for (int i = 0; i < 4; i++)
            {
                float progress = i * 190f - 285f;
                BuildCar(42f, -5f, progress, true, true, pitch);
                BuildCar(-42f, 5f, -progress, true, false, pitch);
                BuildCar(-42f, -5f, progress, false, true, pitch);
                BuildCar(42f, 5f, -progress, false, false, pitch);
            }
        }

        private void BuildCar(float fixedAxis, float laneOffset, float progress, bool movesOnX, bool forward, float pitch)
        {
            var car = new GameObject("Puerto Alba Traffic");
            car.transform.SetParent(transform, false);
            car.transform.localPosition = movesOnX ? new Vector3(progress, .15f, fixedAxis + laneOffset) : new Vector3(fixedAxis + laneOffset, .15f, progress);
            float yaw = movesOnX ? (forward ? 90f : -90f) : (forward ? 0f : 180f);
            car.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var paint = MakeMaterial("Traffic Paint " + (int)(Mathf.Abs(progress) % 4),
                new[] { new Color(.72f, .18f, .14f), new Color(.13f, .37f, .56f), new Color(.78f, .72f, .55f), new Color(.22f, .28f, .27f) }[(int)(Mathf.Abs(progress) % 4)]);
            TrafficPart(car.transform, "Car body", new Vector3(0f, .75f, 0f), new Vector3(2.15f, 1.05f, 4.6f), paint);
            TrafficPart(car.transform, "Car cabin", new Vector3(0f, 1.45f, -.15f), new Vector3(1.8f, .85f, 2.6f),
                MakeMaterial("Car Glass", glass));
            var tire = MakeMaterial("Tire Rubber", new Color(.08f, .09f, .1f));
            foreach (float z in new[] { -1.45f, 1.45f })
                foreach (float x in new[] { -1.05f, 1.05f })
                    TrafficPart(car.transform, "Car wheel", new Vector3(x, .38f, z), new Vector3(.34f, .78f, .75f), tire);
            TrafficPart(car.transform, "Tail lights", new Vector3(0f, .86f, -2.32f), new Vector3(1.2f, .32f, .12f),
                MakeMaterial("Traffic Tail Lights", new Color(.82f, .09f, .05f)));
            TrafficPart(car.transform, "Headlights", new Vector3(0f, .86f, 2.32f), new Vector3(1.2f, .32f, .12f),
                MakeMaterial("Traffic Headlights", new Color(1f, .84f, .53f)));
            car.AddComponent<CityTrafficVehicle>().Configure(movesOnX, forward ? 1f : -1f, pitch * 4f, 12f + Mathf.Abs(progress % 5f));
        }

        private static void TrafficPart(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Object.Destroy(part.GetComponent<Collider>());
        }

        private void BuildGasStation(float x, float z, float lot, Material wall, Material roofMaterial)
        {
            float frontage = lot * .6f;
            Cube("Gas station shop", new Vector3(x, 2, z + lot * .25f), new Vector3(frontage, 4, lot * .38f), wall, true, true);
            TextSign("GAS", new Vector3(x, 4.5f, z + lot * .25f - lot * .19f - .18f), .3f, Color.white);
            Cube("Fuel canopy", new Vector3(x, 4f, z - lot * .2f), new Vector3(lot * .82f, .55f, lot * .48f), roofMaterial, false);
            for (int side = -1; side <= 1; side += 2)
                for (int row = -1; row <= 1; row += 2)
                    Cube("Canopy column", new Vector3(x + side * lot * .32f, 1.9f, z - lot * .2f + row * lot * .17f), new Vector3(.35f, 3.8f, .35f), wall, false);
            for (int pump = -1; pump <= 1; pump++)
                Cube("Fuel pump", new Vector3(x + pump * 5f, .8f, z - lot * .2f), new Vector3(1.1f, 1.6f, 1.1f), MakeMaterial("Pump Red", new Color(.72f, .15f, .11f)), true);
        }

        private void Tree(float x, float z, Material leaves)
        {
            Cube("Tree trunk", new Vector3(x, 1.4f, z), new Vector3(.5f, 2.8f, .5f), MakeMaterial("Tree Wood", new Color(.35f, .23f, .13f)), false);
            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = "Tree crown"; crown.transform.SetParent(transform, false);
            crown.transform.localPosition = new Vector3(x, 3.2f, z); crown.transform.localScale = new Vector3(3, 2.5f, 3);
            crown.GetComponent<Renderer>().sharedMaterial = leaves;
            Object.Destroy(crown.GetComponent<Collider>());
        }

        private void TextSign(string value, Vector3 position, float characterSize, Color color)
        {
            var sign = new GameObject(value + " sign");
            sign.transform.SetParent(transform, false);
            sign.transform.localPosition = position;
            sign.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var text = sign.AddComponent<TextMesh>();
            text.text = value;
            text.fontSize = 64;
            text.characterSize = characterSize;
            text.alignment = TextAlignment.Center;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = color;
        }

        private static Material WallForZone(ZoneId zone)
        {
            switch (zone)
            {
                case ZoneId.MercadoDelSol: return MakeMaterial("Wall Market", new Color(.83f, .58f, .28f));
                case ZoneId.MuelleFarol: return MakeMaterial("Wall Port", new Color(.44f, .57f, .61f));
                case ZoneId.TalleresBrisa: return MakeMaterial("Wall Industrial", new Color(.43f, .46f, .45f));
                case ZoneId.PaseoArena: return MakeMaterial("Wall Park", new Color(.62f, .70f, .56f));
                case ZoneId.CentroDelFaro: return MakeMaterial("Wall Finance", new Color(.36f, .49f, .62f));
                case ZoneId.ColinaCobalto: return MakeMaterial("Wall Cobalto", new Color(.48f, .58f, .74f));
                case ZoneId.EstacionSur: return MakeMaterial("Wall Station", new Color(.75f, .69f, .50f));
                default: return MakeMaterial("Wall Residential", stucco);
            }
        }

        private static Material RoofForZone(ZoneId zone)
        {
            switch (zone)
            {
                case ZoneId.MercadoDelSol: return MakeMaterial("Roof Market", new Color(.73f, .42f, .18f));
                case ZoneId.MuelleFarol: return MakeMaterial("Roof Port", new Color(.29f, .37f, .40f));
                case ZoneId.TalleresBrisa: return MakeMaterial("Roof Industrial", new Color(.27f, .29f, .29f));
                case ZoneId.PaseoArena: return MakeMaterial("Roof Park", new Color(.34f, .47f, .32f));
                case ZoneId.CentroDelFaro: return MakeMaterial("Roof Finance", new Color(.21f, .29f, .40f));
                case ZoneId.ColinaCobalto: return MakeMaterial("Roof Cobalto", new Color(.29f, .35f, .48f));
                case ZoneId.EstacionSur: return MakeMaterial("Roof Station", new Color(.39f, .35f, .27f));
                default: return MakeMaterial("Roof Residential", roof);
            }
        }

        private void Cube(string name, Vector3 position, Vector3 size, Material material, bool collider, bool door = false)
        {
            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = name; item.transform.SetParent(transform, false);
            item.transform.localPosition = position; item.transform.localScale = size;
            item.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.Destroy(item.GetComponent<Collider>());
            if (door)
            {
                var doorTarget = GameObject.CreatePrimitive(PrimitiveType.Cube);
                doorTarget.name = "Door - E: knock";
                doorTarget.transform.SetParent(transform, false);
                doorTarget.transform.localPosition = new Vector3(position.x, 1.15f, position.z - size.z * .5f - .14f);
                doorTarget.transform.localScale = new Vector3(1.4f, 2.3f, .16f);
                doorTarget.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Front Door", new Color(.30f, .16f, .10f));
                doorTarget.AddComponent<DoorVisitorSpawner>();
            }
        }

        private static Material MakeMaterial(string name, Color color)
        {
            if (materials.TryGetValue(name, out var cached)) return cached;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.name = name; material.color = color; materials.Add(name, material); return material;
        }
    }

    public sealed class CityTrafficVehicle : MonoBehaviour
    {
        private bool movesOnX;
        private float direction;
        private float bound;
        private float speed;

        public void Configure(bool alongX, float travelDirection, float travelBound, float metersPerSecond)
        {
            movesOnX = alongX;
            direction = travelDirection;
            bound = travelBound;
            speed = metersPerSecond;
        }

        private void Update()
        {
            Vector3 position = transform.localPosition;
            float along = movesOnX ? position.x : position.z;
            along += direction * speed * Time.deltaTime;
            if (along > bound) along = -bound;
            else if (along < -bound) along = bound;
            if (movesOnX) position.x = along;
            else position.z = along;
            transform.localPosition = position;
        }
    }

    public sealed class DoorVisitorSpawner : MonoBehaviour, IInteractable
    {
        private GameObject visitor;
        public string Prompt => visitor == null ? "E — Tocar puerta" : "E — Despedir visitante";
        public void Interact()
        {
            if (visitor != null) { Object.Destroy(visitor); visitor = null; return; }
            visitor = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visitor.name = "Visitante de la casa";
            visitor.transform.position = transform.position + transform.forward * -1.2f + Vector3.up * .9f;
            var renderer = visitor.GetComponent<Renderer>();
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", new Color(.2f, .55f, .72f));
            renderer.SetPropertyBlock(block);
        }
    }
}
