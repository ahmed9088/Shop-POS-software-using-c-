using CommunityToolkit.Mvvm.ComponentModel;
using PosApp.Services;

namespace PosApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ViewModelBase? _currentViewModel;

    public MainViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        _navigationService.StateChanged += NavigationService_StateChanged;
        CurrentViewModel = (ViewModelBase?)_navigationService.CurrentViewModel;
    }

    private void NavigationService_StateChanged()
    {
        CurrentViewModel = (ViewModelBase?)_navigationService.CurrentViewModel;
    }
}
