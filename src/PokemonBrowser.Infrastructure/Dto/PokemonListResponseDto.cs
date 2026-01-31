using System.Text.Json.Serialization;

namespace PokemonBrowser.Infrastructure.Dto;

public sealed class PokemonListResponseDto
{
    [JsonPropertyName("results")]
    public List<PokemonListEntryDto> Results { get; set; } = [];
}

public sealed class PokemonListEntryDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
