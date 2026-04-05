using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Threading;
using PosApp.Services;
using PosApp.ViewModels;

namespace PosApp;

public partial class App : Application
{
    public new static App Current => (App)Application.Current;
    public IServiceProvider Services { get; }

    public App()
    {
        Services = ConfigureServices();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Services
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<INavigationService, NavigationService>(provider => 
        {
            return new NavigationService(type => (ViewModelBase)provider.GetRequiredService(type));
        });

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<AdminViewModel>();
        services.AddTransient<PosViewModel>();

        // Views
        services.AddTransient<MainWindow>();

        return services.BuildServiceProvider();
    }

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        // Global exception handler — show error dialog instead of silently crashing
        DispatcherUnhandledException += (s, ex) =>
        {
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{ex.Exception.Message}\n\nDetails:\n{ex.Exception.InnerException?.Message}",
                "POS System Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            ex.Handled = true;
        };

        try
        {
            // Initialize database FIRST — must complete before login screen appears
            var dbService = Services.GetRequiredService<IDatabaseService>();
            await dbService.EnsureCreatedAsync();

            var mainWindow = Services.GetRequiredService<MainWindow>();
            var navigationService = Services.GetRequiredService<INavigationService>();
            navigationService.NavigateTo<LoginViewModel>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to start the application:\n\n{ex.Message}",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
