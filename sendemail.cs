using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

class Program
{
    public static async Task<bool> SendEmailAsync(string subject, string msg, string sender, string senderPassword, string receiver)
    {
        try
        {
            // Define the SMTP server and port for Gmail
            string smtpServer = "smtp.gmail.com";
            int smtpPort = 587;

            // Create the SMTP client
            using (var smtpClient = new SmtpClient(smtpServer, smtpPort))
            {
                smtpClient.Credentials = new NetworkCredential(sender, senderPassword);
                smtpClient.EnableSsl = true; // Use SSL for security

                // Create the email message
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(sender),
                    Subject = subject,
                    Body = msg,
                    IsBodyHtml = false // Set to true if the message contains HTML
                };

                // Add the recipient
                mailMessage.To.Add(receiver);

                // Send the email asynchronously
                await smtpClient.SendMailAsync(mailMessage);

                // If the email is sent successfully
                Console.WriteLine($"Email sent successfully to {receiver}");
                return true;
            }
        }
        catch (Exception ex)
        {
            // Log any exceptions
            Console.WriteLine($"Failed to send email: {ex.Message}");
            return false;
        }
    }

    static async Task Main(string[] args)
    {
        // Email details
        string subject = "Reservation";
        string message = "Your reservation has been successfully confirmed.";
        string sender = "markwasfy00@gmail.com";  // Replace with your sender email
        string senderPassword = "hvjj bvac dnyf dede";  // Replace with your sender email password (or App Password)
        string receiver = "omarzaa33@gmail.com";  // Replace with your receiver email

        // Call the SendEmailAsync function
        bool emailSent = await SendEmailAsync(subject, message, sender, senderPassword, receiver);

        if (emailSent)
        {
            Console.WriteLine("Email operation was successful.");
        }
        else
        {
            Console.WriteLine("Failed to send the email.");
        }
    }
}
