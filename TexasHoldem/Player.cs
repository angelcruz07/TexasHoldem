using System.Collections.Generic;

namespace TexasHoldem
{
    internal class Player
    {
        public string Name { get; private set; }
        public List<Card> HoleCards { get; private set; }
        public int Chips { get; set; }
        public int CurrentBet { get; set; } // Lo que ha apostado en la ronda actual (betting round)
        
        // Estados del jugador
        public bool IsFolded { get; set; }
        public bool IsAllIn { get; set; }
        
        // Constructor
        public Player(string name, int startingChips)
        {
            this.Name = name;
            this.Chips = startingChips;
            this.HoleCards = new List<Card>();
            this.IsFolded = false;
            this.IsAllIn = false;
            this.CurrentBet = 0;
        }

        // --- MÉTODOS ---

        // 1. Añade una carta a la mano del jugador
        public void ReceiveCard(Card card)
        {
            if (HoleCards.Count < 2)
            {
                HoleCards.Add(card);
            }
        }

        // 2. Limpia el estado para la siguiente mano
        public void ResetForNewHand()
        {
            HoleCards.Clear();
            IsFolded = false;
            IsAllIn = (Chips == 0); // Si tiene 0 fichas, técnicamente no puede jugar, pero se maneja fuera
            CurrentBet = 0;
        }

        // 3. Resetear apuestas para la siguiente fase (Flop, Turn, River)
        public void ResetBet()
        {
            CurrentBet = 0;
        }

        // Acciones de apuesta

        public void Fold()
        {
            IsFolded = true;
        }

        public void Bet(int amount)
        {
            if (amount > Chips)
            {
                amount = Chips; // All-in si no tiene suficiente
            }

            Chips -= amount;
            CurrentBet += amount;

            if (Chips == 0)
            {
                IsAllIn = true;
            }
        }

        // Devuelve true si el jugador está activo en la mano (no folded, no eliminado)
        public bool IsInHand => !IsFolded;
        
        public override string ToString()
        {
            return $"{Name} (${Chips})";
        }
    }
}
