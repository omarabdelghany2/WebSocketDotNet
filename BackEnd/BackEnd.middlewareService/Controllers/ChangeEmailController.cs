using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System;
using System.Text.Json.Serialization;
using BackEnd.middlewareService.Services;

namespace BackEnd.middlewareService.Controllers
{
    [ApiController]
    [Route("api/account/change-email")]
    public class ChangeEmailController : ControllerBase
    {
        private readonly ForgetPsswordService _forgetPasswordService;
        private readonly HttpClient _httpClient;

        public ChangeEmailController(ForgetPsswordService forgetPasswordService, HttpClient httpClient)
        {
            _forgetPasswordService = forgetPasswordService;
            _httpClient = httpClient;
        }

        // ---------------------------
        // 1️⃣ Send verification code to old email
        // ---------------------------
        [HttpPost("send-code")]
        public async Task<IActionResult> SendCode([FromBody] ChangeEmailRequest request)
        {
            bool exists = await _forgetPasswordService.ValidateEmail(request.OldEmail);
            if (!exists)
                return NotFound(new { message = "The old email is not registered." });

            string code = GenerateRandomCode();
            bool saved = await _forgetPasswordService.SaveCode(request.OldEmail, code);

            Console.WriteLine($"Generated code for {request.OldEmail}: {code}"); // For debugging

            if (!saved)
                return BadRequest(new { message = "Error saving verification code." });

            try
            {
                // SendEmail(
                //     "Change Email Verification Code",
                //     $"Your verification code is: {code}",
                //     "support@t3arff.com",
                //     "T3arff@1ASF",
                //     request.OldEmail
                // );

                return Ok(new { message = "Verification code sent to old email." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Failed to send email: {ex.Message}" });
            }
        }

        // ---------------------------
        // 2️⃣ Verify the code entered from the old email
        // ---------------------------
        [HttpPost("verify-code")]
        public async Task<IActionResult> VerifyCode([FromBody] VerifyCodeRequest request)
        {
            string storedCode = await _forgetPasswordService.GetCode(request.Email);
            if (string.IsNullOrEmpty(storedCode))
                return NotFound(new { message = "No verification code found. Please request again." });

            if (storedCode != request.Code.ToUpper())
                return BadRequest(new { message = "Invalid verification code." });

            return Ok(new { message = "Code verified successfully. You can now enter your new email." });
        }

        // ---------------------------
        // 3️⃣ Confirm change to new email (after code verified)
        // ---------------------------
        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmChange([FromBody] ConfirmEmailChangeRequest request)
        {
            // Verify the code again for safety
            string storedCode = await _forgetPasswordService.GetCode(request.OldEmail);
            if (storedCode != request.Code.ToUpper())
                return BadRequest(new { message = "Invalid or expired code." });

            // Call your database or Python API to actually change the email
            var payload = new
            {
                old_email = request.OldEmail,
                new_email = request.NewEmail
            };

            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var dbUrl = "http://localhost:8004/api/user/auth/change-email/";
            var response = await _httpClient.PostAsync(dbUrl, content);

            if (response.IsSuccessStatusCode)
                return Ok(new { message = "Email changed successfully." });

            var error = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, new { message = "Failed to change email.", error });
        }

        // --------------------------------
        // Helper: Generate a 4-letter code
        // --------------------------------
        private string GenerateRandomCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var random = new Random();
            char[] stringChars = new char[4];
            for (int i = 0; i < 4; i++)
                stringChars[i] = chars[random.Next(chars.Length)];
            return new string(stringChars);
        }

        // --------------------------------
        // Helper: Send email (same as reset password)
        // --------------------------------
        private void SendEmail(string subject, string body, string senderEmail, string senderPassword, string receiverEmail)
        {
            try
            {
                SmtpClient smtpClient = new SmtpClient("mail.privateemail.com", 587)
                {
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true
                };

                MailMessage mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };

                mailMessage.To.Add(receiverEmail);
                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                throw;
            }
        }
    }

    // ---------------------------
    // Models
    // ---------------------------
    public class ChangeEmailRequest
    {
        [JsonPropertyName("old_email")]
        public string OldEmail { get; set; }
    }

    public class ConfirmEmailChangeRequest
    {
        [JsonPropertyName("old_email")]
        public string OldEmail { get; set; }

        [JsonPropertyName("new_email")]
        public string NewEmail { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }
    }


}
