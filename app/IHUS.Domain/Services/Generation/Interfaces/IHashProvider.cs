namespace IHUS.Domain.Services.Generation.Interfaces
{
    public interface IHashProvider
    {
        public byte[] CalculateHash(byte[] input);
    }
}
