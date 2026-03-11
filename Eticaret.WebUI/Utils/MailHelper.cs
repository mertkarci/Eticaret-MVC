using Eticaret.Core.Entities;
using System.Net;
using System.Net.Mail;

namespace Eticaret.WebUI.Utils;

public class MailHelper
{
    public static async Task<bool> SendmMailAsync(Contact contact)
    {
        SmtpClient smtpClient = new SmtpClient("mail.siteadi.com", 587);
        smtpClient.Credentials = new NetworkCredential("info@siteadi.com", "password");
        smtpClient.EnableSsl = true;
        MailMessage message = new MailMessage();
        message.From = new MailAddress("info@siteadi.com");
        message.To.Add(new MailAddress("popup@siteadi.com"));
        message.Subject = "Siteden mesaj geldi";
        message.Body = $"İsim: {contact.Name} - Soyisim: {contact.Surname} - Email: {contact.Email} Telefon: {contact.Phone} - Mesaj: {contact.Message}";
        message.IsBodyHtml = true;
        try
        {
            await smtpClient.SendMailAsync(message);
            smtpClient.Dispose();
            return true;
        }
        catch (Exception)
        {
            return false;
        }


    }
    public static async Task<bool> SendmMailAsync(string email, string mailBody, string subject)
    {
        SmtpClient smtpClient = new SmtpClient("mail.siteadi.com", 587);
        smtpClient.Credentials = new NetworkCredential("info@siteadi.com", "password");
        smtpClient.EnableSsl = true;
        MailMessage message = new MailMessage();
        message.From = new MailAddress("info@siteadi.com");
        message.To.Add(new MailAddress(email));
        message.Subject = subject;
        message.Body = mailBody;
        message.IsBodyHtml = true;
        try
        {
            await smtpClient.SendMailAsync(message);
            smtpClient.Dispose();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("MAIL HATASI DETAYI: " + ex.ToString()); // Bu satırı ekle
            return false;

        }


    }
}
