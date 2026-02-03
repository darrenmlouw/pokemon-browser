using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Threading;
using PokemonBrowser.Application.Services;
using PokemonBrowser.Presentation.Wpf.Mvvm;
using PokemonBrowser.Presentation.Wpf.Services;

namespace PokemonBrowser.Presentation.Wpf.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly IPokemonService _pokemonService;
    private readonly IThemeService _themeService;
    private readonly ISpriteImageLoader _spriteImageLoader;
    private readonly Dispatcher _uiDispatcher;
    private readonly ObservableCollection<PokemonListItemViewModel> _pokemon = [];
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _detailsCts;


    private string _searchText = string.Empty;
    private bool _isDarkMode;
    private bool _isLoadingList;
    private string _listErrorMessage = string.Empty;
    private string _listStatusText = string.Empty;
    private PokemonListItemViewModel? _selectedPokemon;

    private bool _isLoadingDetails;
    private string _detailsStatusText = "Select a Pokemon to see details.";
    private string _detailsErrorMessage = string.Empty;
    private PokemonDetailsViewModel? _pokemonDetails;

    public MainViewModel(IPokemonService pokemonService, IThemeService themeService, ISpriteImageLoader spriteImageLoader)
    {
        _pokemonService = pokemonService;
        _themeService = themeService;
        _spriteImageLoader = spriteImageLoader;
        _uiDispatcher = Dispatcher.CurrentDispatcher;

        _isDarkMode = _themeService.CurrentTheme == AppTheme.Dark;

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

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (SetProperty(ref _isDarkMode, value))
            {
                _themeService.ApplyTheme(value ? AppTheme.Dark : AppTheme.Light);
            }
        }
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

    public string ListErrorMessage
    {
        get => _listErrorMessage;
        private set
        {
            if (SetProperty(ref _listErrorMessage, value))
            {
                OnPropertyChanged(nameof(HasListError));
            }
        }
    }

    public bool HasListError => !string.IsNullOrWhiteSpace(ListErrorMessage);

    public string ListStatusText
    {
        get => _listStatusText;
        private set => SetProperty(ref _listStatusText, value);
    }

    public PokemonListItemViewModel? SelectedPokemon
    {
        get => _selectedPokemon;
        set
        {
            if (SetProperty(ref _selectedPokemon, value))
            {
                _ = LoadSelectedPokemonDetailsAsync();
            }
        }
    }

    public bool IsLoadingDetails
    {
        get => _isLoadingDetails;
        private set => SetProperty(ref _isLoadingDetails, value);
    }

    public string DetailsStatusText
    {
        get => _detailsStatusText;
        private set => SetProperty(ref _detailsStatusText, value);
    }

    public string DetailsErrorMessage
    {
        get => _detailsErrorMessage;
        private set
        {
            if (SetProperty(ref _detailsErrorMessage, value))
            {
                OnPropertyChanged(nameof(HasDetailsError));
            }
        }
    }

    public bool HasDetailsError => !string.IsNullOrWhiteSpace(DetailsErrorMessage);

    public PokemonDetailsViewModel? PokemonDetails
    {
        get => _pokemonDetails;
        private set
        {
            if (SetProperty(ref _pokemonDetails, value))
            {
                OnPropertyChanged(nameof(HasPokemonDetails));
            }
        }
    }

    public bool HasPokemonDetails => PokemonDetails is not null;

    private async Task RefreshAsync()
    {
        CancelInFlightLoads();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        try
        {
            ListErrorMessage = string.Empty;
            IsLoadingList = true;
            ListStatusText = "Loading first generation (151)...";

            _pokemon.Clear();
            SelectedPokemon = null;
            ClearDetails();

            var list = await _pokemonService.GetFirstGenPokemonAsync(forceRefresh: true, cancellationToken: ct).ConfigureAwait(true);

            foreach (var item in list)
            {
                var vm = new PokemonListItemViewModel(item);
                _pokemon.Add(vm);

                // Load sprites through our own loader/caching to avoid occasional blanks when
                // scrolling fast (WPF's built-in URI image loader can cache transient failures).
                _ = LoadSpriteAsync(vm, ct);
            }

            ListStatusText = $"Loaded {list.Count} Pokemon";
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (Exception ex)
        {
            ListErrorMessage = ex.Message;
            ListStatusText = "Failed to load";
        }
        finally
        {
            IsLoadingList = false;
        }
    }

    private async Task LoadSpriteAsync(PokemonListItemViewModel item, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(item.SpriteUrl))
            {
                return;
            }

            var image = await _spriteImageLoader.LoadAsync(item.SpriteUrl, ct).ConfigureAwait(false);
            if (ct.IsCancellationRequested)
            {
                return;
            }

            await _uiDispatcher.InvokeAsync(() => item.SetSpriteImage(image), DispatcherPriority.Background);
        }
        catch
        {
            // best-effort
        }
    }

    private async Task LoadSelectedPokemonDetailsAsync()
    {
        CancelInFlightDetailsLoad();

        PokemonDetails = null;
        DetailsErrorMessage = string.Empty;

        if (SelectedPokemon is null)
        {
            DetailsStatusText = "Select a Pokemon to see details.";
            return;
        }

        _detailsCts = new CancellationTokenSource();
        var ct = _detailsCts.Token;

        try
        {
            IsLoadingDetails = true;
            DetailsStatusText = $"Loading {SelectedPokemon.DisplayName}...";

            var details = await _pokemonService.GetPokemonDetailsAsync(SelectedPokemon.Name, cancellationToken: ct);
            if (ct.IsCancellationRequested)
            {
                return;
            }

            var vm = new PokemonDetailsViewModel(details);

            // Show a dedicated loading state until the high-res artwork is ready.
            vm.BeginArtworkLoading();
            PokemonDetails = vm;

            _ = LoadArtworkAsync(vm, ct);
            DetailsStatusText = string.Empty;
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (Exception ex)
        {
            DetailsErrorMessage = ex.Message;
            DetailsStatusText = "Failed to load details";
        }
        finally
        {
            IsLoadingDetails = false;
        }
    }

    private async Task LoadArtworkAsync(PokemonDetailsViewModel details, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(details.ImageUrl))
            {
                await _uiDispatcher.InvokeAsync(() => details.EndArtworkLoading(), DispatcherPriority.Background);
                return;
            }

            var image = await _spriteImageLoader.LoadAsync(details.ImageUrl, ct).ConfigureAwait(false);
            if (ct.IsCancellationRequested)
            {
                return;
            }

            await _uiDispatcher.InvokeAsync(() => details.SetArtworkImage(image), DispatcherPriority.Background);
        }
        catch
        {
            // best-effort
            try
            {
                await _uiDispatcher.InvokeAsync(() => details.EndArtworkLoading(), DispatcherPriority.Background);
            }
            catch
            {
                // ignored
            }
        }
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

    private void CancelInFlightDetailsLoad()
    {
        try
        {
            _detailsCts?.Cancel();
            _detailsCts?.Dispose();
        }
        catch
        {
            // ignored
        }
        finally
        {
            _detailsCts = null;
        }
    }

    private void ClearDetails()
    {
        CancelInFlightDetailsLoad();
        PokemonDetails = null;
        DetailsErrorMessage = string.Empty;
        DetailsStatusText = "Select a Pokemon to see details.";
        IsLoadingDetails = false;
    }

}
