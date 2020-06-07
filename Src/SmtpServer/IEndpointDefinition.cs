using System;
using System.Net;

namespace SmtpServer
{
    public interface IEndpointDefinition
    {
        /// <summary>
        /// The IP endpoint to listen on.
        /// </summary>
        IPEndPoint Endpoint { get; }

        /// <summary>
        /// The timeout on each individual buffer read.
        /// </summary>
        TimeSpan ReadTimeout { get; }
    }
}