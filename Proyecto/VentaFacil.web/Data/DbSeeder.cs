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

            var categorias = new[]
            {
                new { Nombre = "Comida", Descripcion = "Categoria de comida" },
                new { Nombre = "Bebida", Descripcion = "Categoria de bebidas" },
                new { Nombre = "Otros", Descripcion = "otros productos" }
            };

            foreach (var categoria in categorias)
            {
                using var checkCategoria = new SqlCommand(
                    "SELECT COUNT(*) FROM Categoria WHERE Nombre = @nombre", context);
                checkCategoria.Parameters.AddWithValue("@nombre", categoria.Nombre);
                var categoriaExists = (int)checkCategoria.ExecuteScalar();

                if (categoriaExists == 0)
                {
                    using var insertCategoria = new SqlCommand(
                        "INSERT INTO Categoria (Nombre, Descripcion) VALUES (@nombre, @descripcion)", context);
                    insertCategoria.Parameters.AddWithValue("@nombre", categoria.Nombre);
                    insertCategoria.Parameters.AddWithValue("@descripcion", categoria.Descripcion);
                    insertCategoria.ExecuteNonQuery();
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

            // Crear producto si no existe
            using var checkProducto = new SqlCommand(
                "SELECT COUNT(*) FROM Producto WHERE Nombre = @nombre", context);
            checkProducto.Parameters.AddWithValue("@nombre", "Producto Demo");
            var productoExists = (int)checkProducto.ExecuteScalar();

            if (productoExists == 0)
            {
                // Obtener Id_Categoria para el producto demo
                using var getCategoriaId = new SqlCommand(
                    "SELECT TOP 1 Id_Categoria FROM Categoria WHERE Nombre = @nombreCat", context);
                getCategoriaId.Parameters.AddWithValue("@nombreCat", "Comida");
                var categoriaId = getCategoriaId.ExecuteScalar();

                using var insertProducto = new SqlCommand(@"
                    INSERT INTO Producto (Nombre, Descripcion, Precio, Imagen, StockMinimo, Estado, Id_Categoria)
                    VALUES (@nombre, @descripcion, @precio, @imagen, @stockMinimo, @estado, @idCategoria)", context);

                insertProducto.Parameters.AddWithValue("@nombre", "Producto Demo");
                insertProducto.Parameters.AddWithValue("@descripcion", "Producto de ejemplo para pruebas");
                insertProducto.Parameters.AddWithValue("@precio", 10.00m);
                insertProducto.Parameters.AddWithValue("@imagen", "");
                insertProducto.Parameters.AddWithValue("@stockMinimo", 5);
                insertProducto.Parameters.AddWithValue("@estado", true);
                insertProducto.Parameters.AddWithValue("@idCategoria", categoriaId ?? (object)DBNull.Value);

                insertProducto.ExecuteNonQuery();
            }
        }
    }
}
