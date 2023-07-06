namespace IHUS.Domain.Services.Generation.Interfaces;

public interface ISaltProvider
{
    public byte[] GetSalt();
}
