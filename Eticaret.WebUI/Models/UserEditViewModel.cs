using System.ComponentModel.DataAnnotations;

namespace Eticaret.WebUI;

public class UserEditViewModel
{
    public int Id { get; set; }
    
    [Display(Name = "Adı")]
    public string Name { get; set; }

    [Display(Name = "Soyadı")]
    public string Surname { get; set; }

    [Display(Name = "Eposta")]
    public string Email { get; set; }

    [Display(Name = "Telefon")]
    public string Phone { get; set; }

    [Display(Name = "Şifre")]
    public string Password { get; set; }
}
