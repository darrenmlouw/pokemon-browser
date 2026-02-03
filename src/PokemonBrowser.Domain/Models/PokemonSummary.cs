namespace PokemonBrowser.Domain.Models;

public sealed record PokemonSummary(
    int Id,
    string Name,
    string SpriteUrl);
