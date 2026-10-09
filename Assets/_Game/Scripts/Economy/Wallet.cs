using System;
using UnityEngine;

namespace Tycoon3D.Economy
{
    public sealed class Wallet : MonoBehaviour
    {
        [SerializeField] private int startingPesos = 100;
        public int Pesos { get; private set; }
        public event Action<int> Changed;

        private void Awake() => Pesos = Mathf.Max(0, startingPesos);

        public bool TrySpend(int amount)
        {
            if (amount < 0 || Pesos < amount) return false;
            Pesos -= amount;
            Changed?.Invoke(Pesos);
            return true;
        }

        public void Earn(int amount)
        {
            if (amount <= 0 || Pesos > int.MaxValue - amount) return;
            Pesos += amount;
            Changed?.Invoke(Pesos);
        }
    }
}

