using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TexasHoldem
{
    internal class HandEvaluator : IComparable<HandEvaluator>
    {
        public enum HandRank
        {
            HighCard = 1,        // Carta Alta
            Pair = 2,            // Par
            TwoPair = 3,         // Dos Pares
            ThreeOfAKind = 4,    // Trío
            Straight = 5,        // Escalera
            Flush = 6,           // Color
            FullHouse = 7,       // Full
            FourOfAKind = 8,     // Póker
            StraightFlush = 9    // Escalera de Color
        }
        public HandRank Rank { get; private set; }

        public List<Rank> PrimaryRanks { get; private set; }

        // Propiedad 3: Los kickers, usados solo para desempate.
        public List<Rank> KickerRanks { get; private set; }

        public  HandEvaluator(HandRank rank, List<Rank> primaryRanks, List<Rank> kickerRanks)
        {
            this.Rank = rank;
            this.PrimaryRanks = primaryRanks;
            this.KickerRanks = kickerRanks;
        }

        public int CompareTo(HandEvaluator other)
        {
            // 1. Comparar por Rank (Ej: Full House vs Flush)
            int rankComparison = this.Rank.CompareTo(other.Rank);
            if (rankComparison != 0)
            {
                return rankComparison; // Si es positivo, this es mejor. Si es negativo, other es mejor.
            }

            // 2. Si son iguales, comparar PrimaryRanks (Ej: Full de Ases vs Full de Reyes)
            for (int i = 0; i < this.PrimaryRanks.Count; i++)
            {
                int primaryComparison = this.PrimaryRanks[i].CompareTo(other.PrimaryRanks[i]);
                if (primaryComparison != 0)
                {
                    return primaryComparison;
                }
            }

            // 3. Si siguen empatados, comparar KickerRanks (Ej: Par de Ases con Kicker King vs Kicker Queen)
            for (int i = 0; i < this.KickerRanks.Count; i++)
            {
                // Nota: Es posible que una mano tenga más kickers que la otra, pero en póker,
                // las manos siempre se componen de 5 cartas, por lo que el número de kickers debe ser el mismo
                // para manos del mismo tipo. Asumimos listas de igual longitud.
                int kickerComparison = this.KickerRanks[i].CompareTo(other.KickerRanks[i]);
                if (kickerComparison != 0)
                {
                    return kickerComparison;
                }
            }

            // ESTA ES UNA TAREA DE LÓGICA MUY AVANZADA PARA DESPUÉS.
            return 0; // Debe contener la lógica de comparación
        }

    }
}
