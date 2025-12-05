using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace TexasHoldem
{
    public partial class MesaDeJuegoForm : Form
    {
        private GameEngine _gameEngine;
        private List<Player> _players;

        // UI Controls
        private Label lblPot;
        private Label lblStatus;
        private Label lblPlayerName;
        private Label lblPlayerChips;
        private Button btnFold;
        private Button btnCheck;
        private Button btnCall;
        private Button btnRaise;
        private List<PictureBox> communityCardPbs = new List<PictureBox>();
        private List<PictureBox> playerCardPbs = new List<PictureBox>();

        public MesaDeJuegoForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
            // StartGame se llamará después de que el formulario esté completamente cargado
        }

        private void InitializeCustomComponents()
        {
            // Setup Form
            this.Size = new Size(1024, 768);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Texas Hold'em";
            
            // Asegurar que el formulario esté completamente cargado antes de posicionar controles
            this.Load += MesaDeJuegoForm_Load;
            
            // Iniciar el juego después de que todo esté inicializado
            this.Shown += (s, args) => StartGame();
            
            // Community Cards - Un poco a la derecha del centro
            int cardWidth = 70;
            int cardHeight = 100;
            int cardSpacing = 80;
            int totalCardsWidth = (5 * cardWidth) + (4 * cardSpacing);
            int startX = (this.ClientSize.Width - totalCardsWidth) / 2 + 500; // Desplazado a la derecha
            int startY = this.ClientSize.Height / 2 - 300; // Centro vertical menos offset
            
            for (int i = 0; i < 5; i++)
            {
                PictureBox pb = new PictureBox();
                pb.Size = new Size(cardWidth, cardHeight);
                pb.Location = new Point(startX + (i * cardSpacing), startY);
                pb.SizeMode = PictureBoxSizeMode.StretchImage;
                pb.BackColor = Color.White; // Fondo blanco para PNGs
                pb.BorderStyle = BorderStyle.FixedSingle;
                this.Controls.Add(pb);
                communityCardPbs.Add(pb);
            }

            // Player Cards - Centradas horizontalmente
            int playerCardY = this.ClientSize.Height - 200; // Cerca del fondo
            int playerCardsWidth = (2 * cardWidth) + cardSpacing;
            int playerStartX = (this.ClientSize.Width - playerCardsWidth) / 2;
            
            for (int i = 0; i < 2; i++)
            {
                PictureBox pb = new PictureBox();
                pb.Size = new Size(cardWidth, cardHeight);
                pb.Location = new Point(playerStartX + (i * cardSpacing), playerCardY);
                pb.SizeMode = PictureBoxSizeMode.StretchImage;
                pb.BackColor = Color.White; // Fondo blanco para PNGs
                pb.BorderStyle = BorderStyle.FixedSingle;
                this.Controls.Add(pb);
                playerCardPbs.Add(pb);
            }

            // Labels - Esquina derecha en medio (un poco a la izquierda)
            int rightSideX = this.ClientSize.Width; 
            int rightSideY = this.ClientSize.Height / 2 - 150; // Centro vertical menos offset
            
            lblPot = new Label();
            lblPot.Text = "Bot: $0";
            lblPot.Font = new Font("Arial", 16, FontStyle.Bold);
            lblPot.Location = new Point(rightSideX, rightSideY);
            lblPot.AutoSize = true;
            lblPot.ForeColor = Color.White;
            lblPot.BackColor = Color.Transparent;
            this.Controls.Add(lblPot);

            lblStatus = new Label();
            lblStatus.Text = "Esperando inicio...";
            lblStatus.Font = new Font("Arial", 14);
            lblStatus.Location = new Point(rightSideX, rightSideY + 40);
            lblStatus.AutoSize = true;
            lblStatus.ForeColor = Color.White;
            lblStatus.BackColor = Color.Transparent;
            this.Controls.Add(lblStatus);

            lblPlayerName = new Label();
            lblPlayerName.Location = new Point(rightSideX, rightSideY + 80);
            lblPlayerName.AutoSize = true;
            lblPlayerName.Font = new Font("Arial", 12, FontStyle.Bold);
            lblPlayerName.ForeColor = Color.Yellow;
            lblPlayerName.BackColor = Color.Transparent;
            this.Controls.Add(lblPlayerName);

            lblPlayerChips = new Label();
            lblPlayerChips.Location = new Point(rightSideX, rightSideY + 110);
            lblPlayerChips.AutoSize = true;
            lblPlayerChips.Font = new Font("Arial", 12);
            lblPlayerChips.ForeColor = Color.White;
            lblPlayerChips.BackColor = Color.Transparent;
            this.Controls.Add(lblPlayerChips);

            // Buttons - Al centro abajo
            int btnY = this.ClientSize.Height - 80;
            int btnSpacing = 100;
            int totalBtnWidth = (4 * 90) + (3 * btnSpacing);
            int btnStartX = (this.ClientSize.Width - totalBtnWidth) / 2; // Centrado horizontalmente
            
            btnFold = CreateButton("Retirarse", btnStartX, btnY, BtnFold_Click);
            btnCheck = CreateButton("Pasar", btnStartX + 90 + btnSpacing, btnY, BtnCheck_Click);
            btnCall = CreateButton("Igualar", btnStartX + 2 * (90 + btnSpacing), btnY, BtnCall_Click);
            btnRaise = CreateButton("Subir", btnStartX + 3 * (90 + btnSpacing), btnY, BtnRaise_Click);
        }

        private Button CreateButton(string text, int x, int y, EventHandler onClick)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Location = new Point(x, y);
            btn.Size = new Size(90, 40);
            btn.Click += onClick;
            this.Controls.Add(btn);
            return btn;
        }

        private void StartGame()
        {
            // Initialize Players - Solo 1 jugador humano y 1 IA
            _players = new List<Player>
            {
                new Player("Jugador", 1000),
                new AIPlayer("Bot", 1000)
            };

            _gameEngine = new GameEngine(_players);
            
            // Subscribe to events
            _gameEngine.OnGameStateChanged += (s, e) => {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => UpdateUI()));
                }
                else
                {
                    UpdateUI();
                }
            };
            _gameEngine.OnTurnChanged += (s, playerName) => {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => {
                        lblStatus.Text = $"Turno de: {playerName}";
                        UpdateUI();
                    }));
                }
                else
                {
                    lblStatus.Text = $"Turno de: {playerName}";
                    UpdateUI();
                }
            };
            _gameEngine.OnPhaseChanged += (s, phase) => {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => {
                        lblStatus.Text = $"Fase: {phase}";
                        UpdateUI();
                    }));
                }
                else
                {
                    lblStatus.Text = $"Fase: {phase}";
                    UpdateUI();
                }
            };
            _gameEngine.OnRoundEnded += (s, result) => {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => {
                        MessageBox.Show(result);
                        _gameEngine.StartGame(); // Auto start next hand for now
                    }));
                }
                else
                {
                    MessageBox.Show(result);
                    _gameEngine.StartGame(); // Auto start next hand for now
                }
            };

            _gameEngine.StartGame();
        }

        private void UpdateUI()
        {
            // Pot
            lblPot.Text = $"Bote: ${_gameEngine.Pot} (Apuesta actual: ${_gameEngine.CurrentBet})";

            // Community Cards
            var community = _gameEngine.CommunityCards;
            for (int i = 0; i < 5; i++)
            {
                if (i < community.Count)
                {
                    communityCardPbs[i].Image = GetCardImage(community[i]);
                }
                else
                {
                    communityCardPbs[i].Image = null;
                }
            }

            // Obtener jugador humano (siempre el primero)
            Player humanPlayer = _players[0];
            Player current = _gameEngine.CurrentPlayer;
            
            // Mostrar info del jugador humano
            lblPlayerName.Text = $"{humanPlayer.Name} {(humanPlayer.IsFolded ? "(RETIRADO)" : "")}";
            lblPlayerChips.Text = $"Fichas: ${humanPlayer.Chips}";

            // Mostrar cartas SOLO del jugador humano (nunca del bot)
            if (humanPlayer.HoleCards.Count >= 2)
            {
                playerCardPbs[0].Image = GetCardImage(humanPlayer.HoleCards[0]);
                playerCardPbs[1].Image = GetCardImage(humanPlayer.HoleCards[1]);
            }
            else
            {
                playerCardPbs[0].Image = null;
                playerCardPbs[1].Image = null;
            }

            // Button Logic - Solo habilitar si es turno del jugador humano
            bool isHumanTurn = !_gameEngine.IsCurrentPlayerAI && !humanPlayer.IsFolded;
            
            if (isHumanTurn)
            {
                int callAmount = _gameEngine.CurrentBet - humanPlayer.CurrentBet;
                if (callAmount > 0)
                {
                    btnCall.Text = $"Igualar (${callAmount})";
                }
                else
                {
                    btnCall.Text = "Igualar";
                }
            }
            else
            {
                btnCall.Text = "Igualar";
            }
            
            btnFold.Enabled = isHumanTurn;
            btnCheck.Enabled = isHumanTurn && (humanPlayer.CurrentBet == _gameEngine.CurrentBet);
            btnCall.Enabled = isHumanTurn && (humanPlayer.CurrentBet < _gameEngine.CurrentBet) && (humanPlayer.Chips > 0);
            btnRaise.Enabled = isHumanTurn && (humanPlayer.Chips > 0);
        }

        private Image GetCardImage(Card card)
        {
            if (card == null) return null;
            // Ajustar ruta según estructura del proyecto
            // Intentar buscar en ../../assets/cards/ (Desarrollo) o assets/cards/ (Producción)
            string fileName = card.GetImageFileNameAlternativa();
            
            string[] possiblePaths = new string[]
            {
                Path.Combine(Application.StartupPath, "assets", "cards", fileName),
                Path.Combine(Application.StartupPath, "..", "..", "assets", "cards", fileName)
            };

            foreach(var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return Image.FromFile(path);
                }
            }

            return null; // Imagen no encontrada
        }

        // Event Handlers
        private void BtnFold_Click(object sender, EventArgs e)
        {
            try
            {
                if (_gameEngine == null) return;
                if (_gameEngine.IsCurrentPlayerAI) return;
                
                _gameEngine.ProcessAction("fold");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en Retirarse: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCheck_Click(object sender, EventArgs e)
        {
            try
            {
                if (_gameEngine == null) return;
                if (_gameEngine.IsCurrentPlayerAI) return;
                
                _gameEngine.ProcessAction("check");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en Pasar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCall_Click(object sender, EventArgs e)
        {
            try
            {
                if (_gameEngine == null) return;
                if (_gameEngine.IsCurrentPlayerAI) return;
                
                _gameEngine.ProcessAction("call");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en Igualar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRaise_Click(object sender, EventArgs e)
        {
            try
            {
                if (_gameEngine == null) return;
                if (_gameEngine.IsCurrentPlayerAI) return;
                
                // Simple raise logic: Raise 2x current bet or min bet
                int raiseAmount = _gameEngine.CurrentBet * 2; // Doble de la apuesta actual
                if (raiseAmount < 40) raiseAmount = 40; // Absolute min
                
                _gameEngine.ProcessAction("raise", raiseAmount);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en Subir: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MesaDeJuegoForm_Load(object sender, EventArgs e)
        {
            // Asegurar que los controles se posicionen correctamente después de que el formulario esté cargado
            // Esto es especialmente importante si el formulario está maximizado
            if (this.WindowState == FormWindowState.Maximized)
            {
                // Recalcular posiciones basadas en el tamaño real del cliente
                RepositionControls();
            }
        }
        
        private void RepositionControls()
        {
            // Recalcular posiciones de los controles basándose en el tamaño actual del cliente
            if (communityCardPbs.Count > 0)
            {
                int cardWidth = 70;
                int cardSpacing = 80;
                int totalCardsWidth = (5 * cardWidth) + (4 * cardSpacing);
                int startX = (this.ClientSize.Width - totalCardsWidth) / 2 + 100;
                int startY = this.ClientSize.Height / 2 - 100;
                
                for (int i = 0; i < communityCardPbs.Count; i++)
                {
                    communityCardPbs[i].Location = new Point(startX + (i * cardSpacing), startY);
                }
                
                // Reposicionar labels
                int rightSideX = this.ClientSize.Width - 280; // Movido a la izquierda unos 80 píxeles
                int rightSideY = this.ClientSize.Height / 2 - 150;
                
                if (lblPot != null) lblPot.Location = new Point(rightSideX, rightSideY);
                if (lblStatus != null) lblStatus.Location = new Point(rightSideX, rightSideY + 40);
                if (lblPlayerName != null) lblPlayerName.Location = new Point(rightSideX, rightSideY + 80);
                if (lblPlayerChips != null) lblPlayerChips.Location = new Point(rightSideX, rightSideY + 110);
                
                // Reposicionar botones
                int btnY = this.ClientSize.Height - 80;
                int btnSpacing = 100;
                int totalBtnWidth = (4 * 90) + (3 * btnSpacing);
                int btnStartX = (this.ClientSize.Width - totalBtnWidth) / 2;
                
                if (btnFold != null) btnFold.Location = new Point(btnStartX, btnY);
                if (btnCheck != null) btnCheck.Location = new Point(btnStartX + 90 + btnSpacing, btnY);
                if (btnCall != null) btnCall.Location = new Point(btnStartX + 2 * (90 + btnSpacing), btnY);
                if (btnRaise != null) btnRaise.Location = new Point(btnStartX + 3 * (90 + btnSpacing), btnY);
            }
        }
    }
}
