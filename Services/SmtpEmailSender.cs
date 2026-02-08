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
        private string _fromEmail;
        private string _fromName;
        private string _replyToEmail;
        
        public SmtpEmailSender(string host, int port, bool enableSSL, string username, string password, string? fromEmail = null, string? fromName = null, string? replyToEmail = null)
        {
            this._enableSSL = enableSSL;
            this._host = host;
            this._password = password;
            this._username = username;
            this._port = port;
            this._fromEmail = fromEmail ?? username;
            this._fromName = fromName ?? "Speaking Club";
            this._replyToEmail = replyToEmail ?? fromEmail ?? username;
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

                    var fromAddress = new MailAddress(_fromEmail, _fromName);
                    var toAddress = new MailAddress(email);
                    
                    var mailMessage = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = htmlMessage,
                        IsBodyHtml = true
                    };
                    
                    // Add Reply-To if configured
                    if (!string.IsNullOrEmpty(_replyToEmail))
                    {
                        mailMessage.ReplyToList.Add(new MailAddress(_replyToEmail));
                    }

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