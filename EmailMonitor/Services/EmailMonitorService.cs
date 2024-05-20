using EmailMonitor.Model;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace EmailMonitor.Services
{
    public class EmailMonitorService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailMonitorService> _logger;
        private readonly ImapClient _client;
        private readonly string _server;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;

        public EmailMonitorService(IConfiguration configuration, ILogger<EmailMonitorService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _client = new ImapClient();
            _server = _configuration.GetSection("EmailSettings")["Server"];
            _port = int.Parse(_configuration.GetSection("EmailSettings")["Server"]);
            _username = _configuration.GetSection("EmailSettings")["Username"];
            _password = _configuration.GetSection("EmailSettings")["Password"];
        }

        public async Task StartMonitoringAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _client.ConnectAsync(_server, _port, SecureSocketOptions.SslOnConnect, cancellationToken);
                await _client.AuthenticateAsync(_username, _password, cancellationToken);

                await _client.Inbox.OpenAsync(FolderAccess.ReadOnly);
                _client.Inbox.CountChanged += OnMessagesArrived;

                _logger.LogInformation("Started monitoring email inbox using IMAP IDLE.");
                await _client.IdleAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async void OnMessagesArrived(object sender, EventArgs e)
        {
            try
            {
                var email = await EmailServiceAsync();

                await CallWebHookAsync(email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the received email.");
            }
        }

        private static async Task CallWebHookAsync(EmailModel email)
        {
            var handler = new HttpClientHandler();
            //for SSL
            handler.ServerCertificateCustomValidationCallback = (HttpRequestMessage request, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslErrors) => true;

            var client = new HttpClient(handler);
            var json = JsonConvert.SerializeObject(email);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://localhost:7252/api/email-services", content);
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseBody);
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
            }
        }

        public async Task<EmailModel> EmailServiceAsync()
        {
            var email = new EmailModel();

            using (var imapClient = new ImapClient())
            {
                var cancellationToken = new CancellationTokenSource().Token;
                await imapClient.ConnectAsync(_server, _port, SecureSocketOptions.SslOnConnect, cancellationToken);
                await imapClient.AuthenticateAsync(_username, _password, cancellationToken);

                var mailFolder = await imapClient.GetFolderAsync("INBOX", cancellationToken);
                await mailFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
                var newEmail = await mailFolder.SearchAsync(SearchQuery.NotSeen);
                Console.WriteLine($"Count new arrived{newEmail.Count}");
                email.MessageId = newEmail.LastOrDefault();

                var mimeMessage = await mailFolder.GetMessageAsync(email.MessageId, cancellationToken);
                email.EmailBody = mimeMessage.HtmlBody;
                email.Subject = mimeMessage.Subject;

                await mailFolder.CloseAsync(false);
                await imapClient.DisconnectAsync(true);
            }

            return email;
        }
    }
}
