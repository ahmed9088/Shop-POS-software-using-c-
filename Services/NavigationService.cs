using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace PosApp.Services;

public interface INavigationService
{
    ObservableObject? CurrentViewModel { get; }
    event Action StateChanged;
    void NavigateTo<TViewModel>() where TViewModel : ObservableObject;
}

public class NavigationService : INavigationService
{
    private ObservableObject? _currentViewModel;
    private readonly Func<Type, ObservableObject> _viewModelFactory;

    public NavigationService(Func<Type, ObservableObject> viewModelFactory)
    {
        _viewModelFactory = viewModelFactory;
    }

    public ObservableObject? CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            _currentViewModel = value;
            StateChanged?.Invoke();
        }
    }

    public event Action? StateChanged;

    public void NavigateTo<TViewModel>() where TViewModel : ObservableObject
    {
        ObservableObject viewModel = _viewModelFactory.Invoke(typeof(TViewModel));
        CurrentViewModel = viewModel;
    }
}
