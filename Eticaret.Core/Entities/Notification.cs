using System.ComponentModel.DataAnnotations;

namespace Eticaret.Core.Entities;

public class Notification : IEntity
{
    public int Id { get; set; }

    [Display(Name = "Görsel Bildirim mi?")]

    public bool isString { get; set; }

    [Display(Name = "Bildirim Adı")]
    public string Name { get; set; }

    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Görsel")]
    public string? Image { get; set; }
    public bool isActive { get; set; }

    [Display(Name = "Kayıt Tarihi"), ScaffoldColumn(false)]
    public DateTime CreateDate { get; set; } = DateTime.Now;
}
