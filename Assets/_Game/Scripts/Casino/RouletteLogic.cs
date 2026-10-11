using System;

namespace Tycoon3D.Casino
{
    public enum RouletteBetKind { StraightNumber, Red, Black, Odd, Even, Low, High }

    /// <summary>Una apuesta de ruleta con dinero virtual. StraightNumber usa Selection de 0 a 36.</summary>
    public readonly struct RouletteBet
    {
        public readonly RouletteBetKind Kind;
        public readonly int Selection;
        public readonly int Stake;

        public RouletteBet(RouletteBetKind kind, int stake, int selection = -1)
        {
            if (stake <= 0) throw new ArgumentOutOfRangeException(nameof(stake));
            if (kind == RouletteBetKind.StraightNumber && (selection < 0 || selection > 36))
                throw new ArgumentOutOfRangeException(nameof(selection), "El número debe estar entre 0 y 36.");
            if (kind != RouletteBetKind.StraightNumber && selection != -1)
                throw new ArgumentException("Solo una apuesta a número acepta selección.", nameof(selection));
            if (!Enum.IsDefined(typeof(RouletteBetKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            Kind = kind;
            Selection = selection;
            Stake = stake;
        }
    }

    public readonly struct RouletteRound
    {
        public readonly RouletteBet Bet;
        public readonly int Number;
        public readonly int GrossPayout;
        public bool Won => GrossPayout > 0;
        public int Net => GrossPayout - Bet.Stake;

        public RouletteRound(RouletteBet bet, int number)
        {
            if (number < 0 || number > 36) throw new ArgumentOutOfRangeException(nameof(number));
            Bet = bet;
            Number = number;
            GrossPayout = RouletteLogic.Wins(bet, number)
                ? checked(bet.Stake * (bet.Kind == RouletteBetKind.StraightNumber ? 36 : 2))
                : 0;
        }
    }

    /// <summary>Ruleta europea de un cero. Payout es retorno bruto incluyendo la apuesta; solo moneda virtual.</summary>
    public static class RouletteLogic
    {
        public const int NumberCount = 37;
        private static readonly int[] Reds =
        {
            1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36
        };

        public static RouletteRound Play(RouletteBet bet, IRandomSource rng)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            int number = rng.Next(NumberCount);
            if (number < 0 || number >= NumberCount)
                throw new InvalidOperationException("La fuente aleatoria devolvió un número fuera de rango.");
            return new RouletteRound(bet, number);
        }

        public static bool IsRed(int number)
        {
            ValidateNumber(number);
            for (int i = 0; i < Reds.Length; i++) if (Reds[i] == number) return true;
            return false;
        }

        public static bool IsBlack(int number) => number != 0 && !IsRed(number);

        public static bool Wins(RouletteBet bet, int number)
        {
            ValidateNumber(number);
            switch (bet.Kind)
            {
                case RouletteBetKind.StraightNumber: return number == bet.Selection;
                case RouletteBetKind.Red: return IsRed(number);
                case RouletteBetKind.Black: return IsBlack(number);
                case RouletteBetKind.Odd: return number != 0 && number % 2 == 1;
                case RouletteBetKind.Even: return number != 0 && number % 2 == 0;
                case RouletteBetKind.Low: return number >= 1 && number <= 18;
                case RouletteBetKind.High: return number >= 19 && number <= 36;
                default: throw new ArgumentOutOfRangeException(nameof(bet));
            }
        }

        private static void ValidateNumber(int number)
        {
            if (number < 0 || number >= NumberCount) throw new ArgumentOutOfRangeException(nameof(number));
        }
    }
}
