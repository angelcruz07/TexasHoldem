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
        private List<PictureBox> botCardPbs = new List<PictureBox>();
        private PictureBox pbHandRankingsGuide;
        
        // Cache para la imagen de backcard (evita cargarla múltiples veces)
        private static Image _cachedBackCardImage = null;
        
        // Flag para controlar si las cartas del bot deben mostrarse
        private bool _shouldRevealBotCards = false;

        public MesaDeJuegoForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
            // StartGame se llamará después de que el formulario esté completamente cargado
        }

        private void InitializeCustomComponents()
        {
            // Suspender el layout para evitar múltiples redibujos
            this.SuspendLayout();
            
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
                pb.Image = GetBackCardImage(); // Usar backcard.png como imagen inicial
                pb.BorderStyle = BorderStyle.FixedSingle;
                this.Controls.Add(pb);
                communityCardPbs.Add(pb);
            }

            // Bot Cards - En la parte superior, centradas horizontalmente
            int botCardY = 50; // Cerca de la parte superior
            int botCardsWidth = (2 * cardWidth) + cardSpacing;
            int botStartX = (this.ClientSize.Width - botCardsWidth) / 2;
            
            for (int i = 0; i < 2; i++)
            {
                PictureBox pb = new PictureBox();
                pb.Size = new Size(cardWidth, cardHeight);
                pb.Location = new Point(botStartX + (i * cardSpacing), botCardY);
                pb.SizeMode = PictureBoxSizeMode.StretchImage;
                pb.Image = GetBackCardImage(); // Inicialmente mostrar backcard
                pb.BorderStyle = BorderStyle.FixedSingle;
                this.Controls.Add(pb);
                botCardPbs.Add(pb);
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

            // Guía de rankings de póker - Esquina inferior derecha
            pbHandRankingsGuide = new PictureBox();
            pbHandRankingsGuide.Size = new Size(280, 400); // Tamaño más grande
            pbHandRankingsGuide.Location = new Point(this.ClientSize.Width - 300, this.ClientSize.Height - 420); // Esquina inferior derecha con margen
            pbHandRankingsGuide.SizeMode = PictureBoxSizeMode.Zoom; // Mantener proporción
            pbHandRankingsGuide.BorderStyle = BorderStyle.FixedSingle;
            pbHandRankingsGuide.BackColor = Color.White;
            pbHandRankingsGuide.Image = GetHandRankingsImage();
            this.Controls.Add(pbHandRankingsGuide);
            
            // Labels - Esquina derecha en medio (un poco a la izquierda)
            int rightSideX = this.ClientSize.Width - 200; // Movido 200 píxeles a la izquierda desde el borde derecho
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
            
            // Reanudar el layout después de agregar todos los controles
            this.ResumeLayout(false);
            this.PerformLayout();
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
                        // Si el bot se retiró, revelar sus cartas
                        if (_players.Count > 1 && _players[1].IsFolded)
                        {
                            _shouldRevealBotCards = true;
                        }
                        UpdateUI();
                    }));
                }
                else
                {
                    lblStatus.Text = $"Turno de: {playerName}";
                    // Si el bot se retiró, revelar sus cartas
                    if (_players.Count > 1 && _players[1].IsFolded)
                    {
                        _shouldRevealBotCards = true;
                    }
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
                        // Revelar cartas del bot cuando termina la ronda
                        _shouldRevealBotCards = true;
                        UpdateUI();
                        ShowWinnerDialog(result);
                        // Resetear flag y ocultar cartas para la siguiente mano
                        _shouldRevealBotCards = false;
                        // Ocultar cartas del bot para la nueva mano
                        if (botCardPbs.Count >= 2)
                        {
                            botCardPbs[0].Image = GetBackCardImage();
                            botCardPbs[1].Image = GetBackCardImage();
                        }
                        _gameEngine.StartGame(); // Auto start next hand for now
                    }));
                }
                else
                {
                    // Revelar cartas del bot cuando termina la ronda
                    _shouldRevealBotCards = true;
                    UpdateUI();
                    ShowWinnerDialog(result);
                    // Resetear flag y ocultar cartas para la siguiente mano
                    _shouldRevealBotCards = false;
                    // Ocultar cartas del bot para la nueva mano
                    if (botCardPbs.Count >= 2)
                    {
                        botCardPbs[0].Image = GetBackCardImage();
                        botCardPbs[1].Image = GetBackCardImage();
                    }
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
                    communityCardPbs[i].Image = GetBackCardImage(); // Mostrar backcard cuando no hay carta
                }
            }

            // Obtener jugadores
            Player humanPlayer = _players[0];
            Player botPlayer = _players.Count > 1 ? _players[1] : null;
            Player current = _gameEngine.CurrentPlayer;
            
            // Mostrar info del jugador humano
            lblPlayerName.Text = $"{humanPlayer.Name} {(humanPlayer.IsFolded ? "(RETIRADO)" : "")}";
            lblPlayerChips.Text = $"Fichas: ${humanPlayer.Chips}";

            // Mostrar cartas del jugador humano
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
            
            // Mostrar cartas del bot
            if (botPlayer != null && botCardPbs.Count >= 2)
            {
                // Revelar cartas si:
                // 1. El bot se retiró (folded)
                // 2. El jugador humano se retiró (para mostrar qué tenía el bot)
                // 3. Llegó al showdown (shouldRevealBotCards)
                // 4. Es el turno del bot y está tomando una decisión importante
                bool shouldReveal = _shouldRevealBotCards || 
                                   botPlayer.IsFolded || 
                                   humanPlayer.IsFolded ||
                                   (_gameEngine.CurrentPhase == RoundPhase.Showdown);
                
                if (shouldReveal && botPlayer.HoleCards.Count >= 2)
                {
                    // Mostrar las cartas reales del bot
                    botCardPbs[0].Image = GetCardImage(botPlayer.HoleCards[0]);
                    botCardPbs[1].Image = GetCardImage(botPlayer.HoleCards[1]);
                }
                else
                {
                    // Mostrar backcard mientras las cartas están ocultas
                    botCardPbs[0].Image = GetBackCardImage();
                    botCardPbs[1].Image = GetBackCardImage();
                }
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

        private Image GetBackCardImage()
        {
            // Usar la imagen cacheada si ya existe
            if (_cachedBackCardImage != null)
            {
                return _cachedBackCardImage;
            }
            
            // Cargar la imagen de backcard.png desde assets/
            string[] possiblePaths = new string[]
            {
                Path.Combine(Application.StartupPath, "assets", "backcard.png"),
                Path.Combine(Application.StartupPath, "..", "..", "assets", "backcard.png")
            };

            foreach(var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    // Cargar la imagen y redimensionarla al tamaño estándar de las cartas
                    Image originalImage = Image.FromFile(path);
                    int cardWidth = 70;
                    int cardHeight = 100;
                    
                    // Crear una nueva imagen redimensionada
                    Bitmap resizedImage = new Bitmap(cardWidth, cardHeight);
                    using (Graphics g = Graphics.FromImage(resizedImage))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(originalImage, 0, 0, cardWidth, cardHeight);
                    }
                    
                    // Liberar la imagen original
                    originalImage.Dispose();
                    
                    // Cachear la imagen para uso futuro
                    _cachedBackCardImage = resizedImage;
                    
                    return resizedImage;
                }
            }

            return null; // Imagen no encontrada
        }

        private Image GetHandRankingsImage()
        {
            // Cargar la imagen de Poker-Hand-Rankings.png desde assets/
            string[] possiblePaths = new string[]
            {
                Path.Combine(Application.StartupPath, "assets", "Poker-Hand-Rankings.png"),
                Path.Combine(Application.StartupPath, "..", "..", "assets", "Poker-Hand-Rankings.png")
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
                
                Player humanPlayer = _players[0];
                int currentBet = _gameEngine.CurrentBet;
                int playerCurrentBet = humanPlayer.CurrentBet;
                int callAmount = currentBet - playerCurrentBet;
                
                // Raise mínimo: igualar + raise mínimo (2x la apuesta actual o mínimo 40)
                int minRaise = Math.Max(currentBet * 2, 40);
                int maxRaise = humanPlayer.Chips + playerCurrentBet; // Todo lo que tiene
                
                // Si el raise mínimo es mayor que lo que tiene, hacer all-in
                if (minRaise > maxRaise)
                {
                    _gameEngine.ProcessAction("call", 0); // Solo igualar si no puede hacer raise mínimo
                    return;
                }
                
                // Calcular raise: mínimo entre minRaise y maxRaise
                int raiseAmount = Math.Min(minRaise, maxRaise);
                
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
                int cardHeight = 100;
                int cardSpacing = 80;
                int totalCardsWidth = (5 * cardWidth) + (4 * cardSpacing);
                int startX = (this.ClientSize.Width - totalCardsWidth) / 2 + 100;
                int startY = this.ClientSize.Height / 2 - 100;
                
                for (int i = 0; i < communityCardPbs.Count; i++)
                {
                    communityCardPbs[i].Location = new Point(startX + (i * cardSpacing), startY);
                }
                
                // Reposicionar guía de rankings (esquina inferior derecha)
                if (pbHandRankingsGuide != null)
                {
                    pbHandRankingsGuide.Size = new Size(280, 400); // Tamaño más grande
                    pbHandRankingsGuide.Location = new Point(this.ClientSize.Width - 300, this.ClientSize.Height - 420);
                }
                
                // Reposicionar cartas del bot (parte superior)
                int botCardY = 50;
                int botCardsWidth = (2 * cardWidth) + cardSpacing;
                int botStartX = (this.ClientSize.Width - botCardsWidth) / 2;
                for (int i = 0; i < botCardPbs.Count; i++)
                {
                    botCardPbs[i].Location = new Point(botStartX + (i * cardSpacing), botCardY);
                }
                
                // Reposicionar cartas del jugador (parte inferior)
                int playerCardY = this.ClientSize.Height - 200;
                int playerCardsWidth = (2 * cardWidth) + cardSpacing;
                int playerStartX = (this.ClientSize.Width - playerCardsWidth) / 2;
                for (int i = 0; i < playerCardPbs.Count; i++)
                {
                    playerCardPbs[i].Location = new Point(playerStartX + (i * cardSpacing), playerCardY);
                }
                
                // Reposicionar labels
                int rightSideX = this.ClientSize.Width - 700; // Movido 200 píxeles a la izquierda desde el borde derecho
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

        private void ShowWinnerDialog(string result)
        {
            // Parsear el resultado: "Nombre|Pot|tipo|mano"
            string[] parts = result.Split('|');
            string winnerName = parts.Length > 0 ? parts[0] : "Desconocido";
            string potStr = parts.Length > 1 ? parts[1] : "0";
            string winType = parts.Length > 2 ? parts[2] : "showdown";
            string handName = parts.Length > 3 ? parts[3] : "";

            int pot = 0;
            int.TryParse(potStr, out pot);

            // Crear formulario personalizado para mostrar el ganador
            Form winnerForm = new Form();
            winnerForm.Text = "Resultado de la Mano";
            winnerForm.Size = new Size(400, 250);
            winnerForm.StartPosition = FormStartPosition.CenterParent;
            winnerForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            winnerForm.MaximizeBox = false;
            winnerForm.MinimizeBox = false;
            winnerForm.ShowInTaskbar = false;

            // Panel principal
            Panel mainPanel = new Panel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.BackColor = Color.FromArgb(30, 30, 30);

            // Label del ganador
            Label lblWinner = new Label();
            lblWinner.Text = winType == "tie" ? "¡EMPATE!" : $"¡{winnerName} GANA!";
            lblWinner.Font = new Font("Segoe UI", 24, FontStyle.Bold);
            lblWinner.ForeColor = winType == "tie" ? Color.Gold : Color.LimeGreen;
            lblWinner.AutoSize = false;
            lblWinner.Size = new Size(380, 50);
            lblWinner.Location = new Point(10, 20);
            lblWinner.TextAlign = ContentAlignment.MiddleCenter;

            // Label del bote
            Label lblPot = new Label();
            lblPot.Text = winType == "tie" ? $"Bote dividido: ${pot} cada uno" : $"Bote: ${pot}";
            lblPot.Font = new Font("Segoe UI", 14, FontStyle.Regular);
            lblPot.ForeColor = Color.White;
            lblPot.AutoSize = false;
            lblPot.Size = new Size(380, 30);
            lblPot.Location = new Point(10, 80);
            lblPot.TextAlign = ContentAlignment.MiddleCenter;

            // Label del tipo de victoria
            Label lblWinType = new Label();
            if (winType == "fold")
            {
                lblWinType.Text = "Otros jugadores se retiraron";
            }
            else if (winType == "showdown")
            {
                lblWinType.Text = $"Mejor mano: {handName}";
            }
            else if (winType == "tie")
            {
                lblWinType.Text = $"Mejor mano: {handName}";
            }
            else
            {
                lblWinType.Text = "";
            }
            lblWinType.Font = new Font("Segoe UI", 12, FontStyle.Italic);
            lblWinType.ForeColor = Color.LightGray;
            lblWinType.AutoSize = false;
            lblWinType.Size = new Size(380, 30);
            lblWinType.Location = new Point(10, 120);
            lblWinType.TextAlign = ContentAlignment.MiddleCenter;

            // Botón OK
            Button btnOK = new Button();
            btnOK.Text = "Continuar";
            btnOK.Size = new Size(120, 35);
            btnOK.Location = new Point(140, 170);
            btnOK.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btnOK.BackColor = Color.FromArgb(0, 120, 215);
            btnOK.ForeColor = Color.White;
            btnOK.FlatStyle = FlatStyle.Flat;
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Click += (s, e) => winnerForm.Close();

            mainPanel.Controls.Add(lblWinner);
            mainPanel.Controls.Add(lblPot);
            mainPanel.Controls.Add(lblWinType);
            mainPanel.Controls.Add(btnOK);
            winnerForm.Controls.Add(mainPanel);

            winnerForm.ShowDialog(this);
        }
    }
}
