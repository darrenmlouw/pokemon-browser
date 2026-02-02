using PokemonBrowser.Domain.Models;
using PokemonBrowser.Presentation.Wpf.Mvvm;
using System.Windows.Media;

namespace PokemonBrowser.Presentation.Wpf.ViewModels;

public sealed class PokemonDetailsViewModel : ObservableObject
{
    private ImageSource? _artworkImage;
    private bool _isHighResArtwork;
    private bool _isArtworkLoading;

    public PokemonDetailsViewModel(PokemonDetails model)
    {
        Id = model.Id;
        Name = model.Name;
        ImageUrl = model.ImageUrl;
        TypesDisplay = model.Types.Count == 0 ? string.Empty : string.Join(", ", model.Types.Select(Capitalize));

        HeightMetersDisplay = $"{model.HeightDecimeters / 10.0:0.0} m";
        WeightKgDisplay = $"{model.WeightHectograms / 10.0:0.0} kg";

        BaseStats = model.BaseStats
            .Select(s => new PokemonStatItemViewModel(FormatStatName(s.Name), s.Value))
            .ToList();
    }

    public int Id { get; }

    public string Name { get; }

    public string DisplayName => Capitalize(Name);

    public string ImageUrl { get; }

    public ImageSource? ArtworkImage
    {
        get => _artworkImage;
        private set => SetProperty(ref _artworkImage, value);
    }

    public bool IsHighResArtwork
    {
        get => _isHighResArtwork;
        private set => SetProperty(ref _isHighResArtwork, value);
    }

    public bool IsArtworkLoading
    {
        get => _isArtworkLoading;
        private set => SetProperty(ref _isArtworkLoading, value);
    }

    public string TypesDisplay { get; }

    public string HeightMetersDisplay { get; }

    public string WeightKgDisplay { get; }

    public IReadOnlyList<PokemonStatItemViewModel> BaseStats { get; }

    public void SetArtworkImage(ImageSource? image)
    {
        ArtworkImage = image;
        IsHighResArtwork = true;
        IsArtworkLoading = false;
    }

    public void BeginArtworkLoading()
    {
        ArtworkImage = null;
        IsHighResArtwork = false;
        IsArtworkLoading = true;
    }

    public void EndArtworkLoading()
    {
        IsArtworkLoading = false;
    }

    private static string Capitalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return char.ToUpperInvariant(value[0]) + value[1..];
    }

    private static string FormatStatName(string raw)
    {
        return raw switch
        {
            "hp" => "HP",
            "attack" => "Attack",
            "defense" => "Defense",
            "special-attack" => "Sp. Atk",
            "special-defense" => "Sp. Def",
            "speed" => "Speed",
            _ => string.Join(' ', raw.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(Capitalize))
        };
    }
}

public sealed record PokemonStatItemViewModel(string DisplayName, int Value);
