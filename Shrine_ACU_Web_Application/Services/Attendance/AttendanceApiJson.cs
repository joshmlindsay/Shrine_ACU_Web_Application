using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shrine_ACU_Web_Application.Services.Attendance;

public static class AttendanceApiJson
{
    public static readonly JsonSerializerOptions SerializerOptions = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
