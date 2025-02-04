import smtplib
from email.mime.multipart import MIMEMultipart
from email.mime.text import MIMEText

def send_email():
    sender_email = "support@t3arff.com"
    receiver_email = "markwasfy00@gmail.com"
    subject = "Your Subject Here"
    body = "This is the body of your email."

    msg = MIMEMultipart()
    msg['From'] = sender_email
    msg['To'] = receiver_email
    msg['Subject'] = subject
    msg.attach(MIMEText(body, 'plain'))

    smtp_server = "mail.privateemail.com"
    smtp_port = 587
    smtp_username = "support@t3arff.com"
    smtp_password = "T3arff@1ASF"

    try:
        server = smtplib.SMTP(smtp_server, smtp_port)
        server.starttls()
        server.login(smtp_username, smtp_password)
        server.sendmail(sender_email, receiver_email, msg.as_string())
        print("Email sent successfully.")
    except Exception as e:
        print(f"Error sending email: {e}")
    finally:
        server.quit()

send_email()
