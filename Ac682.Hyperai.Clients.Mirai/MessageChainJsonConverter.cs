using Ac682.Hyperai.Clients.Mirai.Serialization;
using Hyperai.Messages;
using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Ac682.Hyperai.Clients.Mirai
{
    public class MessageChainJsonConverter : JsonConverter<MessageChain>
    {
        static JsonParser parser = new JsonParser();
        static JsonFormatter formatter = new JsonFormatter();
        public override MessageChain ReadJson(JsonReader reader, Type objectType, [AllowNull] MessageChain existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var text = reader.ReadAsString();
            return parser.Parse(text);
        }

        public override void WriteJson(JsonWriter writer, [AllowNull] MessageChain value, JsonSerializer serializer)
        {
            var text = value != null ? formatter.Format(value) : null;
            if (text != null)
            {
                writer.WriteRawValue(text);
            }
        }
    }
}
