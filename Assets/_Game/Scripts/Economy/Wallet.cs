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

        /// <summary>Fija el saldo desde el perfil guardado (arranque o recarga de escena). No cuenta como ganancia ni gasto.</summary>
        public void Restore(int pesos)
        {
            Pesos = Mathf.Max(0, pesos);
            Changed?.Invoke(Pesos);
        }

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

