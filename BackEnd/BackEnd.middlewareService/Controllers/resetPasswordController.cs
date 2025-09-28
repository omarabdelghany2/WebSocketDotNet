
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using BackEnd.middlewareService.Services;

using System;
using System.Net;
using System.Net.Mail;



namespace BackEnd.middlewareService.Controllers
{
    [ApiController]
    [Route("api/")]
    public class ResetPasswordController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ForgetPsswordService _ForgetPsswordService;  //this is how we use TokenValidator 

        public ResetPasswordController(HttpClient httpClient,   ForgetPsswordService forgetPassword)
        {
            _httpClient = httpClient;
            _ForgetPsswordService = forgetPassword;             //this is how we use TokenValidator 
        }
        private void SendEmail(string subject, string body, string senderEmail, string senderPassword, string receiverEmail)
            {
                try
                {
                    // Configure the SMTP client
                    SmtpClient smtpClient = new SmtpClient("mail.privateemail.com", 587)
                    {
                        Credentials = new NetworkCredential(senderEmail, senderPassword),
                        EnableSsl = true // Ensure SSL is enabled
                    };

                    // Create the email message
                    MailMessage mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = false // Set to true if using HTML in the email
                    };

                    // Add the receiver email
                    mailMessage.To.Add(receiverEmail);

                    // Send the email
                    smtpClient.Send(mailMessage);
                    Console.WriteLine("Email sent successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending email: {ex.Message}");
                    throw; // Re-throw the exception to be handled by the caller
                }
            }

        [HttpPost("forget-password")]
        public async Task<IActionResult> forgetPassword([FromBody] ForgetRequest request)
        {
            bool isValid= await _ForgetPsswordService.ValidateEmail(request.Email);
            if (!isValid)
            {
                // send the email with code (Assume you have a method to send emails)
                 return NotFound(new { message = "The email is not registered." });
            }

            //generate random code from 4 numbers
            string randomCode =GenerateRandomCode();

            //save the code in the database
            bool isSaved= await _ForgetPsswordService.SaveCode(request.Email,randomCode);
            if (!isSaved)
            {
                // send the email with code (Assume you have a method to send emails)
                return BadRequest(new { message = "error in saving the code in database." });
            }
            Console.WriteLine(randomCode);

            //send the email with code heree
            try
            {
            // Email configuration
            string senderEmail = "support@t3arff.com"; // Your business email address
            string senderPassword = "T3arff@1ASF";     // Your business email password
            string subject = "Your Password Reset Code";
            string body = $"Your password reset code is: {randomCode}";


                // Call the method to send email
            SendEmail(subject, body, senderEmail, senderPassword, request.Email);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Failed to send email: {ex.Message}" });
            }

            //send the email with code heree
            return Ok(new { message = "Password reset code sent." });

        }

        [HttpPost("verify-code")]
        public async Task<IActionResult> verifyCode([FromBody] VerifyCodeRequest request)
        {
            // Get the code from the database
            string codeFromDatabase = await _ForgetPsswordService.GetCode(request.Email);
            Console.WriteLine(codeFromDatabase);
            if (string.IsNullOrEmpty(codeFromDatabase))
            {
                return NotFound(new { message = "There is no code for reset. Try to send it again." });
            }

            // Normalize the request code by converting it to uppercase
            string normalizedCode = request.Code.ToUpper(); // Make sure the received code is uppercase

            // Check if the provided code matches the code from the database
            if (normalizedCode == codeFromDatabase)
            {
                return Ok(new { message = "The code is verified." });
            }
            else
            {
                return NotFound(new { message = "The code is wrong." });
            }
        }



        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {

            bool Responce = await _ForgetPsswordService.ResetPassword(request.Email ,request.NewPassword , request.VerifyCode);

            if(Responce){

            //call the database function that will save the new passowrd
            return Ok(new { message = "Password reset code sent." });

            }
            else{
                return NotFound(new { message = "Password failed to be reset." });
            }
  

        }



        private string GenerateRandomCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"; // Only uppercase letters
            var random = new Random();
            char[] stringChars = new char[4];
            
            for (int i = 0; i < 4; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }
            
            return new string(stringChars);
        }



    }

    // Input model for login with JsonPropertyName attributes
    // public class UserLoginInput
    // {
    //     [JsonPropertyName("email")]
    //     public string Email { get; set; }

    //     [JsonPropertyName("password")]
    //     public string Password { get; set; }
    // }



    public class ForgetRequest{
        public string Email { get; set; }
    }
    public class VerifyCodeRequest
    {
        public string Email { get; set; }
        public string Code { get; set; }
    }

    // Request model for the "reset-password" endpoint
    public class ResetPasswordRequest
    {
        public string Email { get; set; }
        public string NewPassword { get; set; }
        public string VerifyCode { get; set; }
    }

}
