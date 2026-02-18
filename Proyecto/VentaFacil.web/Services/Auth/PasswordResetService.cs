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
            
            // Activar usuario si estaba inactivo (para el flujo de creación de cuenta)
            if (!resetToken.Usuario.Estado)
            {
                resetToken.Usuario.Estado = true;
            }
            
            // Marcar token como usado
            resetToken.IsUsed = true;
            
            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<bool> SendActivationEmailAsync(string email)
        {
            _logger.LogInformation($"Iniciando solicitud de activación para: {email}");

            var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.Correo == email);
            if (usuario == null)
            {
                _logger.LogWarning($"Usuario no encontrado con correo: {email}");
                return false;
            }

            // Generar token único de activación (reutilizamos la tabla de reset tokens)
            var token = Guid.NewGuid().ToString();
            
            var resetToken = new PasswordResetToken
            {
                UsuarioId = usuario.Id_Usr,
                Token = token,
                ExpirationDate = DateTime.UtcNow.AddHours(48), // 48 horas para activar
                IsUsed = false
            };

            _context.PasswordResetToken.Add(resetToken);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Token de activación generado y guardado para usuario ID: {usuario.Id_Usr}");

            // Construir enlace de activación (usa la misma vista de restablecer contraseña pero el contexto es diferente)
            // Construir enlace de activación
            var baseUrl = _configuration["AppUrl"] ?? "https://ventafacil-web.com";
            var activationLink = $"{baseUrl}/Login/ActivarCuenta?token={token}";

            // Enviar correo de bienvenida
            string body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #4A6CF7;'>¡Bienvenido a VentaFacil!</h2>
                    <p>Se ha creado una cuenta para usted en el sistema.</p>
                    <p>Para completar su registro y activar su cuenta, por favor haga clic en el siguiente enlace y defina su contraseña:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{activationLink}' style='background-color: #4A6CF7; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; font-weight: bold;'>Activar Cuenta</a>
                    </div>
                    <p style='color: #666;'>Si el botón no funciona, copie y pegue el siguiente enlace en su navegador:</p>
                    <p style='color: #666; font-size: 12px;'>{activationLink}</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                    <p style='font-size: 12px; color: #999;'>Este enlace expirará en 48 horas.</p>
                </div>";

            try 
            {
                await _emailService.SendEmailAsync(email, "Bienvenido a VentaFacil - Active su Cuenta", body);
                _logger.LogInformation($"Correo de activación enviado correctamente a {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error crítico enviando correo de activación a {email}");
                throw;
            }

            return true;
        }
    }
}
