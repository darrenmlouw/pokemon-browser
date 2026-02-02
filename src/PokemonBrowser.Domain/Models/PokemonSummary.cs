namespace PokemonBrowser.Domain.Models;

public sealed record PokemonSummary(
    int Id,
    string Name,
    string SpriteUrl,
    IReadOnlyList<string> Types)
{
    public string PrimaryType => Types.Count > 0 ? Types[0] : string.Empty;
}
