﻿using SmtpServer;
using SmtpServer.Tracing;
using System;
using System.Threading;

namespace SampleApp.Examples
{
    public static class SimpleServerExample
    {
        public static void Run()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var options = new SmtpServerOptionsBuilder()
                .ServerName("SmtpServer SampleApp")
                .Port(9025)
                .CommandWaitTimeout(TimeSpan.FromSeconds(100))
                .Build();

            var server = new SmtpServer.SmtpServer(options);
            server.SessionCreated += OnSessionCreated;

            var serverTask = server.StartAsync(cancellationTokenSource.Token);

            Console.WriteLine("Press any key to shutdown the server.");
            Console.ReadKey();

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
    }
}