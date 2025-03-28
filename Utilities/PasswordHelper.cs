using BCrypt.Net;

namespace SchedulediaryApi.Utilities
{
    public class PasswordHelper
    {
        /// <summary>
        /// 哈希密碼，使用 bcrypt 演算法。
        /// </summary>
        public static string HashPassword(string password, int workFactor = 12)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException(nameof(password), "密碼不能為空");

            if (workFactor < 4 || workFactor > 31)
                throw new ArgumentOutOfRangeException(nameof(workFactor), "工作因子必須在 4 到 31 之間");

            return BCrypt.Net.BCrypt.HashPassword(password, workFactor);
        }

        /// <summary>
        /// 驗證密碼是否與哈希值相符。
        /// </summary>
        public static bool VerifyPassword(string inputPassword, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(inputPassword))
                throw new ArgumentNullException(nameof(inputPassword), "輸入的密碼不能為空");

            if (string.IsNullOrWhiteSpace(storedHash))
                throw new ArgumentNullException(nameof(storedHash), "存儲的哈希值不能為空");

            try
            {
                return BCrypt.Net.BCrypt.Verify(inputPassword, storedHash);
            }
            catch (SaltParseException ex)
            {
                // 避免因 hash 格式錯誤導致系統崩潰
                Console.WriteLine("Hash 格式錯誤: " + ex.Message);
                return false;
            }
        }
    }
}