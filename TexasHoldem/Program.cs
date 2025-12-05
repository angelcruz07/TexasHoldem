using System;
using System.Windows.Forms;

namespace TexasHoldem
{
    internal static class Program
    {
        public static User CurrentUser { get; set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Mostrar formulario de login primero
            LoginForm loginForm = new LoginForm();
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                CurrentUser = loginForm.CurrentUser;
                // Si el login es exitoso, mostrar el splash form
                Application.Run(new SplashForm());
            }
        }
    }
}
