using System.Security.Claims;
using Eticaret.Core.Entities;

namespace Eticaret.Service.Abstract;

public interface IAuthService
{
    // Giriş ve Kayıt
    Task<(bool IsSuccess, string ErrorMessage, ClaimsPrincipal Principal)> LoginAsync(string email, string password, string returnUrl = "/");
    Task<(bool IsSuccess, string ErrorMessage, ClaimsPrincipal Principal)> GoogleLoginAsync(string email, string name, string surname, string returnUrl = "/");
    Task<(bool IsSuccess, string ErrorMessage)> RegisterAsync(AppUser user, string plainPassword);

    // Şifre İşlemleri
    Task<(bool IsSuccess, string ErrorMessage, string ResetToken, string UserName)> GeneratePasswordResetTokenAsync(string email);
    Task<(bool IsSuccess, string ErrorMessage)> ResetPasswordAsync(string token, string newPassword);
}