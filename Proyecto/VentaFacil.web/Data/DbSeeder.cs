using Microsoft.Data.SqlClient;
using VentaFacil.web.Helpers;

namespace VentaFacil.web.Data
{
    public static class DbSeeder
    {
        public static void Seed(string connectionString)
        {
            using var context = new SqlConnection(connectionString);
            context.Open();

            // Crear roles si no existen
            string[] roles = { "Administrador", "Cajero" };
            foreach (var rol in roles)
            {
                using var checkRol = new SqlCommand(
                    "SELECT COUNT(*) FROM Rol WHERE Nombre_Rol = @rol", context);
                checkRol.Parameters.AddWithValue("@rol", rol);
                var rolExists = (int)checkRol.ExecuteScalar();

                if (rolExists == 0)
                {
                    using var insertRol = new SqlCommand(
                        "INSERT INTO Rol (Nombre_Rol, Descripcion) VALUES (@rol, @desc)", context);
                    insertRol.Parameters.AddWithValue("@rol", rol);
                    insertRol.Parameters.AddWithValue("@desc", rol == "Administrador" ?
                        "Acceso completo al sistema" : "Acceso a ventas y caja");
                    insertRol.ExecuteNonQuery();
                }
            }

            // Crear usuario admin si no existe
            using var checkCmd = new SqlCommand(
                "SELECT COUNT(*) FROM Usuario WHERE Correo = @correo", context);
            checkCmd.Parameters.AddWithValue("@correo", "admin@ventafacil.com");
            var exists = (int)checkCmd.ExecuteScalar();

            if (exists == 0)
            {
                string hashed = PasswordHelper.HashPassword("Admin123$");

                using var insertCmd = new SqlCommand(@"
                INSERT INTO Usuario (Nombre, Correo, Contrasena, Rol)
                VALUES (@nombre, @correo, @contrasena, 
                        (SELECT Id_Rol FROM Rol WHERE Nombre_Rol = 'Administrador'))", context);

                insertCmd.Parameters.AddWithValue("@nombre", "Administrador");
                insertCmd.Parameters.AddWithValue("@correo", "admin@ventafacil.com");
                insertCmd.Parameters.AddWithValue("@contrasena", hashed);

                insertCmd.ExecuteNonQuery();
            }
        }
    }
}
