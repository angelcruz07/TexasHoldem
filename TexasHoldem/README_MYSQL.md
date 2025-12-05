# Guía de Instalación MySQL para Texas Hold'em

## Paso 1: Instalar MySQL

1. Descarga MySQL desde: https://dev.mysql.com/downloads/installer/
2. Instala MySQL Server
3. Durante la instalación, configura una contraseña para el usuario `root`

## Paso 2: Instalar MySQL Connector para .NET

### Opción A: Usando NuGet Package Manager (Recomendado)

1. Abre Visual Studio
2. Click derecho en el proyecto → "Manage NuGet Packages"
3. Busca "MySql.Data"
4. Instala la versión 8.0.33 o superior

### Opción B: Descarga Manual

1. Descarga desde: https://dev.mysql.com/downloads/connector/net/
2. Instala el paquete
3. Agrega la referencia manualmente en el proyecto

## Paso 3: Configurar la Base de Datos

### Opción A: Usando el Script SQL

1. Abre MySQL Workbench o cualquier cliente MySQL
2. Ejecuta el archivo `database_setup.sql` que está en la raíz del proyecto
3. O ejecuta manualmente:

```sql
CREATE DATABASE IF NOT EXISTS texasholdem_db;
USE texasholdem_db;
CREATE TABLE IF NOT EXISTS users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(64) NOT NULL,
    chips INT DEFAULT 1000,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    last_login DATETIME NULL
);
```

### Opción B: La aplicación creará la base de datos automáticamente

La aplicación intentará crear la base de datos y tabla automáticamente la primera vez que se ejecute.

## Paso 4: Configurar la Cadena de Conexión

Edita el archivo `DatabaseManager.cs` y ajusta estos valores según tu configuración:

```csharp
string server = "localhost";      // Tu servidor MySQL
string database = "texasholdem_db"; // Nombre de la base de datos
string uid = "root";              // Tu usuario MySQL
string password = "tu_password";   // Tu contraseña MySQL
```

## Paso 5: Probar la Conexión

1. Ejecuta la aplicación
2. Deberías ver el formulario de Login
3. Si es la primera vez, haz clic en "Registrarse" para crear una cuenta
4. Luego inicia sesión con tus credenciales

## Solución de Problemas

### Error: "Unable to connect to any of the specified MySQL hosts"
- Verifica que MySQL Server esté ejecutándose
- Verifica que el servidor sea "localhost" o la IP correcta
- Verifica el puerto (por defecto es 3306)

### Error: "Access denied for user"
- Verifica el usuario y contraseña en `DatabaseManager.cs`
- Asegúrate de que el usuario tenga permisos para crear bases de datos

### Error: "Could not load file or assembly 'MySql.Data'"
- Instala el paquete NuGet MySql.Data
- O copia MySql.Data.dll a la carpeta bin/Debug

## Estructura de la Base de Datos

### Tabla: users
- `id`: ID único del usuario (auto-incrementable)
- `username`: Nombre de usuario (único)
- `email`: Email del usuario (único)
- `password_hash`: Hash SHA256 de la contraseña
- `chips`: Fichas del jugador (default: 1000)
- `created_at`: Fecha de creación de la cuenta
- `last_login`: Última vez que inició sesión

## Seguridad

- Las contraseñas se almacenan como hash SHA256 (no en texto plano)
- Se recomienda usar conexiones SSL en producción
- Considera usar parámetros preparados (ya implementado) para prevenir SQL injection

