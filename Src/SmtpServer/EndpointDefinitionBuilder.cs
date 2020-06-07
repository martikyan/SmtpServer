using System;
using System.Collections.Generic;
using System.Net;

namespace SmtpServer
{
    public sealed class EndpointDefinitionBuilder
    {
        private readonly List<Action<EndpointDefinition>> _setters = new List<Action<EndpointDefinition>>();

        /// <summary>
        /// Build the endpoint definition.
        /// </summary>
        /// <returns>The endpoint definition that was built.</returns>
        public IEndpointDefinition Build()
        {
            var definition = new EndpointDefinition
            {
                ReadTimeout = TimeSpan.FromMinutes(2)
            };

            _setters.ForEach(setter => setter(definition));

            return definition;
        }

        /// <summary>
        /// Sets the endpoint to listen on.
        /// </summary>
        /// <param name="endpoint">The endpoint to listen on.</param>
        /// <returns>The endpoint builder to continue building on.</returns>
        public EndpointDefinitionBuilder Endpoint(IPEndPoint endpoint)
        {
            _setters.Add(definition => definition.Endpoint = endpoint);

            return this;
        }

        /// <summary>
        /// Adds an endpoint with the given port.
        /// </summary>
        /// <param name="port">The port for the endpoint to listen on.</param>
        /// <returns>The endpoint builder to continue building on.</returns>
        public EndpointDefinitionBuilder Port(int port)
        {
            _setters.Add(definition => definition.Endpoint = new IPEndPoint(IPAddress.Any, port));

            return this;
        }

        /// <summary>
        /// Sets the read timeout to apply to stream operations.
        /// </summary>
        /// <param name="value">The timeout value to apply to read operations.</param>
        /// <returns>A EndpointDefinitionBuilder to continue building on.</returns>
        public EndpointDefinitionBuilder ReadTimeout(TimeSpan value)
        {
            _setters.Add(options => options.ReadTimeout = value);

            return this;
        }

        #region EndpointDefinition

        internal sealed class EndpointDefinition : IEndpointDefinition
        {
            /// <summary>
            /// The IP endpoint to listen on.
            /// </summary>
            public IPEndPoint Endpoint { get; set; }

            /// <summary>
            /// The timeout on each individual buffer read.
            /// </summary>
            public TimeSpan ReadTimeout { get; set; }
        }

        #endregion EndpointDefinition
    }
}