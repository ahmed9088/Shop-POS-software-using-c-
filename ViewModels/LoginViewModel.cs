using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosApp.Services;
using System.Windows.Controls;

namespace PosApp.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public LoginViewModel(IAuthenticationService authService, INavigationService navigationService)
    {
        _authService = authService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    private void Login(object? parameter)
    {
        var passwordBox = parameter as PasswordBox;
        var password = passwordBox?.Password;

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Username and password cannot be empty.";
            return;
        }

        bool success = _authService.Login(Username, password);
        
        if (success)
        {
            ErrorMessage = string.Empty;
            var user = _authService.CurrentUser;
            if (user?.Role == "Admin")
            {
                _navigationService.NavigateTo<AdminViewModel>();
            }
            else
            {
                _navigationService.NavigateTo<PosViewModel>();
            }
        }
        else
        {
            ErrorMessage = "Invalid username or password.";
        }
    }
}
