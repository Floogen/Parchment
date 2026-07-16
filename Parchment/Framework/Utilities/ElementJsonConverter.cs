using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Enums;
using System;
using System.Formats.Asn1;
using System.Xml.Linq;


namespace Parchment.Framework.Utilities
{
    public class ElementJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(PageElementData);
        }

        // Let default serialization handle writing — it already uses the runtime type,
        // so Title/Panel properties are written correctly without any custom code.
        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            JObject obj = JObject.Load(reader);

            PageElementType type = PageElementType.Unknown;
            JToken typeToken = obj.GetValue("Type", StringComparison.OrdinalIgnoreCase);
            if (typeToken != null)
            {
                try { type = typeToken.ToObject<PageElementType>(serializer); }
                catch (Exception) { /* leave as Unknown */ }
            }

            PageElementData result = type switch
            {
                PageElementType.Panel => new PanelElementData(),
                _ => new PageElementData()
            };

            // Need to do this to prevent recursive converting
            serializer.Populate(obj.CreateReader(), result);
            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }

}