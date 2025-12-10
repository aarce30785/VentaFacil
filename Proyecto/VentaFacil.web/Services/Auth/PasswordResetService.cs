using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Models;
using VentaFacil.web.Services.Email;
using VentaFacil.web.Helpers;

namespace VentaFacil.web.Services.Auth
{
    public class PasswordResetService : IPasswordResetService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PasswordResetService> _logger;

        public PasswordResetService(ApplicationDbContext context, IEmailService emailService, IConfiguration configuration, ILogger<PasswordResetService> logger)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> RequestPasswordResetAsync(string email)
        {
            _logger.LogInformation($"Iniciando solicitud de restablecimiento para: {email}");

            var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.Correo == email);
            if (usuario == null)
            {
                _logger.LogWarning($"Usuario no encontrado con correo: {email}");
                // No revelamos si el correo existe o no por seguridad, pero retornamos false internamente o true genérico
                // En este caso retornamos false para manejar el mensaje en el controller si queremos
                return false; 
            }

            // Generar token único
            var token = Guid.NewGuid().ToString();
            
            var resetToken = new PasswordResetToken
            {
                UsuarioId = usuario.Id_Usr,
                Token = token,
                ExpirationDate = DateTime.UtcNow.AddHours(24),
                IsUsed = false
            };

            _context.PasswordResetToken.Add(resetToken);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Token generado y guardado para usuario ID: {usuario.Id_Usr}");

            // Construir enlace
            var baseUrl = _configuration["AppUrl"] ?? "https://ventafacil-web.com";
            var resetLink = $"{baseUrl}/Login/RestablecerContrasena?token={token}";

            // Enviar correo
            string body = $@"
                <h2>Restablecer Contraseña</h2>
                <p>Se ha solicitado restablecer la contraseña para su cuenta.</p>
                <p>Haga clic en el siguiente enlace para continuar:</p>
                <p><a href='{resetLink}'>Restablecer Contraseña</a></p>
                <p>Si usted no solicitó esto, ignore este correo.</p>";

            try 
            {
                await _emailService.SendEmailAsync(email, "Restablecimiento de Contraseña - VentaFacil", body);
                _logger.LogInformation($"Correo enviado correctamente a {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error crítico enviando correo a {email}");
                // Opcional: Revertir token o ignorar
                throw;
            }

            return true;
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            var resetToken = await _context.PasswordResetToken
                .Include(rt => rt.Usuario)
                .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsUsed);

            if (resetToken == null || resetToken.ExpirationDate < DateTime.UtcNow)
            {
                return false;
            }

            // Actualizar contraseña
            resetToken.Usuario.Contrasena = PasswordHelper.HashPassword(newPassword);
            
            // Marcar token como usado
            resetToken.IsUsed = true;
            
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
