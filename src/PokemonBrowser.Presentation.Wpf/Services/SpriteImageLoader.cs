using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PokemonBrowser.Presentation.Wpf.Services;

public sealed class SpriteImageLoader : ISpriteImageLoader
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ConcurrentDictionary<string, Lazy<Task<ImageSource?>>> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _spriteConcurrency = new(6);
    private readonly SemaphoreSlim _artworkConcurrency = new(2);

    public SpriteImageLoader(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<ImageSource?> LoadAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return Task.FromResult<ImageSource?>(null);
        }

        // Cache per-URL so fast scrolling doesn't spam network.
        var lazy = _cache.GetOrAdd(url, u => new Lazy<Task<ImageSource?>>(() => DownloadWithRetryAsync(u)));
        var task = lazy.Value;

        // If the caller cancels, they stop waiting, but the underlying download continues
        // (so other callers can still benefit from the cache).
        return cancellationToken.CanBeCanceled ? task.WaitAsync(cancellationToken) : task;
    }

    private async Task<ImageSource?> DownloadWithRetryAsync(string url)
    {
        var gate = IsArtworkUrl(url) ? _artworkConcurrency : _spriteConcurrency;
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            // A couple of retries helps with GitHub raw throttling / transient failures.
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    var client = _httpClientFactory.CreateClient("Sprites");
                    using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"Sprite request failed ({(int)response.StatusCode}).");
                    }

                    var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    return CreateImage(bytes);
                }
                catch when (attempt < 2)
                {
                    await Task.Delay(250 * (attempt + 1)).ConfigureAwait(false);
                }
            }

            // Don't cache a permanent failure.
            _cache.TryRemove(url, out _);
            return null;
        }
        catch
        {
            _cache.TryRemove(url, out _);
            return null;
        }
        finally
        {
            gate.Release();
        }
    }

    private static bool IsArtworkUrl(string url)
    {
        // "official-artwork" images are significantly larger than list sprites.
        return url.Contains("official-artwork", StringComparison.OrdinalIgnoreCase);
    }

    private static ImageSource CreateImage(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = ms;
        bitmap.EndInit();
        bitmap.Freeze();

        return bitmap;
    }
}
