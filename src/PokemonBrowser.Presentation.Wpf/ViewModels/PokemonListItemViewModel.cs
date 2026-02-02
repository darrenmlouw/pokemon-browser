using PokemonBrowser.Domain.Models;
using PokemonBrowser.Presentation.Wpf.Mvvm;
using System.Windows.Media;

namespace PokemonBrowser.Presentation.Wpf.ViewModels;

public sealed class PokemonListItemViewModel : ObservableObject
{
    private IReadOnlyList<string> _types;
    private ImageSource? _spriteImage;

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

    public ImageSource? SpriteImage
    {
        get => _spriteImage;
        private set => SetProperty(ref _spriteImage, value);
    }

    public string PrimaryType => _types.Count > 0 ? Capitalize(_types[0]) : string.Empty;

    public string TypesDisplay => _types.Count == 0 ? string.Empty : string.Join(", ", _types.Select(Capitalize));

    public void EnrichTypes(IReadOnlyList<string> types)
    {
        _types = types;
        OnPropertyChanged(nameof(PrimaryType));
        OnPropertyChanged(nameof(TypesDisplay));
    }

    public void SetSpriteImage(ImageSource? image)
    {
        SpriteImage = image;
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
