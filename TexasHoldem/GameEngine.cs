using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TexasHoldem
{
    internal class GameEngine
    {
        private Deck _deck;
        private List<Player> _players;
        public List<Card> CommunityCards { get; private set; }

        //public RoundPhase CurrentPhase { get; private set; }
        //private int _currentPlayerIndex;

        public GameEngine(List<Player> players) {
            /* ... */
        }
    }
}
