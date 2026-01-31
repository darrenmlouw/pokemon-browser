using Microsoft.Extensions.DependencyInjection;
using PokemonBrowser.Application.Services;
using PokemonBrowser.Infrastructure.Services;

namespace PokemonBrowser.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddHttpClient<IPokemonService, PokeApiPokemonService>(client =>
        {
            client.BaseAddress = new Uri("https://pokeapi.co/api/v2/");
        });

        return services;
    }
}
