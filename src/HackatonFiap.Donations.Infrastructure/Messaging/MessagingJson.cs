using System.Text.Json;
using System.Text.Json.Serialization;

namespace HackatonFiap.Donations.Infrastructure.Messaging;

internal static class MessagingJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
