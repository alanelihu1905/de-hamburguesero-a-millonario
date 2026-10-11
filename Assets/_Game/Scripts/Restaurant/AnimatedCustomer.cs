using System.Collections.Generic;
using UnityEngine;

namespace Tycoon3D.Restaurant
{
    public enum CustomerMode { Queue, Stroll, Cashier }

    /// <summary>
    /// Cliente animado (Idle, Caminar, Pedir). No toca la economía: las ventas siguen en BurgerLoop.
    /// Queue: espera en la fila, mira al jugador si está cerca y PIDE una hamburguesa de verdad (CustomerOrderFlow): levanta la
    /// mano, muestra un cartel con el pedido y su paciencia, y cuando el jugador completa la venta paga (CustomerOrders), se va
    /// caminando a la calle, desaparece un rato y vuelve con otro pedido. Si se cansa de esperar se va sin pagar.
    /// Stroll: camina entre puntos con el clip Caminar a su velocidad real (sin patinar) y se detiene a descansar.
    /// Cashier: atiende detrás del mostrador del casino.
    /// </summary>
    public sealed class AnimatedCustomer : MonoBehaviour
    {
        public const float WalkSpeed = 1.42f;      // m/s del ciclo Caminar medido en Blender
        private const float StreetZ = -7.2f;       // la calle por donde pasea el cliente de ambiente y por donde se van los de la fila

        public static readonly List<AnimatedCustomer> All = new List<AnimatedCustomer>();

        [SerializeField] private CustomerMode mode;
        [SerializeField] private Animator animator;
        [SerializeField] private Vector3[] waypoints;
        [SerializeField] private float seed;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int OrderHash = Animator.StringToHash("Pedir");

        private int target;
        private float waitUntil;
        private float nextOrderAt;
        private Transform player;
        private Vector3 home, leaveGoal;
        private Quaternion homeRotation;
        private Renderer[] renderers;
        private TextMesh bubble;
        private BurgerLoop burger;

        public CustomerMode Mode => mode;
        public Animator Animator => animator;
        public bool IsWalking { get; private set; }
        /// <summary>Ciclo de pedido (solo los de la fila).</summary>
        public CustomerOrderFlow Flow { get; private set; }
        public bool Visible { get; private set; } = true;
        public Vector3 Home => home;
        public string BubbleText => bubble != null ? bubble.text : string.Empty;

        public void Configure(CustomerMode customerMode, Animator anim, Vector3[] path, float randomSeed)
        {
            mode = customerMode;
            animator = anim;
            waypoints = path;
            seed = randomSeed;
        }

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        private void Start()
        {
            nextOrderAt = Time.time + 2f + Mathf.Abs(Mathf.Sin(seed * 12.9898f)) * 6f;
            waitUntil = Time.time + Mathf.Abs(Mathf.Sin(seed * 78.233f)) * 3f;
            if (waypoints != null && waypoints.Length > 1) target = 1;
            var cam = Camera.main;
            if (cam != null) player = cam.transform;
            home = transform.position;
            homeRotation = transform.rotation;
            leaveGoal = new Vector3(home.x, home.y, StreetZ);
            if (mode == CustomerMode.Queue)
            {
                Flow = new CustomerOrderFlow(seed);
                Flow.OrderStarted += OnOrderStarted;
                renderers = GetComponentsInChildren<Renderer>(true);
                burger = FindFirstObjectByType<BurgerLoop>();
                BuildBubble();
            }
        }

        private void OnDestroy()
        {
            if (Flow != null) Flow.OrderStarted -= OnOrderStarted;
        }

        private void Update()
        {
            if (animator == null) return;
            if (player == null && Camera.main != null) player = Camera.main.transform;
            if (mode == CustomerMode.Queue) UpdateOrders();
            else if (mode == CustomerMode.Cashier) UpdateQueue();
            else UpdateStroll();
        }

        /// <summary>Gesto de atención (el clip Pedir): la cajera lo usa al atender al jugador.</summary>
        public void Gesture()
        {
            if (animator != null) animator.SetTrigger(OrderHash);
            LastGestureTime = Time.time;
        }

        public float LastGestureTime { get; private set; } = -100f;

        /// <summary>Le sirven la hamburguesa: paga (solo si tenía un pedido) y se va. El dinero lo movió BurgerLoop, no el cliente.</summary>
        public bool Pay()
        {
            if (Flow == null || !Flow.TryPay()) return false;
            Gesture();
            return true;
        }

        // ------------------------------------------------------------------ cliente de la fila con pedido
        private void OnOrderStarted()
        {
            Gesture();
            nextOrderAt = Time.time + 7f + Mathf.Abs(Mathf.Sin((seed + Time.time) * 3.7f)) * 5f;
        }

        private void UpdateOrders()
        {
            bool arrived = false;
            switch (Flow.State)
            {
                case OrderState.Idle:
                case OrderState.Ordering:
                    if (!Visible) SetVisible(true);
                    FacePlayerAtHome();
                    if (Flow.State == OrderState.Ordering && Time.time >= nextOrderAt)
                    {
                        animator.SetTrigger(OrderHash);                 // vuelve a levantar la mano mientras espera
                        nextOrderAt = Time.time + 7f + Mathf.Abs(Mathf.Sin((seed + Time.time) * 3.7f)) * 5f;
                    }
                    break;
                case OrderState.Leaving:
                    arrived = WalkTo(leaveGoal);
                    break;
                case OrderState.Away:
                    if (Visible) SetVisible(false);
                    animator.SetFloat(SpeedHash, 0f);
                    IsWalking = false;
                    break;
                case OrderState.Returning:
                    if (!Visible) { transform.position = leaveGoal; SetVisible(true); }
                    arrived = WalkTo(home);
                    if (arrived) { transform.position = home; transform.rotation = homeRotation; }
                    break;
            }
            Flow.Update(Time.deltaTime, arrived);
            UpdateBubble();
        }

        private void FacePlayerAtHome()
        {
            animator.SetFloat(SpeedHash, 0f);
            IsWalking = false;
            if (Vector3.SqrMagnitude(transform.position - home) > .0004f) transform.position = home;
            if (player == null) return;
            Vector3 d = player.position - transform.position;
            d.y = 0;
            if (d.sqrMagnitude < 25f && d.sqrMagnitude > .01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 120f * Time.deltaTime);
        }

        /// <summary>Camina hacia la meta mirando hacia ella (sin deslizarse de lado). Devuelve true al llegar.</summary>
        private bool WalkTo(Vector3 goal)
        {
            Vector3 delta = goal - transform.position;
            delta.y = 0;
            if (delta.magnitude < .12f)
            {
                animator.SetFloat(SpeedHash, 0f);
                IsWalking = false;
                return true;
            }
            var look = Quaternion.LookRotation(delta);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, 240f * Time.deltaTime);
            if (Quaternion.Angle(transform.rotation, look) < 25f)
            {
                transform.position += transform.forward * (WalkSpeed * Time.deltaTime);
                animator.SetFloat(SpeedHash, 1f);
                IsWalking = true;
            }
            else
            {
                animator.SetFloat(SpeedHash, 0f);
                IsWalking = false;
            }
            return false;
        }

        private void SetVisible(bool visible)
        {
            Visible = visible;
            if (renderers == null) return;
            foreach (var renderer in renderers) if (renderer != null) renderer.enabled = visible;
            if (bubble != null && !visible) bubble.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ cartel del pedido
        private void BuildBubble()
        {
            // se clona un rótulo del casino para heredar su fuente, su material y su escala; sin él, el pedido se ve solo por el gesto
            var template = GameObject.Find("Texto CAJERA");
            if (template == null) return;
            var copy = Instantiate(template, transform);
            copy.name = "Cartel del pedido";
            copy.transform.localPosition = new Vector3(0f, 2.15f, 0f);
            copy.transform.localScale = Vector3.one;
            bubble = copy.GetComponent<TextMesh>();
            if (bubble == null) { Destroy(copy); return; }
            bubble.characterSize *= .8f;
            copy.SetActive(false);
        }

        private void UpdateBubble()
        {
            if (bubble == null) return;
            bool ordering = Flow.State == OrderState.Ordering;
            bool thanks = Flow.State == OrderState.Leaving && Flow.LeftHappy && Flow.StateSeconds < 2f;
            bool shown = Visible && (ordering || thanks);
            if (bubble.gameObject.activeSelf != shown) bubble.gameObject.SetActive(shown);
            if (!shown) return;
            if (ordering)
            {
                int price = burger != null ? burger.SalePrice : 50;
                bubble.text = "HAMBURGUESA $" + price;
                bubble.color = Flow.PatienceLeft > .33f ? new Color32(247, 238, 221, 255) : new Color32(228, 87, 46, 255);
            }
            else
            {
                bubble.text = "¡GRACIAS!";
                bubble.color = new Color32(244, 201, 93, 255);
            }
            if (player != null)
            {
                Vector3 away = bubble.transform.position - player.position;
                away.y = 0;
                if (away.sqrMagnitude > .01f) bubble.transform.rotation = Quaternion.LookRotation(away);
            }
        }

        // ------------------------------------------------------------------ cajera y clientes de ambiente
        private void UpdateQueue()
        {
            animator.SetFloat(SpeedHash, 0f);
            IsWalking = false;
            if (player != null)
            {
                Vector3 d = player.position - transform.position;
                d.y = 0;
                if (d.sqrMagnitude < 25f && d.sqrMagnitude > .01f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 120f * Time.deltaTime);
            }
        }

        private void UpdateStroll()
        {
            if (waypoints == null || waypoints.Length < 2) { animator.SetFloat(SpeedHash, 0f); return; }
            if (Time.time < waitUntil) { animator.SetFloat(SpeedHash, 0f); IsWalking = false; return; }
            Vector3 goal = waypoints[target];
            Vector3 delta = goal - transform.position;
            delta.y = 0;
            if (delta.magnitude < .12f)
            {
                target = (target + 1) % waypoints.Length;
                waitUntil = Time.time + 2f + Mathf.Abs(Mathf.Sin((seed + target) * 5.1f)) * 3f;
                animator.SetFloat(SpeedHash, 0f);
                IsWalking = false;
                return;
            }
            var look = Quaternion.LookRotation(delta);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, 240f * Time.deltaTime);
            // avanza solo cuando ya mira hacia el destino, para no deslizarse de lado
            if (Quaternion.Angle(transform.rotation, look) < 25f)
            {
                transform.position += transform.forward * (WalkSpeed * Time.deltaTime);
                animator.SetFloat(SpeedHash, 1f);
                IsWalking = true;
            }
        }
    }
}
