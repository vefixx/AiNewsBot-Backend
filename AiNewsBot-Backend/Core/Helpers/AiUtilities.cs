namespace AiNewsBot_Backend.Core.Helpers;

public static class AiUtilities
{
    public static List<string> SplitTextIntoChunks(string text, int chunkSize = 2000)
    {
        var chunks = new List<string>();
    
        for (int i = 0; i < text.Length; i += chunkSize)
        {
            int length = Math.Min(chunkSize, text.Length - i);
            string chunk = text.Substring(i, length);
            chunks.Add(chunk);
        }
    
        return chunks;
    }
}