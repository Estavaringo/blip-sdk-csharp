﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lime.Messaging.Resources;
using Lime.Protocol;
using Lime.Protocol.Client;
using Lime.Protocol.Network;
using Lime.Protocol.Network.Modules;
using Lime.Protocol.Security;

namespace Take.Blip.Client
{
    /// <summary>
    /// Defines a service for building instances of <see cref="IClient"/>.
    /// </summary>
    public sealed class ClientBuilder
    {
        private readonly ITransportFactory _transportFactory;

        public ClientBuilder()
            : this(new TcpTransportFactory())
        {

        }

        public ClientBuilder(ITransportFactory transportFactory)
        {
            _transportFactory = transportFactory ?? throw new ArgumentNullException(nameof(transportFactory));

            // Initialize the defaults
            Domain = Constants.DEFAULT_DOMAIN;
            Scheme = Constants.DEFAULT_SCHEME;
            HostName = Constants.DEFAULT_HOST_NAME;
            Port = Constants.DEFAULT_PORT;
            SendTimeout = TimeSpan.FromSeconds(60);
            MaxConnectionRetries = 3;
            Compression = SessionCompression.None;
            Encryption = SessionEncryption.TLS;
            RoutingRule = RoutingRule.Identity;
            AutoNotify = true;
            ChannelCount = 1;
            ReceiptEvents = new Event[] { Event.Accepted, Event.Dispatched, Event.Received, Event.Consumed, Event.Failed };
        }

        public string Identifier { get; private set; }

        public string Instance { get; private set; }

        public string Password { get; private set; }

        public string AccessKey { get; private set; }

        public TimeSpan SendTimeout { get; private set; }

        public int MaxConnectionRetries { get; private set; }

        public string Domain { get; private set; }

        public string Scheme { get; private set; }

        public string HostName { get; private set; }

        public int Port { get; private set; }

        public SessionCompression Compression { get; private set; }

        public SessionEncryption Encryption { get; private set; }

        public RoutingRule RoutingRule { get; private set; }

        public int Throughput { get; private set; }

        public bool AutoNotify { get; private set; }

        public int ChannelCount { get; private set; }

        public Identity Identity => Identity.Parse($"{Identifier}@{Domain}");

        public Uri EndPoint => new Uri($"{Scheme}://{HostName}:{Port}");

        public Event[] ReceiptEvents { get; private set; }

        public ClientBuilder UsingPassword(string identifier, string password)
        {
            if (string.IsNullOrEmpty(identifier)) throw new ArgumentNullException(nameof(identifier));
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            Identifier = identifier;
            Password = password;

            return this;
        }

        public ClientBuilder UsingGuest()
        {
            Identifier = Guid.NewGuid().ToString();
            return this;
        }

        public ClientBuilder UsingAccessKey(string identifier, string accessKey)
        {
            if (string.IsNullOrEmpty(identifier)) throw new ArgumentNullException(nameof(identifier));
            if (string.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            Identifier = identifier;
            AccessKey = accessKey;
            return this;
        }

        public ClientBuilder UsingInstance(string instance)
        {
            Instance = instance;
            return this;
        }

        public ClientBuilder UsingRoutingRule(RoutingRule routingRule)
        {
            RoutingRule = routingRule;
            return this;
        }

        public ClientBuilder UsingScheme(string scheme)
        {
            if (string.IsNullOrEmpty(scheme)) throw new ArgumentNullException(nameof(scheme));
            Scheme = scheme;
            return this;
        }

        public ClientBuilder UsingHostName(string hostName)
        {
            if (string.IsNullOrEmpty(hostName)) throw new ArgumentNullException(nameof(hostName));
            HostName = hostName;
            return this;
        }

        public ClientBuilder UsingPort(int port)
        {
            if (port <= 0) throw new ArgumentOutOfRangeException(nameof(port));
            Port = port;
            return this;
        }

        public ClientBuilder UsingDomain(string domain)
        {
            if (string.IsNullOrEmpty(domain)) throw new ArgumentNullException(nameof(domain));
            Domain = domain;
            return this;
        }

        public ClientBuilder UsingEncryption(SessionEncryption sessionEncryption)
        {
            Encryption = sessionEncryption;
            return this;
        }

        public ClientBuilder UsingCompression(SessionCompression sessionCompression)
        {
            Compression = sessionCompression;
            return this;
        }

        public ClientBuilder WithChannelCount(int channelCount)
        {
            if (channelCount <= 0) throw new ArgumentOutOfRangeException(nameof(channelCount));
            ChannelCount = channelCount;
            return this;
        }

        public ClientBuilder WithSendTimeout(TimeSpan timeout)
        {
            if (timeout == default(TimeSpan)) throw new ArgumentOutOfRangeException(nameof(timeout));
            SendTimeout = timeout;
            return this;
        }

        public ClientBuilder WithAutoNotify(bool enabled)
        {
            AutoNotify = enabled;
            return this;
        }

        public ClientBuilder WithReceiptEvents(Event[] events)
        {
            ReceiptEvents = events;
            return this;
        }

        public ClientBuilder WithMaxConnectionRetries(int maxConnectionRetries)
        {
            if (maxConnectionRetries < 1) throw new ArgumentOutOfRangeException(nameof(maxConnectionRetries));
            if (maxConnectionRetries > 5) throw new ArgumentOutOfRangeException(nameof(maxConnectionRetries));

            MaxConnectionRetries = maxConnectionRetries;
            return this;
        }

        public ClientBuilder WithThroughput(int throughput)
        {
            Throughput = throughput;
            return this;
        }

        /// <summary>
        /// Builds a <see cref="IMessagingHubConnection">connection</see> with the configured parameters
        /// </summary>
        /// <returns>An inactive connection with the Messaging Hub. Call <see cref="IMessagingHubConnection.ConnectAsync"/> to activate it</returns>
        public IClient Build()
        {
            var channelBuilder = ClientChannelBuilder
                .Create(() => _transportFactory.Create(EndPoint), EndPoint)
                .WithSendTimeout(SendTimeout)
                .WithEnvelopeBufferSize(100)
                .AddCommandModule(c => new ReplyPingChannelModule(c))
                .AddBuiltHandler(
                    (c, t) =>
                    {
                        if (Throughput > 0)
                        {
                            throw new NotSupportedException("Throughput is not supported yet");
                            // TODO: Not supported on LIME for .NET standard
                            //ThroughputControlChannelModule.CreateAndRegister(c, Throughput);
                        }

                        return Task.CompletedTask;
                    });


            var establishedClientChannelBuilder = new EstablishedClientChannelBuilder(channelBuilder)
                .WithIdentity(Identity)
                .WithAuthentication(GetAuthenticationScheme())
                .WithCompression(Compression)
                .AddEstablishedHandler(SetPresenceAsync)
                .AddEstablishedHandler(SetReceiptAsync)
                .WithEncryption(Encryption);

            if (Instance != null)
            {
                establishedClientChannelBuilder = establishedClientChannelBuilder.WithInstance(Instance);
            }

            IOnDemandClientChannel onDemandClientChannel = CreateOnDemandClientChannel(establishedClientChannelBuilder);
            return new Client(onDemandClientChannel);
        }

        private Authentication GetAuthenticationScheme()
        {
            Authentication result = null;

            if (IsGuest(Identifier))
            {
                var guestAuthentication = new GuestAuthentication();
                result = guestAuthentication;
            }

            if (Password != null)
            {
                var plainAuthentication = new PlainAuthentication();
                plainAuthentication.SetToBase64Password(Password);
                result = plainAuthentication;
            }

            if (AccessKey != null)
            {
                var keyAuthentication = new KeyAuthentication { Key = AccessKey };
                result = keyAuthentication;
            }

            if (result == null)
                throw new InvalidOperationException($"A password or accessKey should be defined. Please use the '{nameof(UsingPassword)}' or '{nameof(UsingAccessKey)}' methods for that.");

            return result;
        }

        private async Task SetPresenceAsync(IClientChannel clientChannel, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!IsGuest(clientChannel.LocalNode.Name))
                await clientChannel.SetResourceAsync(
                        LimeUri.Parse(UriTemplates.PRESENCE),
                        new Presence { Status = PresenceStatus.Available, RoutingRule = RoutingRule, RoundRobin = true },
                        cancellationToken)
                        .ConfigureAwait(false);
        }

        private async Task SetReceiptAsync(IClientChannel clientChannel, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!IsGuest(clientChannel.LocalNode.Name))
                if (ReceiptEvents.Any())
                {
                    await clientChannel.SetResourceAsync(
                            LimeUri.Parse(UriTemplates.RECEIPT),
                            new Receipt { Events = ReceiptEvents },
                            cancellationToken)
                            .ConfigureAwait(false);
                }
        }

        private static bool IsGuest(string name)
        {
            Guid g;
            return Guid.TryParse(name, out g);
        }

        private IOnDemandClientChannel CreateOnDemandClientChannel(IEstablishedClientChannelBuilder establishedClientChannelBuilder)
        {
            IOnDemandClientChannel onDemandClientChannel;

            // Avoid the overhead of the MultiplexerClientChannel for a single connection
            if (ChannelCount == 1)
            {
                onDemandClientChannel = new OnDemandClientChannel(establishedClientChannelBuilder);
            }
            else
            {
                onDemandClientChannel = new MultiplexerClientChannel(establishedClientChannelBuilder, ChannelCount);
            }
            return onDemandClientChannel;
        }
    }
}