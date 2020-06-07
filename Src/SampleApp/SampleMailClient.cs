﻿using MailKit.Net.Smtp;
using MimeKit;

namespace SampleApp
{
    public static class SampleMailClient
    {
        public static void Send(
            string from = null,
            string to = null,
            string subject = null,
            string user = null,
            MimeEntity body = null,
            int count = 1)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(from ?? "from@sample.com"));
            message.To.Add(new MailboxAddress(to ?? "to@sample.com"));
            message.Subject = subject ?? "Hello";
            message.Body = body ?? new TextPart("plain")
            {
                Text = "Hello World"
            };

            using (var client = new SmtpClient())
            {
                client.Connect("localhost", 9025, false);

                while (count-- > 0)
                {
                    client.Send(message);
                }

                client.Disconnect(true);
            }
        }
    }
}