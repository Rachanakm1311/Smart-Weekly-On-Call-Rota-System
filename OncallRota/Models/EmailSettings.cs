namespace OncallRota.Models
{
    public class EmailSettings
    {
        public string SmtpHost    { get; set; } = "smtp.gmail.com";
        public int    SmtpPort    { get; set; } = 587;
        public bool   UseSsl      { get; set; } = false;
        public bool   UseStartTls { get; set; } = true;
        public string SenderEmail { get; set; } = "";
        public string SenderName  { get; set; } = "Smart On-Call Rota";
        public string Username    { get; set; } = "";
        public string Password    { get; set; } = "";
    }
}