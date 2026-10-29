using System;
using System.Security.Cryptography;
using System.Text;

namespace LuminaFlow
{
    /// <summary>AES-256-CBC + encrypt-then-HMAC-SHA256. Client-held keys are not an anti-cheat boundary.</summary>
    public static class CryptoUtility
    {
        private const int SaltSize = 16, IvSize = 16, TagSize = 32, HeaderSize = 4 + SaltSize + IvSize;
        private const int Iterations = 100000;
        private static readonly byte[] Magic = { (byte)'L', (byte)'F', (byte)'S', (byte)'1' };

        public static byte[] Encrypt(byte[] plaintext, string secret)
        {
            if (plaintext == null) throw new ArgumentNullException("plaintext");
            RequireSecret(secret);
            byte[] salt = new byte[SaltSize], iv = new byte[IvSize];
            using (var random = RandomNumberGenerator.Create()) { random.GetBytes(salt); random.GetBytes(iv); }
            byte[] keys = Derive(secret, salt);
            try
            {
                byte[] encrypted;
                using (var aes = Aes.Create())
                {
                    Configure(aes, keys, iv);
                    using (var transform = aes.CreateEncryptor())
                        encrypted = transform.TransformFinalBlock(plaintext, 0, plaintext.Length);
                }
                byte[] output = new byte[HeaderSize + encrypted.Length + TagSize];
                Buffer.BlockCopy(Magic, 0, output, 0, Magic.Length);
                Buffer.BlockCopy(salt, 0, output, 4, SaltSize);
                Buffer.BlockCopy(iv, 0, output, 4 + SaltSize, IvSize);
                Buffer.BlockCopy(encrypted, 0, output, HeaderSize, encrypted.Length);
                using (var hmac = new HMACSHA256(Slice(keys, 32, 32)))
                {
                    byte[] tag = hmac.ComputeHash(output, 0, output.Length - TagSize);
                    Buffer.BlockCopy(tag, 0, output, output.Length - TagSize, TagSize);
                }
                return output;
            }
            finally { Array.Clear(keys, 0, keys.Length); }
        }

        public static byte[] Decrypt(byte[] envelope, string secret)
        {
            RequireSecret(secret);
            if (envelope == null || envelope.Length < HeaderSize + 16 + TagSize || (envelope.Length - HeaderSize - TagSize) % 16 != 0)
                throw new CryptographicException("Invalid save envelope.");
            for (int i = 0; i < Magic.Length; i++)
                if (envelope[i] != Magic[i]) throw new CryptographicException("Unsupported save format.");
            byte[] keys = Derive(secret, Slice(envelope, 4, SaltSize));
            try
            {
                using (var hmac = new HMACSHA256(Slice(keys, 32, 32)))
                {
                    byte[] expected = hmac.ComputeHash(envelope, 0, envelope.Length - TagSize);
                    int difference = 0;
                    for (int i = 0; i < TagSize; i++) difference |= expected[i] ^ envelope[envelope.Length - TagSize + i];
                    if (difference != 0) throw new CryptographicException("Save integrity check failed.");
                }
                using (var aes = Aes.Create())
                {
                    Configure(aes, keys, Slice(envelope, 4 + SaltSize, IvSize));
                    using (var transform = aes.CreateDecryptor())
                        return transform.TransformFinalBlock(envelope, HeaderSize, envelope.Length - HeaderSize - TagSize);
                }
            }
            finally { Array.Clear(keys, 0, keys.Length); }
        }

        public static string Encrypt(string plaintext, string secret)
        { return Convert.ToBase64String(Encrypt(JsonCodec.Utf8.GetBytes(plaintext), secret)); }
        public static string Decrypt(string envelope, string secret)
        { return JsonCodec.Utf8.GetString(Decrypt(Convert.FromBase64String(envelope), secret)); }

        private static byte[] Derive(string secret, byte[] salt)
        {
            using (var derive = new Rfc2898DeriveBytes(secret, salt, Iterations, HashAlgorithmName.SHA256))
                return derive.GetBytes(64);
        }
        private static void Configure(Aes aes, byte[] keys, byte[] iv)
        {
            aes.KeySize = 256; aes.BlockSize = 128; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
            aes.Key = Slice(keys, 0, 32); aes.IV = iv;
        }
        private static byte[] Slice(byte[] source, int offset, int count)
        { var bytes = new byte[count]; Buffer.BlockCopy(source, offset, bytes, 0, count); return bytes; }
        private static void RequireSecret(string secret)
        { if (string.IsNullOrEmpty(secret)) throw new ArgumentException("A save key is required.", "secret"); }
    }
}
