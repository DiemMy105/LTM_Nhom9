using System.Security.Cryptography;
using System.Text;

namespace ChatTCP.Server.Utils
{
    public static class SecurityUtils
    {
        // Băm mật khẩu 
        public static string HashPassword(string rawPassword)
        {
            if (string.IsNullOrEmpty(rawPassword))
                return string.Empty;

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(rawPassword);
                byte[] hashBytes = sha256.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // Kiểm tra mật khẩu
        public static bool VerifyPassword(string rawPassword, string hashedPassword)
        {
            if (string.IsNullOrEmpty(rawPassword) || string.IsNullOrEmpty(hashedPassword))
                return false;

            string hashedInput = HashPassword(rawPassword);
            return string.Equals(hashedInput, hashedPassword, StringComparison.OrdinalIgnoreCase);
        }
    }
}

