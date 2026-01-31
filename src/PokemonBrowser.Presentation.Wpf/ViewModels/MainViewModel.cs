using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Threading;
using PokemonBrowser.Application.Services;
using PokemonBrowser.Presentation.Wpf.Mvvm;

namespace PokemonBrowser.Presentation.Wpf.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly IPokemonService _pokemonService;
    private readonly Dispatcher _uiDispatcher;
    private readonly ObservableCollection<PokemonListItemViewModel> _pokemon = [];
    private CancellationTokenSource? _loadCts;

    private const int StartupTypeEnrichmentCount = 30;
    private const int TypeEnrichmentConcurrency = 2;

    private string _searchText = string.Empty;
    private bool _isLoadingList;
    private string _errorMessage = string.Empty;
    private string _listStatusText = string.Empty;
    private PokemonListItemViewModel? _selectedPokemon;

    public MainViewModel(IPokemonService pokemonService)
    {
        _pokemonService = pokemonService;
        _uiDispatcher = Dispatcher.CurrentDispatcher;

        FilteredPokemon = CollectionViewSource.GetDefaultView(_pokemon);
        FilteredPokemon.Filter = FilterPokemon;
        FilteredPokemon.SortDescriptions.Add(new SortDescription(nameof(PokemonListItemViewModel.Id), ListSortDirection.Ascending));

        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsLoadingList);
        ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty);

        // Defer initial load so the window can render first.
        _ = _uiDispatcher.InvokeAsync(
            async () => await RefreshAsync(),
            DispatcherPriority.Background);
    }

    public ICollectionView FilteredPokemon { get; }

    public AsyncRelayCommand RefreshCommand { get; }

    public RelayCommand ClearSearchCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                FilteredPokemon.Refresh();
            }
        }
    }

    public bool IsLoadingList
    {
        get => _isLoadingList;
        private set
        {
            if (SetProperty(ref _isLoadingList, value))
            {
                RefreshCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string ListStatusText
    {
        get => _listStatusText;
        private set => SetProperty(ref _listStatusText, value);
    }

    public PokemonListItemViewModel? SelectedPokemon
    {
        get => _selectedPokemon;
        set => SetProperty(ref _selectedPokemon, value);
    }

    private async Task RefreshAsync()
    {
        CancelInFlightLoads();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        try
        {
            ErrorMessage = string.Empty;
            IsLoadingList = true;
            ListStatusText = "Loading first generation (151)...";

            _pokemon.Clear();
            SelectedPokemon = null;

            var list = await _pokemonService.GetFirstGenPokemonAsync(forceRefresh: true, cancellationToken: ct).ConfigureAwait(true);

            foreach (var item in list)
            {
                _pokemon.Add(new PokemonListItemViewModel(item));
            }

            ListStatusText = $"Loaded {list.Count} Pokemon";

            // Best-effort enrichment for primary type (list endpoint doesn't include it).
            var items = _pokemon.Take(StartupTypeEnrichmentCount).ToList();
            _ = Task.Run(() => EnrichListTypesAsync(items, ct), ct);
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            ListStatusText = "Failed to load";
        }
        finally
        {
            IsLoadingList = false;
        }
    }

    private async Task EnrichListTypesAsync(IReadOnlyList<PokemonListItemViewModel> items, CancellationToken ct)
    {
        await Task.Delay(350, ct).ConfigureAwait(false);

        await Parallel.ForEachAsync(
            items,
            new ParallelOptions { MaxDegreeOfParallelism = TypeEnrichmentConcurrency, CancellationToken = ct },
            async (item, token) =>
            {
                try
                {
                    var details = await _pokemonService.GetPokemonDetailsAsync(item.Name, cancellationToken: token).ConfigureAwait(false);
                    var types = details.Types;
                    await _uiDispatcher.InvokeAsync(
                        () => item.EnrichTypes(types),
                        DispatcherPriority.Background);
                }
                catch
                {
                    // best-effort
                }
            }).ConfigureAwait(false);
    }

    private bool FilterPokemon(object obj)
    {
        if (obj is not PokemonListItemViewModel p)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        return p.Name.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private void CancelInFlightLoads()
    {
        try
        {
            _loadCts?.Cancel();
            _loadCts?.Dispose();
        }
        catch
        {
            // ignored
        }
        finally
        {
            _loadCts = null;
        }
    }

}
