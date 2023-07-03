using IHUS.Domain.Services.Generation.Interfaces;
using System.Security.Cryptography;

namespace IHUS.Domain.Services.Generation.Implementations
{
    public class Sha256HashProvider : IHashProvider
    {
        public byte[] CalculateHash(byte[] input)
        {
            using var sha256 = SHA256.Create();

            return sha256.ComputeHash(input);
        }
    }
}
