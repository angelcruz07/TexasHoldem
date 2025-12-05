-- Script SQL para crear la base de datos y tabla de usuarios
-- Ejecuta este script en MySQL antes de usar la aplicación

CREATE DATABASE IF NOT EXISTS texasholdem_db;

USE texasholdem_db;

CREATE TABLE IF NOT EXISTS users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(64) NOT NULL,
    chips INT DEFAULT 1000,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    last_login DATETIME NULL,
    INDEX idx_username (username),
    INDEX idx_email (email)
);

-- Opcional: Insertar un usuario de prueba (contraseña: "test123")
-- INSERT INTO users (username, email, password_hash) 
-- VALUES ('testuser', 'test@example.com', 'ecd71870d1963316a97e3ac3408c9835ad8cf0f3c1bc703527c30265534f75ae');

