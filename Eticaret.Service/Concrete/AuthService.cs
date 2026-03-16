using System.Security.Claims;
using Eticaret.Core.Entities;
using Eticaret.Service.Abstract;


namespace Eticaret.Service.Concrete;

public class AuthService : IAuthService
{
    private readonly IService<AppUser> _serviceAppUser;

    public AuthService(IService<AppUser> serviceAppUser)
    {
        _serviceAppUser = serviceAppUser;
    }

    public async Task<(bool IsSuccess, string ErrorMessage, ClaimsPrincipal Principal)> LoginAsync(string email, string password, string returnUrl = "/")
    {
        var account = await _serviceAppUser.GetAsync(p => p.Email == email && p.isActive);
        if (account == null || !BCrypt.Net.BCrypt.Verify(password, account.Password))
        {
            return (false, "Geçersiz e-posta veya şifre.", null);
        }

        return (true, string.Empty, CreatePrincipal(account, returnUrl));
    }

    public async Task<(bool IsSuccess, string ErrorMessage, ClaimsPrincipal Principal)> GoogleLoginAsync(string email, string name, string surname, string returnUrl = "/")
    {
        var user = await _serviceAppUser.GetAsync(u => u.Email == email);

        if (user == null)
        {
            user = new AppUser
            {
                Email = email,
                Name = name ?? "Google Kullanıcısı",
                Surname = surname ?? "",
                isActive = true,
                isAdmin = false,
                Phone = "05000000000",
                UserGuid = Guid.NewGuid(),
                Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString())
            };
            
            await _serviceAppUser.AddAsync(user);
            await _serviceAppUser.SaveChangesAsync();
        }
        else if (!user.isActive)
        {
            return (false, "Hesabınız askıya alınmıştır.", null);
        }

        return (true, string.Empty, CreatePrincipal(user, returnUrl));
    }

    public async Task<(bool IsSuccess, string ErrorMessage)> RegisterAsync(string name, string surname, string email, string phone, string plainPassword)
    {
        if (await _serviceAppUser.GetAsync(u => u.Email == email) != null)
            return (false, "Bu e-posta adresi zaten kullanılıyor.");

        // Telefon null değilse ve zaten varsa hata ver (Boş telefon numaralarının çakışmasını engeller)
        if (!string.IsNullOrEmpty(phone) && await _serviceAppUser.GetAsync(u => u.Phone == phone) != null)
            return (false, "Bu telefon numarası zaten kullanılıyor.");

        var user = new AppUser
        {
            Name = name,
            Surname = surname,
            Email = email,
            Phone = phone,
            Password = BCrypt.Net.BCrypt.HashPassword(plainPassword),
            isActive = true,
            isAdmin = false,
            UserGuid = Guid.NewGuid()
        };

        await _serviceAppUser.AddAsync(user);
        await _serviceAppUser.SaveChangesAsync();

        return (true, string.Empty);
    }

    public async Task<(bool IsSuccess, string ErrorMessage, string ResetToken, string UserName)> GeneratePasswordResetTokenAsync(string email)
    {
        AppUser user = await _serviceAppUser.GetAsync(p => p.Email == email);
        if (user is null)
            return (false, "Geçersiz bir email girdiniz.", null, null);

        string resetToken = Guid.NewGuid().ToString();
        user.PasswordResetToken = resetToken;
        user.ResetTokenExpires = DateTime.Now.AddHours(2);
        
        _serviceAppUser.Update(user);
        await _serviceAppUser.SaveChangesAsync();

        return (true, string.Empty, resetToken, user.Name);
    }

    public async Task<(bool IsSuccess, string ErrorMessage)> ResetPasswordAsync(string token, string newPassword)
    {
        AppUser appUser = await _serviceAppUser.GetAsync(p => p.PasswordResetToken == token);

        if (appUser is null || appUser.ResetTokenExpires < DateTime.Now)
            return (false, "Geçersiz veya süresi dolmuş bir şifre sıfırlama bağlantısı kullandınız.");

        appUser.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
        appUser.PasswordResetToken = null;
        appUser.ResetTokenExpires = null;

        _serviceAppUser.Update(appUser);
        await _serviceAppUser.SaveChangesAsync();

        return (true, string.Empty);
    }

    private ClaimsPrincipal CreatePrincipal(AppUser account, string returnUrl)
    {
        var claims = new List<Claim>()
        {
            new(ClaimTypes.Name, account.Name),
            new(ClaimTypes.Email, account.Email),
            new(ClaimTypes.Role, account.isAdmin ? "Admin" : "User"),
            new("UserId", account.Id.ToString()),
            new("UserGuid", account.UserGuid.ToString()),
            new("ReturnUrl", returnUrl ?? "/")
        };

        var userIdentity = new ClaimsIdentity(claims, "Cookies");
        return new ClaimsPrincipal(userIdentity);
    }
}