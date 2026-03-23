using System.ComponentModel.DataAnnotations;

namespace Eticaret.Core.Entities;

public class Slider : IEntity
{
    public int Id { get; set; }

    [Display(Name = "Başlık")]
    public string? Title { get; set; }

    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Görsel")]
    public string? Image { get; set; }

    [Display(Name = "Bağlantı Link/URL")]
    public string? Link { get; set; }
    [Display(Name = "Slider Tipi")]
    public EnumSliderType EnumSliderType { get; set; }
}

public enum EnumSliderType
{
    [Display(Name = "Ana Sayfadaki Slider")]
    MainSlider,
    [Display(Name = "Bant gibi Slider")]
    FlexSlider,

}