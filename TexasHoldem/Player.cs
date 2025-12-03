using System.Collections.Generic;


namespace TexasHoldem
{
    internal abstract class Player
    {
        public string Name { get; private set; }
        public List<Card> HoleCards { get; private set; }

        public bool IsActive { get; set; }

        // --- CONSTRUCTOR ---
        public Player(string name)
        {
            this.Name = name;
            this.HoleCards = new List<Card>();
            this.IsActive = true;
        }

        // --- MÉTODOS COMUNES ---

        // 1. Añade una carta a la mano del jugador (llamado por el Deck)
        public void ReceiveCard(Card card)
        {
            if (HoleCards.Count < 2)
            {
                HoleCards.Add(card);
            }
        }

        // 2. Limpia el estado para la siguiente ronda
        public void ResetForNewRound()
        {
            HoleCards.Clear();
            IsActive = true;
            // Si no hay apuestas, esta limpieza es suficiente.
        }

        // --- MÉTODO ABSTRACTO CRÍTICO ---

        // 3. Define la acción del jugador (dependerá de si es Humano o IA)
        // Usaremos un enum para representar las acciones disponibles (aunque no haya apuestas).
        public abstract PlayerAction GetAction();
    }
}
