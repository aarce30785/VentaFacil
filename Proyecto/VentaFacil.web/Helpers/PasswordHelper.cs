using Microsoft.AspNetCore.Identity;

namespace VentaFacil.web.Helpers
{
    public static class PasswordHelper 
    {
        public static string HashPassword(string password)
        {
            var hasher = new PasswordHasher<string>();
            return hasher.HashPassword(null, password);
    
        }

        public static bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            var hasher = new PasswordHasher<string>();
            var result = hasher.VerifyHashedPassword(null, hashedPassword, providedPassword);
            return result == PasswordVerificationResult.Success;
        }
    }
}
