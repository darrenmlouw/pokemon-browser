# Pokemon Browser (WPF / MVVM)

Pokemon Browser is a small WPF application built for the JoyConnect technical challenge.

## Requirements Covered

- MVVM (no UI logic in code-behind beyond initialization)
- Service layer (`IPokemonService`) using async/await and proper error handling
- PokeAPI integration
  - `GET https://pokeapi.co/api/v2/pokemon?limit=151`
  - `GET https://pokeapi.co/api/v2/pokemon/{name}`
- List view shows: name, sprite
- Detail view shows: name, image, height, weight, all types, base stats
- Commands (`ICommand`) + bindings, `ObservableCollection`, `DataTemplate`
- Dependency injection via `Microsoft.Extensions.Hosting`
- Bonus: loading indicators + in-memory caching in the service layer
- Bonus: image loading is cached + resilient (avoids blank sprites on fast scroll)
- Bonus: light/dark theme toggle (persisted between runs)
- Bonus: native Windows title bar switches with theme (best-effort via DWM)

## How to Run

Prereqs: Windows + .NET SDK 10.x

From the repo root:

1. `dotnet restore`
2. `dotnet build`
3. Run the WPF app from Visual Studio / VS Code, or:
  - `dotnet run --project src/PokemonBrowser.Presentation.Wpf/PokemonBrowser.Presentation.Wpf.csproj`

Run tests:

- `dotnet test`

## Troubleshooting

- If `dotnet build` fails with file-lock errors, a running instance of the WPF app (or Visual Studio) is holding the output DLL/EXE.
  - Close the app and rebuild, or use `dotnet build -p:UseAppHost=false`.

## Architecture Notes

This solution uses **Clean Architecture** with **MVVM** in the WPF presentation layer.

- **Domain** (`src/PokemonBrowser.Domain`)
  - Domain models (`PokemonSummary`, `PokemonDetails`, `PokemonStat`)

- **Application** (`src/PokemonBrowser.Application`)
  - Service abstractions used by the UI (`IPokemonService`)
  - Shared errors (`PokemonServiceException`)

- **Infrastructure** (`src/PokemonBrowser.Infrastructure`)
  - PokeAPI DTOs + JSON parsing
  - `PokeApiPokemonService` (HttpClient + in-memory caching)

- **Presentation (WPF/MVVM)** (`src/PokemonBrowser.Presentation.Wpf`)
  - MVVM base types (`ObservableObject`, `RelayCommand`, `AsyncRelayCommand`)
  - `MainViewModel` drives search/list/details via `IPokemonService`
  - UI is pure binding + templates; selection triggers details load

## Assumptions / Trade-offs

- Height/weight are converted from PokeAPI units (decimeters/hectograms) to meters/kg.

## Notes

- Theme preference is saved to `%APPDATA%\PokemonBrowser\settings.json`.
