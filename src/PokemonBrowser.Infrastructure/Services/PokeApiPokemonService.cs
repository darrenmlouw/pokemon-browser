using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using PokemonBrowser.Application.Services;
using PokemonBrowser.Domain.Models;
using PokemonBrowser.Infrastructure.Dto;

namespace PokemonBrowser.Infrastructure.Services;

public sealed class PokeApiPokemonService : IPokemonService
{
    private const string FirstGenListEndpoint = "pokemon?limit=151";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, Lazy<Task<PokemonDetails>>> _detailsCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _listLock = new(1, 1);
    private IReadOnlyList<PokemonSummary>? _firstGenCache;

    public PokeApiPokemonService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<PokemonSummary>> GetFirstGenPokemonAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        if (!forceRefresh && _firstGenCache is not null)
        {
            return _firstGenCache;
        }

        await _listLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!forceRefresh && _firstGenCache is not null)
            {
                return _firstGenCache;
            }

            using var response = await _httpClient.GetAsync(FirstGenListEndpoint, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new PokemonServiceException(
                    $"PokeAPI list request failed ({(int)response.StatusCode}).",
                    response.StatusCode);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var dto = await JsonSerializer.DeserializeAsync<PokemonListResponseDto>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
            if (dto is null)
            {
                throw new PokemonServiceException("Unable to parse PokeAPI list response.");
            }

            var list = dto.Results
                .Select(entry =>
                {
                    var id = TryParsePokemonIdFromUrl(entry.Url) ?? 0;
                    var spriteUrl = id > 0
                        ? $"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/{id}.png"
                        : string.Empty;

                    return new PokemonSummary(
                        Id: id,
                        Name: entry.Name,
                        SpriteUrl: spriteUrl);
                })
                .ToList();

            _firstGenCache = list;
            return list;
        }
        finally
        {
            _listLock.Release();
        }
    }

    public Task<PokemonDetails> GetPokemonDetailsAsync(
        string name,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Pokemon name is required.", nameof(name));
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (forceRefresh)
        {
            _detailsCache.TryRemove(name, out _);
        }

        var lazy = _detailsCache.GetOrAdd(
            name,
            _ => new Lazy<Task<PokemonDetails>>(() => FetchPokemonDetailsAsync(name, CancellationToken.None)));

        return cancellationToken.CanBeCanceled
            ? lazy.Value.WaitAsync(cancellationToken)
            : lazy.Value;
    }

    private async Task<PokemonDetails> FetchPokemonDetailsAsync(string name, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"pokemon/{name}", cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new PokemonServiceException($"Pokemon '{name}' was not found.", response.StatusCode);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new PokemonServiceException(
                $"PokeAPI details request failed ({(int)response.StatusCode}).",
                response.StatusCode);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var dto = await JsonSerializer.DeserializeAsync<PokemonDetailsDto>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
        if (dto is null)
        {
            throw new PokemonServiceException("Unable to parse PokeAPI details response.");
        }

        var types = dto.Types
            .OrderBy(t => t.Slot)
            .Select(t => t.Type?.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            .ToList();

        var stats = dto.Stats
            .Select(s => (Name: s.Stat?.Name, s.BaseStat))
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .Select(x => new PokemonStat(NormalizeStatName(x.Name!), x.BaseStat))
            .OrderBy(s => StatSortKey(s.Name))
            .ToList();

        var imageUrl =
            dto.Sprites?.Other?.OfficialArtwork?.FrontDefault
            ?? dto.Sprites?.FrontDefault
            ?? string.Empty;

        return new PokemonDetails(
            Id: dto.Id,
            Name: dto.Name,
            ImageUrl: imageUrl,
            HeightDecimeters: dto.Height,
            WeightHectograms: dto.Weight,
            Types: types,
            BaseStats: stats);
    }

    private static int? TryParsePokemonIdFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var trimmed = url.TrimEnd('/');
        var lastSlashIndex = trimmed.LastIndexOf('/');
        if (lastSlashIndex < 0 || lastSlashIndex == trimmed.Length - 1)
        {
            return null;
        }

        var idPart = trimmed[(lastSlashIndex + 1)..];
        return int.TryParse(idPart, out var id) ? id : null;
    }

    private static string NormalizeStatName(string apiName)
    {
        return apiName switch
        {
            "hp" => "HP",
            "attack" => "Attack",
            "defense" => "Defense",
            "special-attack" => "Sp. Atk",
            "special-defense" => "Sp. Def",
            "speed" => "Speed",
            _ => apiName
        };
    }

    private static int StatSortKey(string name)
    {
        return name switch
        {
            "HP" => 0,
            "Attack" => 1,
            "Defense" => 2,
            "Sp. Atk" => 3,
            "Sp. Def" => 4,
            "Speed" => 5,
            _ => 99
        };
    }
}
