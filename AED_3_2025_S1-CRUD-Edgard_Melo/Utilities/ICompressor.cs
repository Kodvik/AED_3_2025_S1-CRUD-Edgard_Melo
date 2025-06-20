namespace AED_3_2025_S1_CRUD_Edgard_Melo.Utilities
{
    public interface ICompressor
    {
        void Compress(string inputPath, string outputPath);
        void Decompress(string inputPath, string outputPath);
    }
}