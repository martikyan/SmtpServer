using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Tracing;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SampleApp.Examples
{
    public static class SessionContextExample
    {
        private static CancellationTokenSource _cancellationTokenSource;

        public static void Run()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            var options = new SmtpServerOptionsBuilder()
                .ServerName("SmtpServer SampleApp")
                .Endpoint(builder =>
                    builder
                        .Port(9025))
                .Build();

            var server = new SmtpServer.SmtpServer(options);

            server.SessionCreated += OnSessionCreated;
            server.SessionCompleted += OnSessionCompleted;

            var serverTask = server.StartAsync(_cancellationTokenSource.Token);

            SampleMailClient.Send(user: "cain", count: 5);

            serverTask.WaitWithoutException();
        }

        private static void OnSessionCreated(object sender, SessionEventArgs e)
        {
            // the session context contains a Properties dictionary
            // which can be used to custom session context

            e.Context.Properties["Start"] = DateTimeOffset.Now;
            e.Context.Properties["Commands"] = new List<SmtpCommand>();

            e.Context.CommandExecuting += OnCommandExecuting;
        }

        private static void OnCommandExecuting(object sender, SmtpCommandExecutingEventArgs e)
        {
            ((List<SmtpCommand>)e.Context.Properties["Commands"]).Add(e.Command);
        }

        private static void OnSessionCompleted(object sender, SessionEventArgs e)
        {
            e.Context.CommandExecuting -= OnCommandExecuting;

            Console.WriteLine("The session started at {0}.", e.Context.Properties["Start"]);
            Console.WriteLine();

            Console.WriteLine("The user that authenticated was {0}", e.Context.Properties["User"]);
            Console.WriteLine();

            Console.WriteLine("The following commands were executed during the session;");
            Console.WriteLine();

            var writer = new TracingSmtpCommandVisitor(Console.Out);

            foreach (var command in (List<SmtpCommand>)e.Context.Properties["Commands"])
            {
                writer.Visit(command);
            }

            _cancellationTokenSource.Cancel();
        }
    }
}