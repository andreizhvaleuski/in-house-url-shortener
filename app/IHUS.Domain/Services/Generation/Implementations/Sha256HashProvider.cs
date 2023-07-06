using IHUS.Domain.Services.Generation.Interfaces;
using System.Security.Cryptography;

namespace IHUS.Domain.Services.Generation.Implementations;

public class Sha256HashProvider : IHashProvider
{
    public byte[] CalculateHash(byte[] input)
    {
        return SHA256.HashData(input);
    }
}
