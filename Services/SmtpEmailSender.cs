using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace SpeakingClub.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private string _host;
        private int _port;
        private bool _enableSSL;
        private string _username;
        private string _password;
        public SmtpEmailSender(string host, int port,bool enableSSL,string username,string password)
        {
            this._enableSSL=enableSSL;
            this._host=host;
            this._password=password;
            this._username=username;
            this._port=port;
        }
            public async Task SendEmailAsync(string email, string subject, string htmlMessage)
            {
                try
                {
                    var client = new SmtpClient(this._host, this._port)
                    {
                        Credentials = new NetworkCredential(_username, _password),
                        EnableSsl = this._enableSSL
                    };

                    var mailMessage = new MailMessage(this._username, email, subject, htmlMessage)
                    {
                        IsBodyHtml = true
                    };

                    await client.SendMailAsync(mailMessage);
                }
                catch (SmtpException smtpEx)
                {
                    Console.WriteLine($"SMTP Error: {smtpEx.StatusCode} - {smtpEx.Message}");
                    throw; // Re-throw to ensure it's logged and handled
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"General Error: {ex.Message}");
                    throw;
                }
            }


    }
}