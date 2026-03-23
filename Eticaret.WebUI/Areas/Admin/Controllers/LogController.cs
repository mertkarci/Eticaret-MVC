using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Eticaret.WebUI.Areas.Admin.Models;

namespace Eticaret.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminPolicy")]
    public class LogController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public LogController(IWebHostEnvironment env)
        {
            _env = env;
        }

        public IActionResult Index(int page = 1, string level = "", string search = "")
        {
            int pageSize = 50; // Sayfa başına gösterilecek log sayısı
            
            // Program.cs'deki ile aynı dizin yolunu belirliyoruz
            var dbPath = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "..", "Eticaret.Data", "Logs.db"));
            var connectionString = $"Data Source={dbPath}";

            var logs = new List<AppLog>();
            int totalCount = 0;

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                // Toplam kayıt sayısını buluyoruz (Sayfalama yapabilmek için)
                string countQuery = "SELECT COUNT(*) FROM AppLogs WHERE 1=1";
                if (!string.IsNullOrEmpty(level)) countQuery += " AND Level = @level";
                if (!string.IsNullOrEmpty(search)) countQuery += " AND RenderedMessage LIKE @search";

                using (var countCmd = new SqliteCommand(countQuery, connection))
            {
                    if (!string.IsNullOrEmpty(level)) countCmd.Parameters.AddWithValue("@level", level);
                    if (!string.IsNullOrEmpty(search)) countCmd.Parameters.AddWithValue("@search", $"%{search}%");
                    totalCount = Convert.ToInt32(countCmd.ExecuteScalar());
                }

                // Sadece istenen sayfanın verisini çekiyoruz (LIMIT ve OFFSET)
                string query = "SELECT Id, Timestamp, Level, RenderedMessage, Exception FROM AppLogs WHERE 1=1";
                if (!string.IsNullOrEmpty(level)) query += " AND Level = @level";
                if (!string.IsNullOrEmpty(search)) query += " AND RenderedMessage LIKE @search";
                query += " ORDER BY Id DESC LIMIT @limit OFFSET @offset";

                using (var cmd = new SqliteCommand(query, connection))
                {
                    if (!string.IsNullOrEmpty(level)) cmd.Parameters.AddWithValue("@level", level);
                    if (!string.IsNullOrEmpty(search)) cmd.Parameters.AddWithValue("@search", $"%{search}%");
                    cmd.Parameters.AddWithValue("@limit", pageSize);
                    cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            logs.Add(new AppLog
                            {
                                Id = reader.GetInt32(0),
                                Timestamp = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                Level = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                RenderedMessage = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                Exception = reader.IsDBNull(4) ? "" : reader.GetString(4)
                            });
                        }
                    }
                }
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.CurrentLevel = level;
            ViewBag.CurrentSearch = search;

            return View(logs);
        }
    }
}