using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Tabula.Json
{
    public class TableSerializer : JsonConverter<Table>
    {
        public static TableSerializer INSTANCE = new TableSerializer();

        public override bool CanRead => false;

        public override void WriteJson(JsonWriter writer, Table src, JsonSerializer serializer)
        {
            if (src == null) return;

            JObject result = new JObject
            {
                { "extraction_method", src.ExtractionMethod },
                { "top", src.Top },
                { "left", src.Left },
                { "width", src.Width },
                { "height", src.Height },
                { "right", src.Right },
                { "bottom", src.Bottom }
            };

            result.Add("data", JArray.FromObject(src.Rows, serializer));

            result.WriteTo(writer);
        }

        public override Table ReadJson(JsonReader reader, Type objectType, Table existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
