using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Tabula.Json
{
    public class TableSerializer : JsonConverter<Table>
    {
        public static TableSerializer INSTANCE = new TableSerializer();

        public override bool CanRead => false;

        public override void WriteJson(JsonWriter writer, [AllowNull] Table src, JsonSerializer serializer)
        {
            if (src == null) return;

            JObject result = new JObject();

            result.Add("extraction_method", src.getExtractionMethod());
            result.Add("top", src.getTop());
            result.Add("left", src.getLeft());
            result.Add("width", src.getWidth());
            result.Add("height", src.getHeight());
            result.Add("right", src.getRight());
            result.Add("bottom", src.getBottom());

            var data = JArray.FromObject(src.getRows(), serializer);
            //JArray data = new JArray();
            //result.Add("data", data = new JArray());
            /*
            foreach (List<RectangularTextContainer> srcRow in src.getRows())
            {
                JArray row = new JArray();
                foreach (RectangularTextContainer textChunk in srcRow)
                {
                    row.Add(serializer.Serialize(writer, textChunk));
                }
                data.Add(row);
            }
            */

            result.Add("data", data);

            result.WriteTo(writer);
        }

        public override Table ReadJson(JsonReader reader, Type objectType, [AllowNull] Table existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
