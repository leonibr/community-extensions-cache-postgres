using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Threading.Tasks;

namespace WebSample
{
    public static class HelperExtensions
    {
        public static byte[] Serialize<T>(this T obj) where T : class
        {
            if (obj == null)
            {
                return null;
            }


            var bytes = JsonSerializer.SerializeToUtf8Bytes(obj);
           

            return bytes;

        }


        public static T DeSerialize<T>(this byte[] arrBytes) where T : class
        {

            var returnObject = JsonSerializer.Deserialize<T>(arrBytes);

            return returnObject;
        }

   
    }
}
