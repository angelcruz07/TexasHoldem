using System;
using System.Windows.Forms;

namespace TexasHoldem
{
    public partial class LoginForm : Form
    {
        private DatabaseManager dbManager;
        public User CurrentUser { get; private set; }

        public LoginForm()
        {
            InitializeComponent();
            dbManager = new DatabaseManager();
            
            // Inicializar base de datos si no existe
            dbManager.InitializeDatabase();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Por favor, completa todos los campos.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CurrentUser = dbManager.LoginUser(username, password);

            if (CurrentUser != null)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Usuario o contraseña incorrectos.", "Error de inicio de sesión", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPassword.Clear();
            }
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            RegisterForm registerForm = new RegisterForm();
            if (registerForm.ShowDialog() == DialogResult.OK)
            {
                // Si el registro fue exitoso, intentar login automático
                CurrentUser = registerForm.RegisteredUser;
                if (CurrentUser != null)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            this.AcceptButton = btnLogin;
        }
    }
}

