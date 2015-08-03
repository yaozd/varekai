using System;
using Newtonsoft.Json;
using System.IO;
using System.IO.Compression;

namespace Varekai.Utils
{
    public static class SerializationUtils
    {
        public static string GetJson(this byte[] binary)
        {
            return Convert.ToBase64String(binary);
        }

        public static string DecompressAndGetJson(this byte[] binary)
        {
            return Convert.ToBase64String(binary.Decompress());
        }

        public static T GetObject<T>(this byte[] binary)
        {
            return JsonConvert.DeserializeObject<T>(binary.GetJson());
        }

        public static T DecompressAndGetObject<T>(this byte[] binary)
        {
            return JsonConvert.DeserializeObject<T>(binary.DecompressAndGetJson());
        }

        static byte[] Decompress(this byte[] toDecompress)
        {
            return toDecompress.ApplyGzip(CompressionMode.Decompress);
        }

        static byte[] Compress(this byte[] toCompress)
        {
            return toCompress.ApplyGzip(CompressionMode.Compress);
        }

        static byte[] ApplyGzip(this byte[] toCompress, CompressionMode action)
        {
            using (var memory = new MemoryStream())
            using (var gzip = new GZipStream(memory, CompressionMode.Compress, true))
            {
                gzip.Write(toCompress, 0, toCompress.Length);
                return memory.ToArray();
            }
        }
    }
}

