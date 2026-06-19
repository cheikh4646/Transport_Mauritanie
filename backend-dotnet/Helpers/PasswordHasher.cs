using Microsoft.AspNetCore.Identity;

namespace BackendDotnet.Helpers
{
    public static class PasswordHasher
    {
        private static readonly PasswordHasher<string> Hasher = new PasswordHasher<string>();

        public static string HashPassword(string password)
        {
            return Hasher.HashPassword(string.Empty, password);
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            var result = Hasher.VerifyHashedPassword(string.Empty, hashedPassword, password);
            return result == PasswordVerificationResult.Success;
        }
    }
}
