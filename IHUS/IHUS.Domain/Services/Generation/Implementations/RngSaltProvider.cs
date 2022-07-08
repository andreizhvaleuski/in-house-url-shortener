using IHUS.Domain.Services.Generation.Interfaces;
using System.Security.Cryptography;

namespace IHUS.Domain.Services.Generation.Implementations;

public sealed class RngSaltProvider : ISaltProvider
{
    private const int BufferSize = 64;

    private readonly Lazy<RandomNumberGenerator> _randomNumberGenerator;

    public RngSaltProvider()
    {
        _randomNumberGenerator = new Lazy<RandomNumberGenerator>(
            () => RandomNumberGenerator.Create(),
            true);
    }

    public byte[] GetSalt()
    {
        var buffer = new byte[BufferSize];

        _randomNumberGenerator.Value.GetBytes(buffer);

        return buffer;
    }
}
