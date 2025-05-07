using System.Text.Json.Serialization;

namespace Arkgihts_Skill_Level10_Calculator.Models;

public class DepotItem
{
    public int Have { get; set; } = 0;
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class Depot
{
    [JsonPropertyName("@type")]
    public string Type { get; set; } = string.Empty;
    public DepotItem[] Items { get; set; } = [];
}