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
                { "extraction_method", src.GetExtractionMethod() },
                { "top", src.GetTop() },
                { "left", src.GetLeft() },
                { "width", src.GetWidth() },
                { "height", src.GetHeight() },
                { "right", src.GetRight() },
                { "bottom", src.GetBottom() }
            };

            var data = JArray.FromObject(src.GetRows(), serializer);
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

        public override Table ReadJson(JsonReader reader, Type objectType, Table existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
