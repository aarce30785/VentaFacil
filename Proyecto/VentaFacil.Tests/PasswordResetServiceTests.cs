using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using VentaFacil.web.Data;
using VentaFacil.web.Models;
using VentaFacil.web.Services.Auth;
using VentaFacil.web.Services.Email;
using Xunit;

namespace VentaFacil.Tests
{
    public class PasswordResetServiceTests
    {
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<PasswordResetService>> _mockLogger;
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public PasswordResetServiceTests()
        {
            _mockEmailService = new Mock<IEmailService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<PasswordResetService>>();
            
            // Configurar base de datos en memoria para cada test
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            // Mock configuración base
            _mockConfiguration.Setup(c => c["AppUrl"]).Returns("http://localhost");
        }

        private ApplicationDbContext CreateContext()
        {
            return new ApplicationDbContext(_options);
        }

        [Fact]
        public async Task RequestPasswordResetAsync_UsuarioNoExiste_RetornaFalse()
        {
            // Arrange
            using var context = CreateContext();
            var service = new PasswordResetService(context, _mockEmailService.Object, _mockConfiguration.Object, _mockLogger.Object);

            // Act
            var result = await service.RequestPasswordResetAsync("noexiste@test.com");

            // Assert
            Assert.False(result);
            _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RequestPasswordResetAsync_UsuarioExiste_EnviaCorreoYRetornaTrue()
        {
            // Arrange
            using var context = CreateContext();
            var usuario = new Usuario { Nombre = "Test", Correo = "test@test.com", Contrasena = "123", Rol = 1 };
            context.Usuario.Add(usuario);
            await context.SaveChangesAsync();

            var service = new PasswordResetService(context, _mockEmailService.Object, _mockConfiguration.Object, _mockLogger.Object);

            // Act
            var result = await service.RequestPasswordResetAsync("test@test.com");

            // Assert
            Assert.True(result);
            
            // Verificar que se creó el token
            var token = await context.PasswordResetToken.FirstOrDefaultAsync(t => t.UsuarioId == usuario.Id_Usr);
            Assert.NotNull(token);
            Assert.False(token.IsUsed);

            // Verificar envío de correo
            _mockEmailService.Verify(x => x.SendEmailAsync("test@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_TokenValido_CambiaContrasena()
        {
            // Arrange
            using var context = CreateContext();
            var usuario = new Usuario { Nombre = "Test", Correo = "test@test.com", Contrasena = "OldPass", Rol = 1 };
            context.Usuario.Add(usuario);
            await context.SaveChangesAsync();

            var token = new PasswordResetToken
            {
                UsuarioId = usuario.Id_Usr,
                Token = "valid-token",
                ExpirationDate = DateTime.UtcNow.AddHours(1),
                IsUsed = false
            };
            context.PasswordResetToken.Add(token);
            await context.SaveChangesAsync();

            var service = new PasswordResetService(context, _mockEmailService.Object, _mockConfiguration.Object, _mockLogger.Object);

            // Act
            var result = await service.ResetPasswordAsync("valid-token", "NewPass");

            // Assert
            Assert.True(result);

            var usuarioActualizado = await context.Usuario.FindAsync(usuario.Id_Usr);
            // Nota: En la realidad se hashea, aquí verificamos que camibo. 
            // Como el servicio usa PasswordHelper.HashPassword, deberíamos observar que NO es "NewPass" pelado y NO es "OldPass".
            Assert.NotEqual("OldPass", usuarioActualizado.Contrasena);
            // El token debe estar usado
            var tokenActualizado = await context.PasswordResetToken.FirstOrDefaultAsync(t => t.Token == "valid-token");
            Assert.True(tokenActualizado.IsUsed);
        }

        [Fact]
        public async Task ResetPasswordAsync_TokenInvalido_RetornaFalse()
        {
            // Arrange
            using var context = CreateContext();
            var service = new PasswordResetService(context, _mockEmailService.Object, _mockConfiguration.Object, _mockLogger.Object);

            // Act
            var result = await service.ResetPasswordAsync("invalid-token", "NewPass");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ResetPasswordAsync_TokenExpirado_RetornaFalse()
        {
            // Arrange
            using var context = CreateContext();
            var usuario = new Usuario { Nombre = "Test", Correo = "test@test.com", Contrasena = "OldPass", Rol = 1 };
            context.Usuario.Add(usuario);
            
            var token = new PasswordResetToken
            {
                UsuarioId = usuario.Id_Usr,
                Token = "expired-token",
                ExpirationDate = DateTime.UtcNow.AddHours(-1), // Expirado
                IsUsed = false
            };
            context.PasswordResetToken.Add(token);
            await context.SaveChangesAsync();

            var service = new PasswordResetService(context, _mockEmailService.Object, _mockConfiguration.Object, _mockLogger.Object);

            // Act
            var result = await service.ResetPasswordAsync("expired-token", "NewPass");

            // Assert
            Assert.False(result);
            
            // Password no cambia
            var usuarioDb = await context.Usuario.FindAsync(usuario.Id_Usr);
            Assert.Equal("OldPass", usuarioDb.Contrasena);
        }
    }
}
