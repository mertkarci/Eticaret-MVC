
public static class UrlHelper
{
    public static string FriendlyUrl(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        name = name.ToLower().Trim();
        name = name.Replace("ş", "s").Replace("ı", "i").Replace("ğ", "g").Replace("ç", "c").Replace("ö", "o").Replace("ü", "u");
        name = System.Text.RegularExpressions.Regex.Replace(name, @"[^a-z0-9\s-]", "");
        name = System.Text.RegularExpressions.Regex.Replace(name, @"\s+", " ").Replace(" ", "-");
        return name;
    }
}