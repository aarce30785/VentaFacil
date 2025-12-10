namespace VentaFacil.web.Services.Auth
{
    public interface IPasswordResetService
    {
        Task<bool> RequestPasswordResetAsync(string email);
        Task<bool> ResetPasswordAsync(string token, string newPassword);
    }
}
