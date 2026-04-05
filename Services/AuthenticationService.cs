using PosApp.Models;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using PosApp.Data;

namespace PosApp.Services;

public interface IAuthenticationService
{
    User? CurrentUser { get; }
    bool Login(string username, string password);
    void Logout();
    string HashPassword(string password);
}

public class AuthenticationService : IAuthenticationService
{
    public User? CurrentUser { get; private set; }
    
    // We instantiate context locally to avoid long-lived contexts for login, 
    // or we could inject IDatabaseService. For simplicity we'll create a new context.
    
    public bool Login(string username, string password)
    {
        string hash = HashPassword(password);
        
        using var context = new AppDbContext();
        var user = context.Users.FirstOrDefault(u => u.Username == username && u.PasswordHash == hash);
        
        if (user != null)
        {
            CurrentUser = user;
            return true;
        }
        
        return false;
    }

    public void Logout()
    {
        CurrentUser = null;
    }

    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}
