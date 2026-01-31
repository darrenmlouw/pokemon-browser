using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PokemonBrowser.Infrastructure;
using PokemonBrowser.Presentation.Wpf.ViewModels;

namespace PokemonBrowser.Presentation.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private IHost? _host;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _host = Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
                    logging.AddFilter("Microsoft.Extensions.Http", LogLevel.Warning);
                    logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
                    logging.AddDebug();
                })
                .ConfigureServices(services =>
                {
                    services.AddInfrastructure();

                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<MainWindow>();
                })
                .Build();

            _host.Start();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_host is not null)
            {
                _host.StopAsync().GetAwaiter().GetResult();
                _host.Dispose();
            }

            base.OnExit(e);
        }
    }

}
