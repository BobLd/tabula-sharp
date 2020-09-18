using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Tabula.Json
{
    public class RectangularTextContainerSerializer : JsonConverter<RectangularTextContainer>
    {
        public static RectangularTextContainerSerializer INSTANCE = new RectangularTextContainerSerializer();

        public override bool CanRead => false;

        public override void WriteJson(JsonWriter writer, RectangularTextContainer src, JsonSerializer serializer)
        {
            if (src == null) return;

            JObject result = new JObject
            {
                { "top", src.Top },
                { "left", src.Left },
                { "width", src.Width },
                { "height", src.Height },
                { "text", src.GetText() }
            };
            result.WriteTo(writer);
        }

        public override RectangularTextContainer ReadJson(JsonReader reader, Type objectType, RectangularTextContainer existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
