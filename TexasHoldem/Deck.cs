using System;
using System.Collections.Generic;

namespace TexasHoldem
{
    internal class Deck
    {
        private List<Card> _cards;

        public Deck()
        {
            _cards = new List<Card>();
            InitializeDeck();
        }


        /*
         * Inicializar el mazo
         * Crea una lista de cartas donde se asigna
         * una imagen a cada una
         */
        private void InitializeDeck()
        {
            if (_cards == null)
            {
                _cards = new List<Card>();
            }
            _cards.Clear();


            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                {
                    _cards.Add(new Card(suit, rank));
                }
            }
        }

        public void Shuffle()
        {
            Random random = new Random();

            for(int i = _cards.Count - 1; i > 0; i--)
            {
                // Indice aleatorio
                int j = random.Next(i + 1);

                // Swap

                //1. Guardar temporalmente la carta en la posicion actual
                Card temp = _cards[i];

                // 2. mover la carta aleatoria
                _cards[i] = _cards[j];

                // 3. mover la carta temporal i 
                _cards[j] = temp;
            }
        }

        public Card DealCard() // Paso 4: Repartir
        {
            if (_cards.Count == 0)
            {
                throw new InvalidOperationException("No cards left in the deck.");
            }

            // Seleccionar la cartas del indice 0;
            Card dealtCard = _cards[0];

            // Remover la carta y evitar que se vuelva a repartir
            _cards.RemoveAt(0);

            // Retornar la carta seleccionada
            return dealtCard;        
        }
    }
}
