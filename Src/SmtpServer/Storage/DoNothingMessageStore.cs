﻿using SmtpServer.Protocol;
using System.Threading;
using System.Threading.Tasks;

namespace SmtpServer.Storage
{
    internal sealed class DoNothingMessageStore : MessageStore
    {
        internal static readonly DoNothingMessageStore Instance = new DoNothingMessageStore();

        /// <summary>
        /// Save the given message to the underlying storage system.
        /// </summary>
        /// <param name="context">The session context.</param>
        /// <param name="transaction">The SMTP message transaction to store.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response code to return that indicates the result of the message being saved.</returns>
        public override Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, CancellationToken cancellationToken)
        {
            return Task.FromResult(SmtpResponse.Ok);
        }
    }
}