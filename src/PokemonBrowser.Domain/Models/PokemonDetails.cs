namespace PokemonBrowser.Domain.Models;

public sealed record PokemonDetails(
    int Id,
    string Name,
    string ImageUrl,
    int HeightDecimeters,
    int WeightHectograms,
    IReadOnlyList<string> Types,
    IReadOnlyList<PokemonStat> BaseStats);
