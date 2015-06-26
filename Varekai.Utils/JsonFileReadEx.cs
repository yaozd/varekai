using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Varekai.Utils
{
    public static class JsonFileReadEx
    {
        public static IEnumerable<T> LoadListFromFile<T>(string path)
        {
            using (var stream = new StreamReader(path))
            {
                string json = stream.ReadToEnd();

                return JsonConvert.DeserializeObject<List<T>>(json);
            }
        }
    }
}