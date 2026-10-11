using System.Collections.Generic;
using Tycoon3D.Restaurant;
using UnityEngine;

namespace Tycoon3D.Casino
{
    /// <summary>
    /// Lo que se ve de la cajera: un rótulo con su horario y si está en turno o en descanso (la cajera se oculta fuera de turno y
    /// vuelve a su hora), y las fichas sobre el mostrador cuando se cambian pesos por fichas (se apilan de diez en diez) o se
    /// canjean (las recoge). Se crea solo desde la ventanilla, sin tocar la escena. No decide nada de las reglas: el horario sale
    /// de <see cref="CashierSchedule"/> y el casino sigue abierto siempre.
    /// </summary>
    public sealed class CashierShiftView : MonoBehaviour
    {
        private const int MaxChipsShown = 40;
        private const int ChipsPerStack = 10;
        private const float ChipRadius = .06f, ChipThickness = .009f;

        private sealed class Chip
        {
            public Transform Body;
            public Vector3 Rest;
            public float Delay;
            public float Life;
        }

        private static Material[] chipMaterials;

        private CashierDesk desk;
        private CasinoService service;
        private TextMesh sign;
        private Renderer[] cashierRenderers;
        private readonly List<Chip> chips = new List<Chip>();
        private Transform chipRoot;
        private float nextRefresh;
        private bool? lastOnDuty;

        /// <summary>Fichas dibujadas ahora sobre el mostrador (pruebas).</summary>
        public int ChipsOnCounter => chips.Count;
        public bool CashierVisible { get; private set; } = true;
        public string SignText => sign != null ? sign.text : string.Empty;

        public void Bind(CashierDesk cashierDesk, CasinoService casino)
        {
            desk = cashierDesk;
            service = casino;
            if (service != null) service.ChipsExchanged += OnChipsExchanged;
            BuildSign();
            Refresh();
        }

        private void OnDestroy()
        {
            if (service != null) service.ChipsExchanged -= OnChipsExchanged;
        }

        private void BuildSign()
        {
            // se clona el rótulo «CAJERA» de la escena para heredar su fuente, su material y su escala
            var template = GameObject.Find("Texto CAJERA");
            if (template == null) return;
            var copy = Instantiate(template, template.transform.parent);
            copy.name = "Texto turno de la cajera";
            copy.transform.position = template.transform.position + new Vector3(0f, -.3f, 0f);
            copy.transform.rotation = template.transform.rotation;
            sign = copy.GetComponent<TextMesh>();
            if (sign != null) sign.characterSize *= .55f;
        }

        private float Hour
        {
            get
            {
                var cycle = Tycoon3D.World.DayNightCycle.Current;
                return cycle != null ? cycle.Clock.Hour : 12f;
            }
        }

        public void Refresh()
        {
            float hour = Hour;
            bool onDuty = CashierSchedule.OnDuty(hour);
            if (sign != null)
            {
                sign.text = CashierSchedule.ShiftText + "\n" + CashierSchedule.StatusText(hour);
                sign.color = onDuty ? new Color32(244, 201, 93, 255) : new Color32(228, 87, 46, 255);
            }
            if (lastOnDuty != onDuty)
            {
                lastOnDuty = onDuty;
                SetCashierVisible(onDuty);
            }
        }

        private void SetCashierVisible(bool visible)
        {
            CashierVisible = visible;
            var npc = desk != null ? desk.Cashier : null;
            if (npc == null) return;
            if (cashierRenderers == null) cashierRenderers = npc.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in cashierRenderers) if (renderer != null) renderer.enabled = visible;
        }

        // ------------------------------------------------------------------ fichas sobre el mostrador
        private static Material[] ChipMaterials()
        {
            if (chipMaterials != null && chipMaterials[0] != null) return chipMaterials;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Color[] colors = { new Color32(244, 201, 93, 255), new Color32(228, 87, 46, 255), new Color32(247, 238, 221, 255) };
            chipMaterials = new Material[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                chipMaterials[i] = new Material(shader) { name = "Ficha runtime " + i, color = colors[i] };
                if (chipMaterials[i].HasProperty("_BaseColor")) chipMaterials[i].SetColor("_BaseColor", colors[i]);
                if (chipMaterials[i].HasProperty("_Smoothness")) chipMaterials[i].SetFloat("_Smoothness", .55f);
            }
            return chipMaterials;
        }

        private void OnChipsExchanged(int count, bool bought)
        {
            if (desk == null) return;
            if (chipRoot != null) Destroy(chipRoot.gameObject);
            chips.Clear();
            chipRoot = new GameObject("Fichas sobre el mostrador").transform;
            chipRoot.SetParent(transform, false);
            // el mostrador está del lado del jugador (oeste); la tapa queda unos 6 cm sobre el centro de la ventanilla
            Vector3 origin = desk.transform.position + new Vector3(-.34f, .062f, -.55f);
            var materials = ChipMaterials();
            int shown = Mathf.Min(count, MaxChipsShown);
            for (int i = 0; i < shown; i++)
            {
                int stack = i / ChipsPerStack, level = i % ChipsPerStack;
                var chip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(chip.GetComponent<Collider>());
                chip.name = "Ficha " + (i + 1);
                chip.transform.SetParent(chipRoot, false);
                chip.transform.localScale = new Vector3(ChipRadius * 2f, ChipThickness / 2f, ChipRadius * 2f);
                chip.GetComponent<Renderer>().sharedMaterial = materials[stack % materials.Length];
                Vector3 rest = origin + new Vector3((stack % 2) * .16f, level * ChipThickness * 1.05f, (stack / 2) * .17f);
                chips.Add(new Chip
                {
                    Body = chip.transform,
                    Rest = rest,
                    Delay = i * .035f,
                    Life = bought ? 6f : 1.8f      // al canjear, la cajera las recoge enseguida
                });
                chip.transform.position = rest + Vector3.up * .25f;
                chip.SetActive(false);
            }
            elapsed = 0f;
        }

        private float elapsed;

        private void Update()
        {
            if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + .5f; Refresh(); }
            if (chips.Count == 0) return;
            elapsed += Time.deltaTime;
            bool anyLeft = false;
            foreach (var chip in chips)
            {
                if (chip.Body == null) continue;
                float t = elapsed - chip.Delay;
                if (t < 0f) { anyLeft = true; continue; }
                if (t > chip.Life) { chip.Body.gameObject.SetActive(false); continue; }
                anyLeft = true;
                chip.Body.gameObject.SetActive(true);
                float fall = Mathf.Clamp01(t / .2f);
                chip.Body.position = Vector3.Lerp(chip.Rest + Vector3.up * .25f, chip.Rest, fall * fall);
                float fade = Mathf.Clamp01((chip.Life - t) / .4f);
                chip.Body.localScale = new Vector3(ChipRadius * 2f * fade, ChipThickness / 2f, ChipRadius * 2f * fade);
            }
            if (!anyLeft)
            {
                if (chipRoot != null) Destroy(chipRoot.gameObject);
                chips.Clear();
            }
        }
    }
}
