namespace Eticaret.WebUI.Areas.Admin.Models
{
    public class AppLog
    {
        public int Id { get; set; }
        public string Timestamp { get; set; }
        public string Level { get; set; }
        public string RenderedMessage { get; set; }
        public string Exception { get; set; }
    }
}