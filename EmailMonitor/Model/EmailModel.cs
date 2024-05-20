using MailKit;

namespace EmailMonitor.Model
{
    public class EmailModel
    {
        public UniqueId MessageId { get; set; }

        public string EmailBody { get; set; }

        public string Subject { get; set; }
    }
}
