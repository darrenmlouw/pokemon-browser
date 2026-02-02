using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PokemonBrowser.Presentation.Wpf.Services;

public interface ISpriteImageLoader
{
    Task<ImageSource?> LoadAsync(string url, CancellationToken cancellationToken = default);
}
