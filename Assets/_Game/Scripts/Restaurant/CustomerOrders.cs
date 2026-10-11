using System.Linq;
using Tycoon3D.Economy;
using UnityEngine;

namespace Tycoon3D.Restaurant
{
    /// <summary>
    /// Une la venta con los clientes de la fila: cuando <see cref="BurgerLoop"/> completa una venta, el cliente que lleva más
    /// tiempo esperando su pedido «paga» (se va a la calle y vuelve con otro). No mueve dinero ni escribe en el libro: eso ya
    /// lo hizo BurgerLoop una sola vez. Una venta sin nadie esperando se cobra igual. Se crea sola desde el perfil.
    /// </summary>
    public sealed class CustomerOrders : MonoBehaviour
    {
        private BurgerLoop burger;

        /// <summary>Clientes servidos desde que cargó la escena.</summary>
        public int Served { get; private set; }
        public AnimatedCustomer LastServed { get; private set; }

        public void Bind(BurgerLoop loop)
        {
            if (burger != null) burger.MovementRecorded -= OnMovement;
            burger = loop;
            if (burger != null) burger.MovementRecorded += OnMovement;
        }

        private void OnDestroy()
        {
            if (burger != null) burger.MovementRecorded -= OnMovement;
        }

        private void OnMovement(MovementKind kind, int amount, MovementAccount from, MovementAccount to)
        {
            if (kind == MovementKind.Sale) ServeNext();
        }

        /// <summary>Le da la venta al cliente que más espera. Devuelve true si había alguien con un pedido.</summary>
        public bool ServeNext()
        {
            var next = AnimatedCustomer.All
                .Where(c => c != null && c.Mode == CustomerMode.Queue && c.Flow != null && c.Flow.HasOrder)
                .OrderByDescending(c => c.Flow.WaitingSeconds)
                .FirstOrDefault();
            if (next == null || !next.Pay()) return false;
            Served++;
            LastServed = next;
            return true;
        }
    }
}
