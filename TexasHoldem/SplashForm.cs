using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TexasHoldem
{
    public partial class SplashForm : Form
    {
        private MesaDeJuegoForm _gameForm;
        
        public SplashForm()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            // Crear el formulario pero no mostrarlo aún
            _gameForm = new MesaDeJuegoForm();
            
            // Configurar el evento de cierre del formulario de juego para cerrar la aplicación
            _gameForm.FormClosed += (s, args) => Application.ExitThread();
            
            // Ocultar el splash primero para una transición más suave
            this.Hide();
            
            // Forzar actualización de la UI
            Application.DoEvents();
            
            // Mostrar el nuevo formulario
            _gameForm.Show();
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Si el formulario de juego está abierto, no cerrar la aplicación
            if (_gameForm != null && !_gameForm.IsDisposed && _gameForm.Visible)
            {
                // Solo ocultar este formulario, no cerrarlo
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                // Si no hay formulario de juego, cerrar normalmente
                base.OnFormClosing(e);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void SplashForm_Load(object sender, EventArgs e)
        {
            // Centrar el formulario en la pantalla
            this.StartPosition = FormStartPosition.CenterScreen;
            
            // Configurar labels con mejor legibilidad
            ConfigureLabelWithShadow(lblTitle);
            ConfigureLabelWithShadow(lblSubtitle);
        }

        private void ConfigureLabelWithShadow(Label label)
        {
            // Configurar el label para dibujo personalizado
            //label.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | 
            //              ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
            
            label.Paint += (s, e) =>
            {
                // Limpiar el área del label
                e.Graphics.Clear(label.BackColor);
                
                // Dibujar sombra negra semi-transparente (múltiples capas para efecto más fuerte)
                using (Brush shadowBrush = new SolidBrush(Color.FromArgb(200, Color.Black)))
                {
                    for (int offset = 2; offset <= 5; offset++)
                    {
                        e.Graphics.DrawString(label.Text, label.Font, shadowBrush,
                            new PointF(offset, offset));
                    }
                }
                // Dibujar texto principal encima
                using (Brush textBrush = new SolidBrush(label.ForeColor))
                {
                    StringFormat sf = new StringFormat();
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    e.Graphics.DrawString(label.Text, label.Font, textBrush, 
                        new RectangleF(0, 0, label.Width, label.Height), sf);
                }
            };
        }
    }
}
