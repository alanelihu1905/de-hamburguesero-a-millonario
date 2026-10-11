using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tycoon3D.Restaurant
{
    /// <summary>
    /// Más variedad en la calle sin cambiar la escena ni la geometría: la cajera viste otra variante de la paleta, y salen dos
    /// paseantes más (copias de otros dos modelos con su propia ropa) que cruzan la calle en sentido contrario. Cada variante es
    /// la textura de paleta del proyecto con algunas celdas de ropa cambiadas por otros colores de la misma paleta
    /// (<see cref="PaletteVariants"/>): no hay colores nuevos, ni mallas nuevas, ni materiales extra más allá de uno por variante.
    /// Se crea sola desde el perfil.
    /// </summary>
    public sealed class CustomerVariety : MonoBehaviour
    {
        public const int ExtraStrollers = 2;

        public sealed class Look
        {
            public AnimatedCustomer Customer;
            public int Variant;
        }

        private static readonly Dictionary<int, Texture2D> Textures = new Dictionary<int, Texture2D>();
        private readonly Dictionary<(Material, int), Material> materials = new Dictionary<(Material, int), Material>();

        public List<Look> Looks { get; } = new List<Look>();
        public int Spawned { get; private set; }

        private IEnumerator Start()
        {
            yield return null;                 // los clientes ya corrieron su Start (casa, animador, flujo de pedidos)
            Apply();
        }

        public void Apply()
        {
            Looks.Clear();
            var customers = AnimatedCustomer.All.Where(c => c != null && c.Animator != null).ToList();
            foreach (var cashier in customers.Where(c => c.Mode == CustomerMode.Cashier)) Recolor(cashier, 1);

            // dos paseantes más, de otros modelos y con otra ropa, cruzando la calle en sentido contrario al del paseante original
            var pescador = customers.FirstOrDefault(c => c.Mode == CustomerMode.Queue && Model(c) == "Pescador");
            var vecina = customers.FirstOrDefault(c => c.Mode == CustomerMode.Queue && Model(c) == "Vecina");
            if (pescador != null) Spawn(pescador, 2, new[] { new Vector3(11.5f, 0f, -6.1f), new Vector3(-7.5f, 0f, -6.1f) }, 7.3f);
            if (vecina != null) Spawn(vecina, 3, new[] { new Vector3(2.5f, 0f, -8.4f), new Vector3(15f, 0f, -8.4f) }, 8.9f);
        }

        public static string Model(AnimatedCustomer customer)
        {
            var controller = customer.Animator != null ? customer.Animator.runtimeAnimatorController : null;
            string name = controller != null ? controller.name : customer.name;
            foreach (var who in new[] { "Pescador", "Vecina", "Joven" }) if (name.Contains(who)) return who;
            return string.Empty;
        }

        private void Spawn(AnimatedCustomer source, int variant, Vector3[] path, float seed)
        {
            var copy = Instantiate(source.gameObject, source.transform.parent);
            copy.name = source.name + " (paseante " + (Spawned + 1) + ")";
            var bubble = copy.transform.Find("Cartel del pedido");
            if (bubble != null) Destroy(bubble.gameObject);
            copy.transform.position = path[0];
            copy.transform.rotation = Quaternion.LookRotation(path[1] - path[0]);
            var customer = copy.GetComponent<AnimatedCustomer>();
            customer.Configure(CustomerMode.Stroll, copy.GetComponentInChildren<Animator>(), path, seed);
            Recolor(customer, variant);
            Spawned++;
        }

        private void Recolor(AnimatedCustomer customer, int variant)
        {
            if (variant <= 0 || variant >= PaletteVariants.All.Length) return;
            foreach (var renderer in customer.GetComponentsInChildren<Renderer>(true))
            {
                var shared = renderer.sharedMaterials;
                for (int i = 0; i < shared.Length; i++) shared[i] = VariantOf(shared[i], variant);
                renderer.sharedMaterials = shared;
            }
            Looks.RemoveAll(l => l.Customer == customer);
            Looks.Add(new Look { Customer = customer, Variant = variant });
        }

        private Material VariantOf(Material source, int variant)
        {
            if (source == null || !source.HasProperty("_BaseMap")) return source;
            if (materials.TryGetValue((source, variant), out var cached) && cached != null) return cached;
            var copy = new Material(source) { name = source.name + " - " + PaletteVariants.All[variant].Name };
            copy.SetTexture("_BaseMap", TextureOf(variant));
            materials[(source, variant)] = copy;
            return copy;
        }

        /// <summary>La textura de paleta de la variante, armada con los mismos colores del proyecto (nunca se lee de disco).</summary>
        public static Texture2D TextureOf(int variant)
        {
            if (Textures.TryGetValue(variant, out var cached) && cached != null) return cached;
            var texture = new Texture2D(PaletteVariants.Width, PaletteVariants.Height, TextureFormat.RGBA32, false, false)
            {
                name = "Paleta - " + PaletteVariants.All[variant].Name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.LoadRawTextureData(PaletteVariants.BuildPixels(variant));
            texture.Apply(false, false);          // queda legible: son 32 KB y así las pruebas pueden leer los colores
            Textures[variant] = texture;
            return texture;
        }
    }
}
