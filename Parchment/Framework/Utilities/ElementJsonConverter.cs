using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Xml.Linq;


namespace Parchment.Framework.Utilities
{
    public class ElementJsonConverter : JsonConverter
    {
        private static readonly Dictionary<ElementType, Type> ELEMENT_TYPE_MAP = new Dictionary<ElementType, Type>()
        {
            { ElementType.Title, typeof(TitleElementData) },
            { ElementType.Heading, typeof(HeadingElementData) },
            { ElementType.Paragraph, typeof(ParagraphElementData) },
            { ElementType.Image, typeof(ImageElementData) },
            { ElementType.Panel, typeof(PanelElementData) },
        };

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ElementData);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            JObject obj = JObject.Load(reader);
            JToken typeToken = obj.GetValue("Type", StringComparison.OrdinalIgnoreCase);

            ElementType elementType = ElementType.Unknown;
            if (typeToken != null)
            {
                try
                {
                    elementType = typeToken.ToObject<ElementType>(serializer);
                }
                catch (Exception)
                {
                    // TODO: Log unconverted Type here
                }
            }

            if (ELEMENT_TYPE_MAP.TryGetValue(elementType, out Type targetType) is false)
            {
                return new UnknownElementData();
            }

            ElementData element = (ElementData)Activator.CreateInstance(targetType);

            // Need to do this to prevent recursive converting
            serializer.Populate(obj.CreateReader(), element);
            return element;
        }


        public override bool CanWrite => false;
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }

}