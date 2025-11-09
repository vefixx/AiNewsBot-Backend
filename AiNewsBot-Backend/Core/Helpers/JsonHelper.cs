using Newtonsoft.Json;

namespace AiNewsBot_Backend.Core.Helpers;

public class JsonHelper
{
    public static T ReadJson<T>(string filePath)
    {
        T? result = JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
        if (result == null)
        {
            throw new JsonReaderException($"Результат десериализации контента по пути {filePath} равен null");
        }

        return result;
    }
}