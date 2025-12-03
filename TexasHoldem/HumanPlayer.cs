using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TexasHoldem
{
    internal class HumanPlayer: Player
    {
        public HumanPlayer(string name) : base(name) { }

        // Simplemente devuelve una acción. La acción real es capturada por Windows Forms.
        //public override PlayerAction GetAction()
        //{
        //    // En un proyecto real, esto notificaría al Form que es el turno del jugador humano
        //    // y esperaría a que un botón sea presionado. 
        //    // Para la lógica de clases, podemos devolver algo temporal.
        //    return PlayerAction.Check;
        //}
    }
}
