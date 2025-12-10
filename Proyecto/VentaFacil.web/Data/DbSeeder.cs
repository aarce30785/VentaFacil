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

            // --- Productos de ejemplo ---
            var productosDemo = new[]
            {
                new { Nombre = "Hamburguesa Clásica", Descripcion = "Pan, carne, lechuga, tomate y salsa especial.", Precio = 3500m, Imagen = "", StockMinimo = 10, Estado = true, Categoria = "Comida" },
                new { Nombre = "Pizza Margarita", Descripcion = "Pizza con salsa de tomate, queso mozzarella y albahaca.", Precio = 4500m, Imagen = "", StockMinimo = 8, Estado = true, Categoria = "Comida" },
                new { Nombre = "Refresco Natural", Descripcion = "Refresco de frutas naturales.", Precio = 1200m, Imagen = "", StockMinimo = 20, Estado = true, Categoria = "Bebida" },
                new { Nombre = "Café Americano", Descripcion = "Café negro tradicional.", Precio = 1000m, Imagen = "", StockMinimo = 15, Estado = true, Categoria = "Bebida" },
                new { Nombre = "Postre Brownie", Descripcion = "Brownie de chocolate con nueces.", Precio = 2000m, Imagen = "", StockMinimo = 5, Estado = true, Categoria = "Otros" }
            };
            foreach (var prod in productosDemo)
            {
                using var checkProd = new SqlCommand("SELECT COUNT(*) FROM Producto WHERE Nombre = @nombre", context);
                checkProd.Parameters.AddWithValue("@nombre", prod.Nombre);
                var existsProd = (int)checkProd.ExecuteScalar();
                if (existsProd == 0)
                {
                    using var getCatId = new SqlCommand("SELECT TOP 1 Id_Categoria FROM Categoria WHERE Nombre = @nombreCat", context);
                    getCatId.Parameters.AddWithValue("@nombreCat", prod.Categoria);
                    var catId = getCatId.ExecuteScalar();
                    using var insertProd = new SqlCommand(@"
                        INSERT INTO Producto (Nombre, Descripcion, Precio, Imagen, StockMinimo, Estado, Id_Categoria)
                        VALUES (@nombre, @desc, @precio, @img, @stockMin, @estado, @catId)", context);
                    insertProd.Parameters.AddWithValue("@nombre", prod.Nombre);
                    insertProd.Parameters.AddWithValue("@desc", prod.Descripcion);
                    insertProd.Parameters.AddWithValue("@precio", prod.Precio);
                    insertProd.Parameters.AddWithValue("@img", prod.Imagen);
                    insertProd.Parameters.AddWithValue("@stockMin", prod.StockMinimo);
                    insertProd.Parameters.AddWithValue("@estado", prod.Estado);
                    insertProd.Parameters.AddWithValue("@catId", catId ?? (object)DBNull.Value);
                    insertProd.ExecuteNonQuery();
                }
            }

            // --- Inventario de ejemplo ---
            var inventariosDemo = new[]
            {
                new { Nombre = "Harina", Unidad = "Kg", StockActual = 50, StockMinimo = 10 },
                new { Nombre = "Queso Mozzarella", Unidad = "Kg", StockActual = 20, StockMinimo = 5 },
                new { Nombre = "Tomate", Unidad = "Kg", StockActual = 30, StockMinimo = 8 },
                new { Nombre = "Café Molido", Unidad = "Kg", StockActual = 15, StockMinimo = 3 },
                new { Nombre = "Azúcar", Unidad = "Kg", StockActual = 40, StockMinimo = 10 }
            };
            foreach (var inv in inventariosDemo)
            {
                using var checkInv = new SqlCommand("SELECT COUNT(*) FROM Inventario WHERE Nombre = @nombre", context);
                checkInv.Parameters.AddWithValue("@nombre", inv.Nombre);
                var existsInv = (int)checkInv.ExecuteScalar();
                if (existsInv == 0)
                {
                    // Convertir string a int del enum UnidadMedida
                    int unidadInt = Enum.TryParse<VentaFacil.web.Models.Enum.UnidadMedida>(inv.Unidad, out var unidadEnum) ? (int)unidadEnum : 0;
                    using var insertInv = new SqlCommand(@"
                        INSERT INTO Inventario (Nombre, UnidadMedida, StockActual, StockMinimo)
                        VALUES (@nombre, @unidad, @stockActual, @stockMinimo)", context);
                    insertInv.Parameters.AddWithValue("@nombre", inv.Nombre);
                    insertInv.Parameters.AddWithValue("@unidad", unidadInt);
                    insertInv.Parameters.AddWithValue("@stockActual", inv.StockActual);
                    insertInv.Parameters.AddWithValue("@stockMinimo", inv.StockMinimo);
                    insertInv.ExecuteNonQuery();
                }
            }
        }
    }
}
