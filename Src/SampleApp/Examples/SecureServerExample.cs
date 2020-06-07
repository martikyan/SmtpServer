using SmtpServer;
using SmtpServer.Tracing;
using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace SampleApp.Examples
{
    public static class SecureServerExample
    {
        public static void Run()
        {
            // this is important when dealing with a certificate that isnt valid
            ServicePointManager.ServerCertificateValidationCallback = IgnoreCertificateValidationFailureForTestingOnly;

            var cancellationTokenSource = new CancellationTokenSource();

            var options = new SmtpServerOptionsBuilder()
                .ServerName("SmtpServer SampleApp")
                .Endpoint(b => b.Port(9025))
                .Build();

            var server = new SmtpServer.SmtpServer(options);
            server.SessionCreated += OnSessionCreated;

            var serverTask = server.StartAsync(cancellationTokenSource.Token);

            SampleMailClient.Send(user: "user");

            cancellationTokenSource.Cancel();
            serverTask.WaitWithoutException();
        }

        private static void OnSessionCreated(object sender, SessionEventArgs e)
        {
            Console.WriteLine("Session Created.");

            e.Context.CommandExecuting += OnCommandExecuting;
        }

        private static void OnCommandExecuting(object sender, SmtpCommandExecutingEventArgs e)
        {
            Console.WriteLine("Command Executing.");

            new TracingSmtpCommandVisitor(Console.Out).Visit(e.Command);
        }

        private static bool IgnoreCertificateValidationFailureForTestingOnly(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}