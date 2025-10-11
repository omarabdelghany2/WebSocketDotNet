import smtplib
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart

# Gmail credentials
sender_email = "markwasfy00@gmail.com"
app_password = "xquf vqkg pxay sktv"  # Gmail App Password
receiver_email = "omarzaa33@gmail.com"  # change this to who you want to send to

# Email content
subject = "Test Email from Python"
body = "Hello! This is a test email sent via Gmail SMTP using Python."

# Create message
msg = MIMEMultipart()
msg["From"] = sender_email
msg["To"] = receiver_email
msg["Subject"] = subject
msg.attach(MIMEText(body, "plain"))

try:
    # Connect to Gmail SMTP
    server = smtplib.SMTP("smtp.gmail.com", 587)
    server.starttls()  # Secure connection
    server.login(sender_email, app_password)
    
    # Send the email
    server.send_message(msg)
    print("✅ Email sent successfully!")

except Exception as e:
    print(f"❌ Failed to send email: {e}")

finally:
    server.quit()
