using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TexasHoldem
{
    internal class AIPlayer : Player
    {
        public AIPlayer(string name) : base(name) { }

        // Este es el corazón de la IA
        public override PlayerAction GetAction()
        {
            // Lógica de IA:
            // 1. Si no hay cartas comunitarias, decide al azar (o siempre Check/Call).

            // 2. Si ya hay cartas comunitarias (Flop/Turn/River):
            //    a. LLAMA al HandEvaluator para ver su fuerza.
            //    b. Si es una mano débil (Par o peor), decide Fold.
            //    c. Si es una mano fuerte (Dos Pares o mejor), decide Check/Call.

            // Deberás implementar aquí la llamada al HandEvaluator y la lógica de decisión.
            return PlayerAction.Check; // Placeholder
        }
    }
}
