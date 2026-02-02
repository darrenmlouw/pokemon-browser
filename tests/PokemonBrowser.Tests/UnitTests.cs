using System.Net;
using PokemonBrowser.Application.Services;
using PokemonBrowser.Infrastructure.Services;
using PokemonBrowser.Tests.TestDoubles;

namespace PokemonBrowser.Tests;

public sealed class PokemonServiceTests
{
	[Fact]
	public async Task GetFirstGenPokemonAsync_ParsesListAndBuildsSpriteUrlFromId()
	{
		var handler = new StubHttpMessageHandler(req =>
		{
			Assert.EndsWith("/pokemon?limit=151", req.RequestUri!.ToString());

			return StubHttpMessageHandler.Json(
				HttpStatusCode.OK,
				"{\"results\":[{\"name\":\"bulbasaur\",\"url\":\"https://pokeapi.co/api/v2/pokemon/1/\"}]}"
			);
		});

		var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://pokeapi.co/api/v2/") };
		IPokemonService service = new PokeApiPokemonService(httpClient);

		var list = await service.GetFirstGenPokemonAsync(forceRefresh: true);

		Assert.Single(list);
		Assert.Equal(1, list[0].Id);
		Assert.Equal("bulbasaur", list[0].Name);
		Assert.Contains("/sprites/pokemon/1.png", list[0].SpriteUrl);
	}

	[Fact]
	public async Task GetPokemonDetailsAsync_ParsesDetails_AndUsesOfficialArtworkWhenPresent()
	{
		var handler = new StubHttpMessageHandler(req =>
		{
			Assert.EndsWith("/pokemon/pikachu", req.RequestUri!.ToString());

			return StubHttpMessageHandler.Json(
				HttpStatusCode.OK,
				"{\"id\":25,\"name\":\"pikachu\",\"height\":4,\"weight\":60,\"types\":[{\"slot\":1,\"type\":{\"name\":\"electric\"}}],\"stats\":[{\"base_stat\":35,\"stat\":{\"name\":\"hp\"}}],\"sprites\":{\"front_default\":\"front.png\",\"other\":{\"official-artwork\":{\"front_default\":\"art.png\"}}}}"
			);
		});

		var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://pokeapi.co/api/v2/") };
		IPokemonService service = new PokeApiPokemonService(httpClient);

		var details = await service.GetPokemonDetailsAsync("pikachu", forceRefresh: true);

		Assert.Equal(25, details.Id);
		Assert.Equal("pikachu", details.Name);
		Assert.Equal(4, details.HeightDecimeters);
		Assert.Equal(60, details.WeightHectograms);
		Assert.Equal("art.png", details.ImageUrl);
		Assert.Contains("electric", details.Types);
		Assert.Contains(details.BaseStats, s => s.Name == "HP" && s.Value == 35);
	}

	[Fact]
	public async Task GetPokemonDetailsAsync_CachesPerName_ByDefault()
	{
		var handler = new StubHttpMessageHandler(_ =>
			StubHttpMessageHandler.Json(
				HttpStatusCode.OK,
				"{\"id\":1,\"name\":\"bulbasaur\",\"height\":7,\"weight\":69,\"types\":[],\"stats\":[],\"sprites\":{\"front_default\":\"front.png\"}}"
			));

		var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://pokeapi.co/api/v2/") };
		IPokemonService service = new PokeApiPokemonService(httpClient);

		_ = await service.GetPokemonDetailsAsync("bulbasaur");
		_ = await service.GetPokemonDetailsAsync("bulbasaur");

		Assert.Equal(1, handler.CallCount);
	}

	[Fact]
	public async Task GetPokemonDetailsAsync_ThrowsPokemonServiceException_OnNotFound()
	{
		var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(HttpStatusCode.NotFound, "{}"));
		var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://pokeapi.co/api/v2/") };
		IPokemonService service = new PokeApiPokemonService(httpClient);

		var ex = await Assert.ThrowsAsync<PokemonServiceException>(() => service.GetPokemonDetailsAsync("does-not-exist", forceRefresh: true));
		Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
	}

}
