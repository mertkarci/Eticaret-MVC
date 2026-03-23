using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Eticaret.Service.Abstract;

namespace Eticaret.WebUI.Middlewares
{
    public class MaintenanceMiddleware
    {
        private readonly RequestDelegate _next;

        public MaintenanceMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IMaintenanceService maintenanceService)
        {

            if (maintenanceService.IsEnabled())
            {
                var path = context.Request.Path.Value;


                bool isBypassedRoute = !string.IsNullOrEmpty(path) && 
                    (path.StartsWith("/Admin", System.StringComparison.OrdinalIgnoreCase) || 
                     path.StartsWith("/hesabim/giris-yap", System.StringComparison.OrdinalIgnoreCase));

                if (isBypassedRoute)
                {
                    await _next(context);
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.ContentType = "text/html; charset=utf-8";
                
                string html = @"
                    <html lang='tr'>
                        <head><title>Bakım Modu</title></head>
                        <body style='display:flex; justify-content:center; align-items:center; height:100vh; text-align:center; font-family: Arial, sans-serif; background-color:#f8f9fa;'>
                            <div>
                                <h1 style='color:#333;'>Sitemiz şu anda güncellenmektedir.</h1>
                                <p style='color:#666;'>Lütfen daha sonra tekrar deneyiniz. Anlayışınız için teşekkür ederiz.</p>
                            </div>
                        </body>
                    </html>";

                await context.Response.WriteAsync(html);
                return; 
            }

            await _next(context);
        }
    }
}