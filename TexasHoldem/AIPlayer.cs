using System;
using System.Collections.Generic;
using System.Linq;

namespace TexasHoldem
{
    internal class AIPlayer : Player
    {
        private Random _random;

        public AIPlayer(string name, int startingChips) : base(name, startingChips)
        {
            _random = new Random();
        }

        // Lógica simple de IA: decide qué acción tomar basándose en probabilidades básicas
        public string DecideAction(int currentBet, int pot, List<Card> communityCards)
        {
            // Si no hay apuesta que igualar, puede check o raise
            if (currentBet == CurrentBet)
            {
                // Decisión: 70% check, 30% raise pequeño
                if (_random.Next(100) < 70)
                {
                    return "check";
                }
                else
                {
                    return "raise";
                }
            }
            else
            {
                // Hay apuesta que igualar
                int callAmount = currentBet - CurrentBet;
                
                // Si la apuesta es muy alta comparada con sus fichas, más probabilidad de fold
                double betRatio = (double)callAmount / Chips;
                
                if (betRatio > 0.5 && _random.Next(100) < 40)
                {
                    return "fold";
                }
                else if (betRatio < 0.2 || _random.Next(100) < 70)
                {
                    return "call";
                }
                else
                {
                    return "fold";
                }
            }
        }

        public int DecideRaiseAmount(int currentBet, int pot)
        {
            // Raise mínimo: 2x la apuesta actual o mínimo 40
            int minRaise = Math.Max(currentBet * 2, 40);
            int maxRaise = Math.Min(minRaise + 100, Chips);
            
            // Raise aleatorio entre min y max
            if (maxRaise > minRaise)
            {
                return _random.Next(minRaise, maxRaise);
            }
            return minRaise;
        }
    }
}


