using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TexasHoldem
{
    public enum RoundPhase
    {
        PreFlop,
        Flop,
        Turn,
        River,
        Showdown
    }

    internal class GameEngine
    {
        private Deck _deck;
        private List<Player> _players;
        
        public List<Card> CommunityCards { get; private set; }
        public int Pot { get; private set; }
        public int CurrentBet { get; private set; } // La apuesta más alta actual en la mesa
        public RoundPhase CurrentPhase { get; private set; }

        private int _dealerIndex;
        private int _currentPlayerIndex;
        private int _playersActedInRound; // Cuántos jugadores han actuado en la ronda actual
        
        // Configuración de ciegas
        private const int SMALL_BLIND = 10;
        private const int BIG_BLIND = 20;

        public Player CurrentPlayer => _players[_currentPlayerIndex];
        public bool IsCurrentPlayerAI => CurrentPlayer is AIPlayer;

        // Eventos
        public event EventHandler OnGameStateChanged;       // Actualizar toda la mesa
        public event EventHandler<string> OnTurnChanged;    // Turno de jugador
        public event EventHandler<string> OnRoundEnded;     // Fin de una mano
        public event EventHandler<string> OnPhaseChanged;   // Cambio de Flop/Turn/etc

        public GameEngine(List<Player> players)
        {
            _players = players;
            _deck = new Deck();
            CommunityCards = new List<Card>();
            _dealerIndex = 1; // El bot es el dealer, así el jugador humano comienza primero
        }

        public void StartGame()
        {
            StartNewHand();
        }

        private void StartNewHand()
        {
            // Resetear estado
            _deck = new Deck();
            _deck.Shuffle();
            CommunityCards.Clear();
            Pot = 0;
            CurrentBet = 0;
            CurrentPhase = RoundPhase.PreFlop;
            _playersActedInRound = 0;

            // Resetear jugadores y repartir cartas
            foreach (var p in _players)
            {
                p.ResetForNewHand();
                if (p.Chips > 0)
                {
                    p.ReceiveCard(_deck.DealCard());
                    p.ReceiveCard(_deck.DealCard());
                }
            }

            // Poner ciegas
            // Dealer -> Small Blind -> Big Blind
            int activePlayersCount = _players.Count(p => p.Chips > 0);
            if (activePlayersCount < 2) return; // Fin del juego

            int sbIndex = GetNextActivePlayerIndex(_dealerIndex);
            int bbIndex = GetNextActivePlayerIndex(sbIndex);

            PlaceBet(_players[sbIndex], SMALL_BLIND);
            PlaceBet(_players[bbIndex], BIG_BLIND);

            CurrentBet = BIG_BLIND;

            // El turno empieza después del Big Blind en PreFlop
            _currentPlayerIndex = GetNextActivePlayerIndex(bbIndex);

            UpdateUI();
            NotifyTurn();
        }

        private void PlaceBet(Player player, int amount)
        {
            player.Bet(amount);
            Pot += amount;
        }

        public void ProcessAction(string action, int amount = 0)
        {
            Player player = CurrentPlayer;

            switch (action.ToLower())
            {
                case "fold":
                    player.Fold();
                    break;
                case "check":
                    // Solo válido si CurrentBet == player.CurrentBet
                    break;
                case "call":
                    int callAmount = CurrentBet - player.CurrentBet;
                    PlaceBet(player, callAmount);
                    break;
                case "raise":
                    // Raise to 'amount' (total bet)
                    // Simplified: raise adds to pot and sets new CurrentBet
                    // amount here is usually the TOTAL amount they want to put in? Or the ADDED amount?
                    // Let's assume amount is the TOTAL bet they want to have.
                    // But usually UI sends "Raise" and we calculate.
                    // For simplicity: Raise means "Match current bet + Raise Amount"
                    // Or "Make the bet X". Let's assume input 'amount' is the target total bet.
                    if (amount > CurrentBet)
                    {
                        int diff = amount - player.CurrentBet;
                        PlaceBet(player, diff);
                        CurrentBet = amount;
                        // Al hacer raise, reiniciamos el conteo de quién ha igualado,
                        // pero debemos tener cuidado con el loop.
                        // Simplificación: Todos deben actuar de nuevo excepto el que hizo raise (si nadie más sube).
                        _playersActedInRound = 0; 
                    }
                    break;
            }

            _playersActedInRound++;
            
            // Actualizar UI inmediatamente después de la acción
            UpdateUI();
            
            NextTurn();
        }

        private void NextTurn()
        {
            // Verificar si todos han foldeado menos uno
            if (_players.Count(p => !p.IsFolded) == 1)
            {
                EndHand(_players.First(p => !p.IsFolded));
                return;
            }

            // Verificar si la ronda de apuestas ha terminado
            // La ronda termina cuando todos los jugadores activos han actuado Y sus apuestas son iguales al CurrentBet
            // O están All-In.
            
            bool allBetsEqual = _players.Where(p => !p.IsFolded && !p.IsAllIn).All(p => p.CurrentBet == CurrentBet);
            int activePlayers = _players.Count(p => !p.IsFolded && !p.IsAllIn); // Jugadores que aún pueden actuar

            // Si todos actuaron y las apuestas están igualadas
            if (allBetsEqual && _playersActedInRound >= activePlayers) 
            {
                NextPhase();
                return;
            }

            // Siguiente jugador
            _currentPlayerIndex = GetNextActivePlayerIndex(_currentPlayerIndex);
            
            UpdateUI();
            NotifyTurn();
            
            // Si es turno de la IA, actuar automáticamente después de un pequeño delay
            if (IsCurrentPlayerAI)
            {
                // Usar Timer en lugar de Thread.Sleep para no bloquear la UI
                System.Windows.Forms.Timer aiTimer = new System.Windows.Forms.Timer();
                aiTimer.Interval = 1500; // 1.5 segundos de delay
                aiTimer.Tick += (s, args) =>
                {
                    aiTimer.Stop();
                    aiTimer.Dispose();
                    ProcessAIAction();
                };
                aiTimer.Start();
            }
        }
        
        private void ProcessAIAction()
        {
            if (CurrentPlayer is AIPlayer aiPlayer)
            {
                string action = aiPlayer.DecideAction(CurrentBet, Pot, CommunityCards);
                int raiseAmount = 0;
                
                if (action == "raise")
                {
                    raiseAmount = aiPlayer.DecideRaiseAmount(CurrentBet, Pot);
                }
                
                ProcessAction(action, raiseAmount);
            }
        }

        private void NextPhase()
        {
            // Resetear apuestas de los jugadores para la nueva ronda
            foreach (var p in _players) p.ResetBet();
            CurrentBet = 0;
            _playersActedInRound = 0;
            
            // Mover dealer button virtualmente para iniciar ronda de apuestas?
            // En flop/turn/river, empieza el primero activo a la izquierda del dealer.
            _currentPlayerIndex = GetNextActivePlayerIndex(_dealerIndex);

            switch (CurrentPhase)
            {
                case RoundPhase.PreFlop:
                    CurrentPhase = RoundPhase.Flop;
                    CommunityCards.Add(_deck.DealCard());
                    CommunityCards.Add(_deck.DealCard());
                    CommunityCards.Add(_deck.DealCard());
                    break;
                case RoundPhase.Flop:
                    CurrentPhase = RoundPhase.Turn;
                    CommunityCards.Add(_deck.DealCard());
                    break;
                case RoundPhase.Turn:
                    CurrentPhase = RoundPhase.River;
                    CommunityCards.Add(_deck.DealCard());
                    break;
                case RoundPhase.River:
                    CurrentPhase = RoundPhase.Showdown;
                    EndHand(null); // Showdown
                    return;
            }

            OnPhaseChanged?.Invoke(this, CurrentPhase.ToString());
            UpdateUI();
            NotifyTurn();
            
            // Si es turno de la IA, actuar automáticamente después de un pequeño delay
            if (IsCurrentPlayerAI)
            {
                // Usar Timer en lugar de Thread.Sleep para no bloquear la UI
                System.Windows.Forms.Timer aiTimer = new System.Windows.Forms.Timer();
                aiTimer.Interval = 1500; // 1.5 segundos de delay
                aiTimer.Tick += (s, args) =>
                {
                    aiTimer.Stop();
                    aiTimer.Dispose();
                    ProcessAIAction();
                };
                aiTimer.Start();
            }
        }

        private void EndHand(Player winner)
        {
            if (winner != null)
            {
                // Ganador por fold
                winner.Chips += Pot;
                OnRoundEnded?.Invoke(this, $"{winner.Name} gana ${Pot} (otros se retiraron)");
            }
            else
            {
                // Showdown logic
                // Aquí iría el HandEvaluator. Como no está implementado, le damos el bote al primero activo (dummy).
                // TODO: Implementar HandEvaluator real.
                var activePlayers = _players.Where(p => !p.IsFolded).ToList();
                if (activePlayers.Count > 0)
                {
                    // Lógica temporal: Gana el azar o el primero
                    Player showdownWinner = activePlayers[0]; 
                    showdownWinner.Chips += Pot;
                    OnRoundEnded?.Invoke(this, $"Showdown: {showdownWinner.Name} gana ${Pot}");
                }
            }

            // Preparar siguiente mano
            _dealerIndex = (_dealerIndex + 1) % _players.Count;
            // Esperar un momento o pedir al usuario reiniciar?
            // Dejamos que la UI llame a StartNewHand() cuando esté lista.
        }

        private int GetNextActivePlayerIndex(int startIndex)
        {
            int i = (startIndex + 1) % _players.Count;
            int count = 0;
            while ((_players[i].IsFolded || _players[i].Chips == 0) && count < _players.Count)
            {
                i = (i + 1) % _players.Count;
                count++;
            }
            return i;
        }

        private void UpdateUI()
        {
            OnGameStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void NotifyTurn()
        {
            OnTurnChanged?.Invoke(this, CurrentPlayer.Name);
        }
    }
}
