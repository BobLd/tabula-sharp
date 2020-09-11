using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Tabula.Json
{
    public class RectangularTextContainerSerializer : JsonConverter<RectangularTextContainer>
    {
        public static RectangularTextContainerSerializer INSTANCE = new RectangularTextContainerSerializer();

        public override bool CanRead => false;

        public override void WriteJson(JsonWriter writer, [AllowNull] RectangularTextContainer src, JsonSerializer serializer)
        {
            if (src == null) return;

            JObject result = new JObject
            {
                { "top", src.getTop() },
                { "left", src.getLeft() },
                { "width", src.getWidth() },
                { "height", src.getHeight() },
                { "text", src.getText() }
            };
            result.WriteTo(writer);
        }

        public override RectangularTextContainer ReadJson(JsonReader reader, Type objectType, [AllowNull] RectangularTextContainer existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
