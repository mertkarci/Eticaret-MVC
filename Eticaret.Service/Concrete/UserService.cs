using System.Linq.Expressions;
using Eticaret.Core.Entities;
using Eticaret.Data;
using Eticaret.Service.Abstract;
using Microsoft.EntityFrameworkCore;

namespace Eticaret.Service.Concrete;

public class UserService : IUserService
{
    private readonly IService<AppUser> _service;
    public UserService(IService<AppUser> service)
    {
        _service = service;
    }

    public async Task<(bool IsSuccess, string ErrorMessage)> EditAccount(string phone, string name, string surname, Guid guidFromCookie)
    {
        AppUser user = await _service.GetAsync(p => p.UserGuid == guidFromCookie);
        if (user == null)
        {
            return (false, "Kullanıcı bulunamadı.");
        }

        user.Name = name;
        user.Surname = surname;

        if (user.Phone != phone && !string.IsNullOrEmpty(phone))
        {
            if (await _service.GetAsync(u => u.Phone == phone) != null)
            {
                return (false, "Bu telefon numarası zaten kullanılıyor.");
            }
        }

        user.Phone = phone;

        _service.Update(user);
        await _service.SaveChangesAsync();
        return (true, string.Empty);
    }
}
