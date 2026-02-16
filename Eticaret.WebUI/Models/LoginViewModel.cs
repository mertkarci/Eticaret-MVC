using System.ComponentModel.DataAnnotations;

namespace Eticaret.WebUI;

public class LoginViewModel
{
    [Display(Name ="E-posta")]
        [DataType(DataType.EmailAddress)]
    public string Email {get; set;}

    [Display(Name ="Şifre")]
    [DataType(DataType.Password)]
    public string Password {get; set;}

    public string? ReturnUrl{get; set;}
    public bool RememberMe {get; set;}
}
