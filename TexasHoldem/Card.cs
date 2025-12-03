using System;

namespace TexasHoldem
{
    public enum Suit
    {
        // Asignar 0, 1, 2, 3 facilita el manejo interno
        Clubs = 0,    // Tréboles
        Diamonds = 1, // Diamantes
        Hearts = 2,   // Corazones
        Spades = 3    // Picas
    }

    public enum Rank
    {
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11, // J
        Queen = 12, // Q
        King = 13, // K
        Ace = 14  // A
    }

    internal class Card : IComparable<Card>
    {
        public Suit Suit { get; private set; }
        public Rank Rank { get; private set; }

        public Card(Suit suit, Rank rank)
        {
            this.Suit = suit;
            this.Rank = rank;
        }

        public int CompareTo(Card other)
        {
            if (other == null) return 1;
            // Retorna -1 si es menor, 0 si es igual, 1 si es mayor.
            return this.Rank.CompareTo(other.Rank);
        }
        public override string ToString()
        {
            return $"{this.Rank} de {this.Suit}";
        }

        public string GetImageFileNameAlternativa()
        {
            string rankPart;
            int rankValue = (int)this.Rank;

            // Si es del 2 al 10, usa el número; si es figura, usa el nombre.
            if (rankValue >= 2 && rankValue <= 10)
            {
                rankPart = rankValue.ToString();
            }
            else
            {
                // Se usa el nombre de la figura (Jack, Queen, King, Ace)
                rankPart = this.Rank.ToString();
            }

            string suitText = this.Suit.ToString();

            // Ejemplo: 2_of_clubs.png o ace_of_spades.png
            return $"{rankPart.ToLower()}_of_{suitText.ToLower()}.png";
        }

    }
}
