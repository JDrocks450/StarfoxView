using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StarFox.Interop.MISC
{
    public static class StrongTypeSerialization
    {
        public class StrongTypeSerializationObject
        {
            public StrongTypeSerializationObject(string typeName, string serializedData)
            {
                TypeName = typeName;
                SerializedData = serializedData;
            }

            public string TypeName { get; }
            public string SerializedData { get; }
        }
        public static async Task SerializeObject(Stream Stream, object Object)
        {
            var text = JsonSerializer.Serialize(Object, Object.GetType());
            var obj = new StrongTypeSerializationObject(
                Object.GetType().AssemblyQualifiedName,
                text
            );
            await JsonSerializer.SerializeAsync(Stream, obj);
        }
        public static void SerializeObjects(Stream Stream, params object[] Objects)
        {
            SerializeObjects(Stream, Objects);
        }
        public static async void SerializeObjects(Stream Stream, IEnumerable<object> Objects)
        {
            var sObjs = Objects.Select(x => new StrongTypeSerializationObject(
                x.GetType().AssemblyQualifiedName,
                JsonSerializer.Serialize(x, x.GetType())));
            await JsonSerializer.SerializeAsync(Stream, sObjs);
        }
        public static async Task<object> DeserializeObject(Stream Stream)
        {
            var sobj = await JsonSerializer.
                DeserializeAsync<StrongTypeSerializationObject>(Stream);
            List<object> resultList = new();
            var result = JsonSerializer.Deserialize(sobj.SerializedData, Type.GetType(sobj.TypeName));
            return result;
        }
        public static async Task<IEnumerable<object>> DeserializeObjects(Stream Stream)
        {
            var sObjs = await JsonSerializer.
                DeserializeAsync<IEnumerable<StrongTypeSerializationObject>>(Stream);
            List<object> resultList = new();
            foreach(var sobj in sObjs)
            {
                var result = JsonSerializer.Deserialize(sobj.SerializedData, Type.GetType(sobj.TypeName));
                if (result == null) continue;
                resultList.Add(result);
            }
            return resultList;
        }
    }
}
