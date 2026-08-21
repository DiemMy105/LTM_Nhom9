using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChatTCP.Shared.Enums;
using ChatTCP.Shared.Models;

namespace ChatTCP.Shared.Network
{
    public static class MessageParser
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
        public static string Serialize(Message message)
        {
            if (message == null) return string.Empty;

            return JsonSerializer.Serialize(message, Options) + "\n";
        }
        public static Message? Deserialize(string rawData)
        {
            if (string.IsNullOrWhiteSpace(rawData)) return null;
            try
            {
                return JsonSerializer.Deserialize<Message>(rawData.Trim(), Options);
            }
            catch
            {
                return null;
            }
        }
 