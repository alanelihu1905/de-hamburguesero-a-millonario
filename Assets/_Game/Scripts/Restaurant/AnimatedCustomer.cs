using UnityEngine;

namespace Tycoon3D.Restaurant
{
    public enum CustomerMode { Queue, Stroll, Cashier }

    /// <summary>
    /// Cliente animado de ambiente (Idle, Caminar, Pedir). No toca la economía: las ventas siguen en BurgerLoop.
    /// Queue: espera en la fila, mira al jugador si está cerca y pide de vez en cuando.
    /// Stroll: camina entre puntos con el clip Caminar a su velocidad real (sin patinar) y se detiene a descansar.
    /// </summary>
    public sealed class AnimatedCustomer : MonoBehaviour
    {
        public const float WalkSpeed = 1.42f;      // m/s del ciclo Caminar medido en Blender

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

        public CustomerMode Mode => mode;
        public Animator Animator => animator;
        public bool IsWalking { get; private set; }

        public void Configure(CustomerMode customerMode, Animator anim, Vector3[] path, float randomSeed)
        {
            mode = customerMode;
            animator = anim;
            waypoints = path;
            seed = randomSeed;
        }

        private void Start()
        {
            nextOrderAt = Time.time + 2f + Mathf.Abs(Mathf.Sin(seed * 12.9898f)) * 6f;
            waitUntil = Time.time + Mathf.Abs(Mathf.Sin(seed * 78.233f)) * 3f;
            if (waypoints != null && waypoints.Length > 1) target = 1;
            var cam = Camera.main;
            if (cam != null) player = cam.transform;
        }

        private void Update()
        {
            if (animator == null) return;
            if (player == null && Camera.main != null) player = Camera.main.transform;
            if (mode == CustomerMode.Queue || mode == CustomerMode.Cashier) UpdateQueue(); else UpdateStroll();
        }

        /// <summary>Gesto de atención (el clip Pedir): la cajera lo usa al atender al jugador.</summary>
        public void Gesture()
        {
            if (animator != null) animator.SetTrigger(OrderHash);
            LastGestureTime = Time.time;
        }

        public float LastGestureTime { get; private set; } = -100f;

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
            if (mode == CustomerMode.Queue && Time.time >= nextOrderAt)
            {
                animator.SetTrigger(OrderHash);
                nextOrderAt = Time.time + 7f + Mathf.Abs(Mathf.Sin((seed + Time.time) * 3.7f)) * 5f;
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
