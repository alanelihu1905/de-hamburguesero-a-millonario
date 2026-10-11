using System;

namespace Tycoon3D.Restaurant
{
    public enum OrderState { Idle, Ordering, Leaving, Away, Returning }

    /// <summary>
    /// Ciclo de pedido de un cliente de la fila (sin Unity, para probarlo con dotnet con un reloj simulado). Espera un rato,
    /// pide una hamburguesa, aguanta con paciencia limitada y, si le sirven, se va y vuelve más tarde con otro pedido; si se
    /// le acaba la paciencia se va sin pagar. No toca el dinero: el cobro lo hace solo <c>BurgerLoop.Sell</c>; esto solo dice
    /// qué cliente está esperando (ver Docs/DISENO_CLIENTES_PIDEN.md).
    /// </summary>
    public sealed class CustomerOrderFlow
    {
        public const float PatienceSeconds = 45f;
        public const float AwayAfterPaidSeconds = 8f;
        public const float AwayAfterGivingUpSeconds = 14f;
        public const float FirstOrderMinSeconds = 3f, FirstOrderMaxSeconds = 9f;
        public const float NextOrderMinSeconds = 5f, NextOrderMaxSeconds = 12f;
        /// <summary>Si por lo que sea no llega a donde iba (queda atascado), el flujo sigue pasados estos segundos.</summary>
        public const float WalkTimeoutSeconds = 40f;

        private readonly float seed;
        private int draws;
        private float stateTime;
        private float orderDelay;
        private float awayFor;

        public CustomerOrderFlow(float seed)
        {
            this.seed = seed;
            orderDelay = Range(FirstOrderMinSeconds, FirstOrderMaxSeconds);
        }

        public OrderState State { get; private set; } = OrderState.Idle;
        public int Served { get; private set; }
        public int GaveUp { get; private set; }
        /// <summary>True si el cliente se está yendo (o se fue) porque le sirvieron; false si se cansó de esperar.</summary>
        public bool LeftHappy { get; private set; }
        /// <summary>Segundos que lleva esperando en el pedido actual (0 si no está pidiendo).</summary>
        public float WaitingSeconds => State == OrderState.Ordering ? stateTime : 0f;
        /// <summary>Paciencia que le queda, de 1 (recién pidió) a 0 (se va).</summary>
        public float PatienceLeft => State == OrderState.Ordering ? Math.Max(0f, 1f - stateTime / PatienceSeconds) : 0f;
        public bool HasOrder => State == OrderState.Ordering;
        /// <summary>Segundos que lleva en el estado actual.</summary>
        public float StateSeconds => stateTime;

        public event Action OrderStarted;
        public event Action Paid;
        public event Action GaveUpWaiting;

        /// <summary>Avanza el reloj. <paramref name="arrived"/> avisa que el cliente llegó a donde caminaba (calle o fila).</summary>
        public void Update(float dt, bool arrived)
        {
            if (dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            stateTime += dt;
            switch (State)
            {
                case OrderState.Idle:
                    if (stateTime >= orderDelay) Begin();
                    break;
                case OrderState.Ordering:
                    if (stateTime >= PatienceSeconds)
                    {
                        GaveUp++;
                        LeftHappy = false;
                        awayFor = AwayAfterGivingUpSeconds;
                        Enter(OrderState.Leaving);
                        GaveUpWaiting?.Invoke();
                    }
                    break;
                case OrderState.Leaving:
                    if (arrived || stateTime >= WalkTimeoutSeconds) Enter(OrderState.Away);
                    break;
                case OrderState.Away:
                    if (stateTime >= awayFor) Enter(OrderState.Returning);
                    break;
                case OrderState.Returning:
                    if (arrived || stateTime >= WalkTimeoutSeconds)
                    {
                        orderDelay = Range(NextOrderMinSeconds, NextOrderMaxSeconds);
                        Enter(OrderState.Idle);
                    }
                    break;
            }
        }

        /// <summary>El jugador completó una venta y este cliente era el que esperaba. Solo vale con un pedido en curso.</summary>
        public bool TryPay()
        {
            if (State != OrderState.Ordering) return false;
            Served++;
            LeftHappy = true;
            awayFor = AwayAfterPaidSeconds;
            Enter(OrderState.Leaving);
            Paid?.Invoke();
            return true;
        }

        /// <summary>Pruebas y capturas: empieza un pedido ya.</summary>
        public void ForceOrder()
        {
            if (State == OrderState.Idle) Begin();
        }

        private void Begin()
        {
            Enter(OrderState.Ordering);
            OrderStarted?.Invoke();
        }

        private void Enter(OrderState next)
        {
            State = next;
            stateTime = 0f;
        }

        // azar determinista por cliente (sin System.Random): dos clientes con otra semilla piden en momentos distintos
        private float Range(float min, float max)
        {
            double x = Math.Sin(seed * 12.9898 + (++draws) * 78.233) * 43758.5453;
            double frac = x - Math.Floor(x);
            return min + (float)frac * (max - min);
        }
    }
}
