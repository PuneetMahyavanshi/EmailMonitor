using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using System.Threading;

namespace EmailMonitor.Services
{
    public class EmailMonitorService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailMonitorService> _logger;
        private readonly ImapClient _client;
        private CancellationTokenSource _cancellationTokenSource;
        //private SemaphoreSlim _semaphore;

        public EmailMonitorService(IConfiguration configuration, ILogger<EmailMonitorService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _client = new ImapClient();
            // _semaphore = new SemaphoreSlim(1, 1); // Initialize semaphore with 1 available slot
        }

        public async Task StartMonitoringAsync(CancellationToken cancellationToken)
        {
            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");
                var server = emailSettings["Server"];
                var port = int.Parse(emailSettings["Port"]);
                var username = emailSettings["Username"];
                var password = emailSettings["Password"];

                await _client.ConnectAsync(server, port, SecureSocketOptions.SslOnConnect, cancellationToken);
                await _client.AuthenticateAsync(username, password, cancellationToken);

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
                var folder = await _client.GetFolderAsync("Inbox");
                await folder.OpenAsync(FolderAccess.ReadOnly);

                var message = folder.LastOrDefault();


                _logger.LogInformation($"New email received: {message.Subject}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the received email.");
            }
        }
    }
}
