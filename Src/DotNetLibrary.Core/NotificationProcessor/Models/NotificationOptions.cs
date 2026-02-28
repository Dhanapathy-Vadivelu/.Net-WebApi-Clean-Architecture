namespace DotNetLibrary.Core.NotificationProcessor.Models;

public class NotificationOptions
{
    public EmailOptions Email { get; set; } = new();
    public SmsOptions Sms { get; set; } = new();
}

public class EmailOptions
{
    public string Provider { get; set; } = "GmailSmtp";
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderDisplayName { get; set; } = "DotNetLibrary";
    public GmailSmtpOptions GmailSmtp { get; set; } = new();
    public AzureEmailServiceOptions AzureEmailService { get; set; } = new();
}

public class GmailSmtpOptions
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}

public class AzureEmailServiceOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
}

public class SmsOptions
{
    public bool Enabled { get; set; }
}
