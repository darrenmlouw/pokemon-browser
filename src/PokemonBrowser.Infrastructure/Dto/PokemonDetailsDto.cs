using System.Text.Json.Serialization;

namespace PokemonBrowser.Infrastructure.Dto;

public sealed class PokemonDetailsDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("weight")]
    public int Weight { get; set; }

    [JsonPropertyName("types")]
    public List<PokemonTypeSlotDto> Types { get; set; } = [];

    [JsonPropertyName("stats")]
    public List<PokemonStatSlotDto> Stats { get; set; } = [];

    [JsonPropertyName("sprites")]
    public PokemonSpritesDto? Sprites { get; set; }
}

public sealed class PokemonTypeSlotDto
{
    [JsonPropertyName("slot")]
    public int Slot { get; set; }

    [JsonPropertyName("type")]
    public NamedApiResourceDto? Type { get; set; }
}

public sealed class PokemonStatSlotDto
{
    [JsonPropertyName("base_stat")]
    public int BaseStat { get; set; }

    [JsonPropertyName("stat")]
    public NamedApiResourceDto? Stat { get; set; }
}

public sealed class NamedApiResourceDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public sealed class PokemonSpritesDto
{
    [JsonPropertyName("front_default")]
    public string? FrontDefault { get; set; }

    [JsonPropertyName("other")]
    public PokemonSpritesOtherDto? Other { get; set; }
}

public sealed class PokemonSpritesOtherDto
{
    [JsonPropertyName("official-artwork")]
    public PokemonOfficialArtworkDto? OfficialArtwork { get; set; }
}

public sealed class PokemonOfficialArtworkDto
{
    [JsonPropertyName("front_default")]
    public string? FrontDefault { get; set; }
}
