using Tycoon3D.Interaction;
using Tycoon3D.City;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon3D.World
{
    /// <summary>Cheap primitive city pass. Replace visuals with authored assets without changing layout data.</summary>
    public sealed class CityGridBlockout : MonoBehaviour
    {
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private int seed = 42017;
        private static readonly Color asphalt = new Color(.20f, .23f, .23f);
        private static readonly Color sidewalk = new Color(.68f, .62f, .52f);
        private static readonly Color stucco = new Color(.85f, .75f, .61f);
        private static readonly Color roof = new Color(.52f, .28f, .19f);
        private static readonly Color grass = new Color(.24f, .42f, .25f);
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
            float pitch = CityGridLayout.BlockPitchMeters;
            float road = CityGridLayout.RoadWidthMeters;
            float extent = CityGridLayout.SideLength * pitch;
            Cube("City Ground", new Vector3(0, -.35f, 0), new Vector3(extent + road, .6f, extent + road), greenMat, true);

            for (int i = -CityGridLayout.BlocksFromCenter; i <= CityGridLayout.BlocksFromCenter + 1; i++)
            {
                float edge = (i - .5f) * pitch;
                Cube("Street X", new Vector3(0, -.015f, edge), new Vector3(extent, .08f, road), roadMat, false);
                Cube("Street Z", new Vector3(edge, -.014f, 0), new Vector3(road, .08f, extent), roadMat, false);
                float walkOffset = road * .5f + 1.5f;
                Cube("Sidewalk X north", new Vector3(0, .02f, edge + walkOffset), new Vector3(extent, .16f, 3f), sidewalkMat, false);
                Cube("Sidewalk X south", new Vector3(0, .02f, edge - walkOffset), new Vector3(extent, .16f, 3f), sidewalkMat, false);
                Cube("Sidewalk Z east", new Vector3(edge + walkOffset, .02f, 0), new Vector3(3f, .16f, extent), sidewalkMat, false);
                Cube("Sidewalk Z west", new Vector3(edge - walkOffset, .02f, 0), new Vector3(3f, .16f, extent), sidewalkMat, false);
            }

            foreach (var block in layout.Blocks)
                BuildBlock(block, pitch, road, WallForZone(block.Zone), RoofForZone(block.Zone), greenMat);
            Debug.Log($"TYCOON_CITY_GRID_BUILT blocks={layout.Blocks.Count} diameter={layout.DiameterMeters:0}m seed={seed}");
        }

        private void BuildBlock(CityBlockDefinition block, float pitch, float road,
            Material wall, Material roofMaterial, Material green)
        {
            float x = block.X * pitch, z = block.Z * pitch;
            float lot = pitch - road - 12f;
            if (block.Feature == CityBlockFeature.Park)
            {
                Cube("Park", new Vector3(x, .06f, z), new Vector3(lot, .2f, lot), green, false);
                Tree(x - lot * .25f, z - lot * .2f, green);
                Tree(x + lot * .22f, z + lot * .2f, green);
                return;
            }

            if (block.Feature == CityBlockFeature.GasStation)
            {
                BuildGasStation(x, z, lot, wall, roofMaterial);
                return;
            }

            float height = block.Feature switch
            {
                CityBlockFeature.Casino => 22f,
                CityBlockFeature.Market => 8f,
                CityBlockFeature.Port => 7f,
                CityBlockFeature.Restaurant => 6f,
                CityBlockFeature.IndustrialYard => 9f,
                _ => 4.5f + (Mathf.Abs(block.X * 31 + block.Z * 17 + seed) % 3) * 1.3f
            };
            Material facade = block.Feature == CityBlockFeature.Casino ? MakeMaterial("Casino Gold", new Color(.66f, .39f, .1f)) : wall;
            Material top = block.Feature == CityBlockFeature.Casino ? MakeMaterial("Casino Night", new Color(.08f, .08f, .1f)) : roofMaterial;
            Cube(block.Feature.ToString(), new Vector3(x, height * .5f, z), new Vector3(lot, height, lot), facade, true, block.HasNpcDoor);
            Cube("Flat roof", new Vector3(x, height + .16f, z), new Vector3(lot + .35f, .32f, lot + .35f), top, false);

            if (block.Feature == CityBlockFeature.Casino)
            {
                Cube("Casino entrance canopy", new Vector3(x, 4.2f, z - lot * .5f - 2f), new Vector3(15, .6f, 4), top, false);
                Cube("Casino lit sign", new Vector3(x, 7.5f, z - lot * .5f - 2.1f), new Vector3(12, 1.2f, .35f), MakeMaterial("Casino Marquee", new Color(1f, .65f, .1f)), false);
                TextSign("CASINO FOX 24H", new Vector3(x, 7.5f, z - lot * .5f - 2.32f), .20f, Color.white);
            }
            else if (block.Feature == CityBlockFeature.Market)
                TextSign("MERCADO DEL SOL", new Vector3(x, height + 1f, z - lot * .5f - .2f), .16f, Color.white);
            else if (block.Feature == CityBlockFeature.Port)
                TextSign("MUELLE FAROL", new Vector3(x, height + 1f, z - lot * .5f - .2f), .16f, Color.white);
            else if (block.Feature == CityBlockFeature.Restaurant)
                TextSign("LOS SABORES", new Vector3(x, height + 1f, z - lot * .5f - .2f), .16f, Color.white);
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
            renderer.material.color = new Color(.2f, .55f, .72f);
        }
    }
}
