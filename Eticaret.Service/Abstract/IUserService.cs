using System.Security.Claims;
using Eticaret.Core.Entities;

namespace Eticaret.Service.Abstract;

public interface IUserService
{
    Task<(bool IsSuccess, string ErrorMessage)> EditAccount(string phone, string name, string surname, Guid guidFromCookie);
}
