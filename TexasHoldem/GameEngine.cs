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
                // Solo repartir cartas a jugadores con fichas
                if (p.Chips > 0)
                {
                    p.ReceiveCard(_deck.DealCard());
                    p.ReceiveCard(_deck.DealCard());
                }
                else
                {
                    // Si no tiene fichas, está fuera del juego
                    p.Fold();
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
                    _playersActedInRound++;
                    break;
                case "check":
                    // Solo válido si CurrentBet == player.CurrentBet
                    if (CurrentBet != player.CurrentBet)
                    {
                        // Invalid action, should call instead
                        // Mostrar mensaje de error pero no procesar la acción
                        System.Windows.Forms.MessageBox.Show(
                            "No puedes pasar. Debes igualar la apuesta actual primero.", 
                            "Acción Inválida", 
                            System.Windows.Forms.MessageBoxButtons.OK, 
                            System.Windows.Forms.MessageBoxIcon.Warning);
                        UpdateUI(); // Actualizar UI aunque la acción sea inválida
                        return; // No avanzar turno si la acción es inválida
                    }
                    _playersActedInRound++;
                    break;
                case "call":
                    int callAmount = CurrentBet - player.CurrentBet;
                    if (callAmount > 0)
                    {
                        PlaceBet(player, callAmount);
                    }
                    _playersActedInRound++;
                    break;
                case "raise":
                    // Raise: amount es el total que quiere apostar
                    if (amount <= CurrentBet)
                    {
                        // Invalid raise amount, debe ser mayor que CurrentBet
                        System.Windows.Forms.MessageBox.Show(
                            $"La apuesta debe ser mayor que ${CurrentBet}. La apuesta mínima es ${CurrentBet + 1}.", 
                            "Apuesta Inválida", 
                            System.Windows.Forms.MessageBoxButtons.OK, 
                            System.Windows.Forms.MessageBoxIcon.Warning);
                        UpdateUI(); // Actualizar UI aunque la acción sea inválida
                        return; // No avanzar turno si la acción es inválida
                    }
                    int raiseDiff = amount - player.CurrentBet;
                    PlaceBet(player, raiseDiff);
                    CurrentBet = amount;
                    // Al hacer raise, reiniciamos el conteo porque otros deben responder
                    _playersActedInRound = 1; // El que hizo raise ya actuó
                    break;
            }
            
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
            
            var activePlayers = _players.Where(p => !p.IsFolded && !p.IsAllIn).ToList();
            var allInPlayers = _players.Where(p => !p.IsFolded && p.IsAllIn).ToList();
            
            // Verificar si todos los jugadores activos (no all-in) tienen la misma apuesta que CurrentBet
            bool allBetsEqual = activePlayers.Count == 0 || 
                               activePlayers.All(p => p.CurrentBet == CurrentBet);
            
            // Verificar si todos los jugadores activos han actuado en esta ronda
            // Los jugadores all-in no necesitan actuar, pero deben haber igualado antes de ir all-in
            bool allActed = activePlayers.Count == 0 || _playersActedInRound >= activePlayers.Count;

            // Si todos actuaron y las apuestas están igualadas (o todos están all-in)
            if (allBetsEqual && allActed)
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
                OnRoundEnded?.Invoke(this, $"{winner.Name}|{Pot}|fold");
            }
            else
            {
                // Showdown logic - Evaluar manos de todos los jugadores activos
                var activePlayers = _players.Where(p => !p.IsFolded && p.HoleCards.Count == 2).ToList();
                if (activePlayers.Count == 0)
                {
                    // Si no hay jugadores activos, no hay ganador
                    OnRoundEnded?.Invoke(this, "Empate|0|showdown");
                    return;
                }

                // Evaluar la mejor mano de cada jugador
                var playerHands = activePlayers.Select(p => new
                {
                    Player = p,
                    Hand = HandEvaluator.EvaluateHand(p.HoleCards, CommunityCards)
                }).ToList();

                // Encontrar la mejor mano
                var bestHand = playerHands.OrderByDescending(ph => ph.Hand).First();
                var winners = playerHands.Where(ph => ph.Hand.CompareTo(bestHand.Hand) == 0).ToList();

                if (winners.Count == 1)
                {
                    // Un solo ganador
                    winners[0].Player.Chips += Pot;
                    string handName = bestHand.Hand.GetHandName();
                    OnRoundEnded?.Invoke(this, $"{winners[0].Player.Name}|{Pot}|showdown|{handName}");
                }
                else
                {
                    // Empate - dividir el bote equitativamente
                    int potPerPlayer = Pot / winners.Count;
                    int remainder = Pot % winners.Count; // Fichas restantes por división entera
                    
                    // Distribuir la parte base del bote a todos los ganadores
                    foreach (var w in winners)
                    {
                        w.Player.Chips += potPerPlayer;
                    }
                    
                    // Distribuir el resto (si hay) a los primeros ganadores
                    // Esto asegura que todas las fichas se distribuyan sin pérdida
                    for (int i = 0; i < remainder; i++)
                    {
                        winners[i].Player.Chips += 1;
                    }
                    
                    int totalDistributed = (potPerPlayer * winners.Count) + remainder;
                    string handName = bestHand.Hand.GetHandName();
                    string winnersNames = string.Join(", ", winners.Select(w => w.Player.Name));
                    OnRoundEnded?.Invoke(this, $"{winnersNames}|{potPerPlayer}|tie|{handName}");
                }
            }

            // Preparar siguiente mano
            _dealerIndex = (_dealerIndex + 1) % _players.Count;
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
