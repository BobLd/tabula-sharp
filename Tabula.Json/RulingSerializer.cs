using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Tabula.Json
{
    [Obsolete("This class is unused (Aug 2017) and will be removed at some later point")]
    public class RulingSerializer : JsonConverter<Ruling>
    {
        public override bool CanRead => false;

        public override void WriteJson(JsonWriter writer, [AllowNull] Ruling value, JsonSerializer serializer)
        {
            // return null;
        }

        public override Ruling ReadJson(JsonReader reader, Type objectType, [AllowNull] Ruling existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
