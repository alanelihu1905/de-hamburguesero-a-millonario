using System;

namespace Tycoon3D.Casino
{
    /// <summary>Resultado de pagar un premio contra la caja: lo cobrado ya, la cuota de lo que se debía y lo que queda por cobrar.</summary>
    public struct CashboxPayment
    {
        public int Now;           // parte del premio de esta ronda que se paga ya
        public int Installment;   // cuota de premios anteriores que se paga en esta misma ronda
        public int Deferred;      // parte del premio de esta ronda que pasa a «por cobrar»
        public int Total => Now + Installment;
    }

    /// <summary>
    /// Caja del casino (sin Unity). Tiene un fondo inicial; las apuestas entran y los premios salen. Reglas:
    /// 1) Ninguna ronda paga más que el tope por ronda (premio nuevo más cuota de lo que se debía).
    /// 2) Si el fondo no alcanza, se paga lo que haya y el resto queda «por cobrar» (nunca se pierde ni bloquea el juego):
    ///    se va pagando en cuotas en las rondas siguientes.
    /// 3) Antes de pagar, la casa repone el fondo hasta una reserva mínima, así que cada ronda puede pagar al menos esa reserva
    ///    y el avance rápido del casino no se frena. Lo repuesto se anota (Refilled) para auditar.
    /// Las tablas de premios no cambian: el tope solo reparte en el tiempo lo que es muy grande.
    /// </summary>
    public sealed class CasinoCashbox
    {
        public const int DefaultInitialFunds = 20000;
        public const int DefaultRoundCap = 5000;
        public const int DefaultReserveFloor = 1000;

        public int InitialFunds { get; }
        public int RoundCap { get; }
        public int ReserveFloor { get; }

        public int Funds { get; private set; }
        public int Owed { get; private set; }
        /// <summary>Pesos que la casa puso para sostener la reserva mínima (no salen de los jugadores).</summary>
        public long Refilled { get; private set; }

        public CasinoCashbox(int initialFunds = DefaultInitialFunds, int roundCap = DefaultRoundCap, int reserveFloor = DefaultReserveFloor)
        {
            if (initialFunds < 0 || roundCap <= 0 || reserveFloor < 0 || reserveFloor > roundCap)
                throw new ArgumentException("Reglas de caja inválidas.");
            InitialFunds = initialFunds;
            RoundCap = roundCap;
            ReserveFloor = reserveFloor;
            Funds = initialFunds;
        }

        public void Restore(int funds, int owed, long refilled)
        {
            Funds = Math.Max(0, funds);
            Owed = Math.Max(0, owed);
            Refilled = Math.Max(0, refilled);
        }

        /// <summary>La apuesta aceptada entra a la caja.</summary>
        public void TakeStake(int amount)
        {
            if (amount < 0 || Funds > int.MaxValue - amount) throw new ArgumentOutOfRangeException(nameof(amount));
            Funds += amount;
        }

        /// <summary>Si el fondo bajó de la reserva, la casa lo repone. Devuelve lo repuesto (0 casi siempre).</summary>
        public int EnsureReserve()
        {
            if (Funds >= ReserveFloor) return 0;
            int add = ReserveFloor - Funds;
            Funds = ReserveFloor;
            Refilled += add;
            return add;
        }

        /// <summary>Paga el premio de la ronda y una cuota de lo que se debía, respetando tope y fondo.</summary>
        public CashboxPayment Pay(int prize)
        {
            if (prize < 0) throw new ArgumentOutOfRangeException(nameof(prize));
            int owedBefore = Owed;
            int now = Math.Min(prize, Math.Min(RoundCap, Funds));
            Funds -= now;
            int deferred = prize - now;
            int installment = Math.Min(owedBefore, Math.Min(RoundCap - now, Funds));
            Funds -= installment;
            Owed = owedBefore - installment + deferred;
            return new CashboxPayment { Now = now, Installment = installment, Deferred = deferred };
        }
    }
}
