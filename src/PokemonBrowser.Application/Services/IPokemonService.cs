using PokemonBrowser.Domain.Models;

namespace PokemonBrowser.Application.Services;

public interface IPokemonService
{
    Task<IReadOnlyList<PokemonSummary>> GetFirstGenPokemonAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
    Task<PokemonDetails> GetPokemonDetailsAsync(string name, bool forceRefresh = false, CancellationToken cancellationToken = default);
}
