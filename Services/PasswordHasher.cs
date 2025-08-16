namespace MzansiMarket.Services
{
    using System.Security.Cryptography;

    public interface IPasswordHasher
    {
        (byte[] Hash, byte[] Salt) Hash(string password);
        bool Verify(string password, byte[] storedHash, byte[] storedSalt);
    }

    public class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16; // 128-bit
        private const int KeySize = 32;  // 256-bit
        private const int Iterations = 100_000; // PBKDF2 iterations

        public (byte[] Hash, byte[] Salt) Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(KeySize);
            return (hash, salt);
        }

        public bool Verify(string password, byte[] storedHash, byte[] storedSalt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, storedSalt, Iterations, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(KeySize);
            return CryptographicOperations.FixedTimeEquals(hash, storedHash);
        }
    }
}