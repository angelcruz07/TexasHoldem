using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.Text;

namespace TexasHoldem
{
    public class DatabaseManager
    {
        private string connectionString;

        public DatabaseManager()
        {
            // Configuración de conexión - Ajusta estos valores según tu servidor MySQL
            string server = "localhost";
            string database = "texasholdem_db";
            string uid = "root";
            string password = ""; // Cambia esto por tu contraseña de MySQL
            
            connectionString = $"Server={server};Database={database};Uid={uid};Pwd={password};";
        }

        public DatabaseManager(string server, string database, string uid, string password)
        {
            connectionString = $"Server={server};Database={database};Uid={uid};Pwd={password};";
        }

        // Método para probar la conexión
        public bool TestConnection()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error de conexión: {ex.Message}", "Error", 
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return false;
            }
        }

        // Método para registrar un nuevo usuario
        public bool RegisterUser(string username, string email, string password)
        {
            try
            {
                // Verificar si el usuario ya existe
                if (UserExists(username, email))
                {
                    return false;
                }

                // Hash de la contraseña
                string hashedPassword = HashPassword(password);

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "INSERT INTO users (username, email, password_hash, created_at) VALUES (@username, @email, @password, NOW())";
                    
                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@password", hashedPassword);
                        
                        int result = cmd.ExecuteNonQuery();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error al registrar usuario: {ex.Message}", "Error", 
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return false;
            }
        }

        // Método para verificar login
        public User LoginUser(string username, string password)
        {
            try
            {
                string hashedPassword = HashPassword(password);

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT id, username, email, chips FROM users WHERE (username = @username OR email = @username) AND password_hash = @password";
                    
                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@password", hashedPassword);
                        
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new User
                                {
                                    Id = reader.GetInt32("id"),
                                    Username = reader.GetString("username"),
                                    Email = reader.GetString("email"),
                                    Chips = reader.GetInt32("chips")
                                };
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error al iniciar sesión: {ex.Message}", "Error", 
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return null;
            }
        }

        // Verificar si el usuario existe
        private bool UserExists(string username, string email)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM users WHERE username = @username OR email = @email";
                    
                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@email", email);
                        
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        // Actualizar fichas del usuario
        public bool UpdateUserChips(int userId, int chips)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "UPDATE users SET chips = @chips WHERE id = @userId";
                    
                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@chips", chips);
                        cmd.Parameters.AddWithValue("@userId", userId);
                        
                        int result = cmd.ExecuteNonQuery();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error al actualizar fichas: {ex.Message}", "Error", 
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return false;
            }
        }

        // Hash de contraseña usando SHA256
        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // Crear la base de datos y tabla si no existen
        public bool InitializeDatabase()
        {
            try
            {
                // Primero conectarse sin especificar la base de datos
                string server = connectionString.Split(';')[0].Split('=')[1];
                string uid = connectionString.Split(';')[2].Split('=')[1];
                string pwd = connectionString.Split(';')[3].Split('=')[1];
                string initConnectionString = $"Server={server};Uid={uid};Pwd={pwd};";

                using (MySqlConnection connection = new MySqlConnection(initConnectionString))
                {
                    connection.Open();
                    
                    // Crear base de datos si no existe
                    string createDbQuery = "CREATE DATABASE IF NOT EXISTS texasholdem_db";
                    using (MySqlCommand cmd = new MySqlCommand(createDbQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                // Ahora conectarse a la base de datos y crear la tabla
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    
                    string createTableQuery = @"
                        CREATE TABLE IF NOT EXISTS users (
                            id INT AUTO_INCREMENT PRIMARY KEY,
                            username VARCHAR(50) UNIQUE NOT NULL,
                            email VARCHAR(100) UNIQUE NOT NULL,
                            password_hash VARCHAR(64) NOT NULL,
                            chips INT DEFAULT 1000,
                            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                            last_login DATETIME NULL
                        )";
                    
                    using (MySqlCommand cmd = new MySqlCommand(createTableQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error al inicializar base de datos: {ex.Message}", "Error", 
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return false;
            }
        }
    }

    // Clase para representar un usuario
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public int Chips { get; set; }
    }
}

