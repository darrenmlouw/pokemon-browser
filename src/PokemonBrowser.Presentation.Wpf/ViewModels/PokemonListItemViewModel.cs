using PokemonBrowser.Domain.Models;
using PokemonBrowser.Presentation.Wpf.Mvvm;

namespace PokemonBrowser.Presentation.Wpf.ViewModels;

public sealed class PokemonListItemViewModel : ObservableObject
{
    private IReadOnlyList<string> _types;

    public PokemonListItemViewModel(PokemonSummary model)
    {
        Id = model.Id;
        Name = model.Name;
        SpriteUrl = model.SpriteUrl;
        _types = model.Types;
    }

    public int Id { get; }

    public string Name { get; }

    public string DisplayName => Capitalize(Name);

    public string SpriteUrl { get; }

    public string PrimaryType => _types.Count > 0 ? Capitalize(_types[0]) : string.Empty;

    public string TypesDisplay => _types.Count == 0 ? string.Empty : string.Join(", ", _types.Select(Capitalize));

    public void EnrichTypes(IReadOnlyList<string> types)
    {
        _types = types;
        OnPropertyChanged(nameof(PrimaryType));
        OnPropertyChanged(nameof(TypesDisplay));
    }

    private static string Capitalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return char.ToUpperInvariant(value[0]) + value[1..];
    }
}
