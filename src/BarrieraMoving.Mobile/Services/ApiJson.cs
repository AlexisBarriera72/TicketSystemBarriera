using System.Text.Json;
using System.Text.Json.Serialization;

namespace BarrieraMoving.Mobile.Services;

// Mismas convenciones JSON que el servidor: camelCase + enums como texto ("EnRoute")
public static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
