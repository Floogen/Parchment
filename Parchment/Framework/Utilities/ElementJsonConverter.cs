using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.UI.Rendering;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Xml.Linq;


namespace Parchment.Framework.Utilities
{
    public class ElementJsonConverter : JsonConverter
    {
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
                catch (Exception exception)
                {
                    Parchment.monitor.Log($"Unable to parse element type '{typeToken}': {exception.Message}", LogLevel.Warn);
                }
            }

            if (Parchment.bookManager.ElementRegistry.TryResolve(elementType, out ElementRegistration registration) is false)
            {
                if (elementType is not ElementType.Unknown)
                {
                    Parchment.monitor.LogOnce($"No renderer is registered for element type {elementType}; the element will be ignored.", LogLevel.Warn);
                }

                return new UnknownElementData();
            }

            ElementData element = (ElementData)Activator.CreateInstance(registration.Renderer.DataType);

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