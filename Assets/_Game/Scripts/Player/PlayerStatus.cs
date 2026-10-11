using UnityEngine;

namespace Tycoon3D.Player
{
    /// <summary>Estado vital y carga del jugador. Daños y entregas futuros usan esta fuente única.</summary>
    public sealed class PlayerStatus : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximumHealth = 100;
        [SerializeField, Min(0)] private int contrabandPackages;
        [SerializeField, Range(0, 100)] private int heat;

        public int MaximumHealth => Mathf.Max(1, maximumHealth);
        public int Health { get; private set; }
        public int ContrabandPackages => contrabandPackages;
        public int Heat => heat;

        private void Awake() => Health = MaximumHealth;

        public void TakeDamage(int amount)
        {
            if (amount <= 0) return;
            Health = Mathf.Max(0, Health - amount);
        }

        public void Heal(int amount)
        {
            if (amount <= 0) return;
            Health = Mathf.Min(MaximumHealth, Health + amount);
        }

        public void AddContraband(int packages, int heatAdded)
        {
            if (packages < 0 || heatAdded < 0) return;
            contrabandPackages = (int)System.Math.Min(int.MaxValue, (long)contrabandPackages + packages);
            heat = (int)System.Math.Min(100L, (long)heat + heatAdded);
        }

        public void RemoveContraband(int packages)
        {
            if (packages <= 0) return;
            contrabandPackages = Mathf.Max(0, contrabandPackages - packages);
        }

        public void SetHeat(int value) => heat = Mathf.Clamp(value, 0, 100);
    }
}
