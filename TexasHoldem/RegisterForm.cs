using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TexasHoldem
{
    public partial class RegisterForm : Form
    {
        private DatabaseManager dbManager;
        public User RegisteredUser { get; private set; }

        public RegisterForm()
        {
            InitializeComponent();
            dbManager = new DatabaseManager();
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;
            string confirmPassword = txtConfirmPassword.Text;

            // Validaciones
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || 
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                MessageBox.Show("Por favor, completa todos los campos.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (username.Length < 3)
            {
                MessageBox.Show("El nombre de usuario debe tener al menos 3 caracteres.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!IsValidEmail(email))
            {
                MessageBox.Show("Por favor, ingresa un email válido.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (password.Length < 6)
            {
                MessageBox.Show("La contraseña debe tener al menos 6 caracteres.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("Las contraseñas no coinciden.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Intentar registrar
            if (dbManager.RegisterUser(username, email, password))
            {
                MessageBox.Show("¡Registro exitoso! Ahora puedes iniciar sesión.", "Éxito", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // Intentar login automático
                RegisteredUser = dbManager.LoginUser(username, password);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("El usuario o email ya existe. Por favor, intenta con otros datos.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        private void RegisterForm_Load(object sender, EventArgs e)
        {
            this.AcceptButton = btnRegister;
        }
    }
}

