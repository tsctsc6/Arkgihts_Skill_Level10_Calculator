using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arkgihts_Skill_Level10_Calculator.Models;

[JsonSourceGenerationOptions(
    UseStringEnumConverter = true,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ResourceInfo))]
[JsonSerializable(typeof(Material))]
[JsonSerializable(typeof(DepotItem))]
[JsonSerializable(typeof(Depot))]
public partial class MyJsonSerializerContext : JsonSerializerContext
{
    static MyJsonSerializerContext()
    {
        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true,
            IndentSize = 4,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };
        Default = new MyJsonSerializerContext(options);
    }
}