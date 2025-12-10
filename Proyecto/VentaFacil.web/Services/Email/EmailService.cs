using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace VentaFacil.web.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var host = emailSettings["Host"] ?? "localhost";
            var port = int.Parse(emailSettings["Port"] ?? "25");
            var from = emailSettings["From"] ?? "no-reply@ventafacil-web.com";

            _logger.LogInformation($"Intentando enviar correo a: {to} via SMTP: {host}:{port}");

            try
            {
                using (var client = new SmtpClient(host, port))
                {
                    // Configuraciones opcionales si usaras un SMTP externo con autenticación
                    // client.Credentials = new NetworkCredential("user", "pass");
                    // client.EnableSsl = true; 

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(from),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(to);

                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation($"Correo enviado exitosamente a {to}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error enviando correo a {to}");
                throw;
            }
        }
    }
}
