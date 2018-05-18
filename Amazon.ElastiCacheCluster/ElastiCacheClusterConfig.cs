﻿/*
 * Copyright 2014 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Portions copyright 2010 Attila Kiskó, enyim.com. Please see LICENSE.txt
 * for applicable license terms and NOTICE.txt for applicable notices.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 *  http://aws.amazon.com/apache2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Net;
using Enyim.Caching.Memcached;
using Enyim.Reflection;
using Enyim.Caching.Configuration;
using Amazon.ElastiCacheCluster.Pools;
using Amazon.ElastiCacheCluster.Factories;
using Enyim.Caching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Amazon.ElastiCacheCluster
{
    /// <summary>
    /// Configuration class for auto discovery
    /// </summary>
    public class ElastiCacheClusterConfig : IMemcachedClientConfiguration
    {
        // these are lazy initialized in the getters
        private Type _nodeLocator;
        private ITranscoder _transcoder;
        private IMemcachedKeyTransformer _keyTransformer;
        private ILogger<MemcachedClient> _logger;

        internal ClusterConfigSettings Setup;
        internal AutoServerPool Pool;
        internal IConfigNodeFactory NodeFactory;

        /// <summary>
        /// The node used to check the cluster's configuration
        /// </summary>
        public DiscoveryNode DiscoveryNode { get; }

        #region Constructors

        /// <inheritdoc />
        /// <summary>
        /// Initializes a MemcahcedClient config with auto discovery enabled
        /// </summary>
        /// <param name="hostname">The hostname of the cluster containing ".cfg."</param>
        /// <param name="port">The port to connect to for communication</param>
        public ElastiCacheClusterConfig(string hostname, int port)
            : this(new ClusterConfigSettings(hostname, port))
        {
        }

        /// <summary>
        /// Initializes a MemcahcedClient config with auto discovery enabled using the setup provided
        /// </summary>
        /// <param name="setup">The setup to get conifg settings from</param>
        public ElastiCacheClusterConfig(ClusterConfigSettings setup)
        {
            if (setup == null)
            {
                throw new ArgumentNullException(nameof(setup));
            }

            if (setup.ClusterEndPoint == null)
                throw new ArgumentException("Cluster Settings are null");
            if (String.IsNullOrEmpty(setup.ClusterEndPoint.HostName))
                throw new ArgumentNullException(nameof(setup.ClusterEndPoint.HostName));
            if (setup.ClusterEndPoint.Port <= 0)
                throw new ArgumentException("Port cannot be 0 or less");

            Setup = setup;
            Servers = new List<EndPoint>();

            Protocol = setup.Protocol;

            if (setup.KeyTransformer == null)
                KeyTransformer = new DefaultKeyTransformer();
            else
                KeyTransformer = setup.KeyTransformer ?? new DefaultKeyTransformer();

            SocketPool = setup.SocketPool ?? new SocketPoolConfiguration();
            Authentication = setup.Authentication ?? new AuthenticationConfiguration();

            NodeFactory = setup.NodeFactory ?? new DefaultConfigNodeFactory(Logger);
            _nodeLocator = setup.NodeLocator ?? typeof(DefaultNodeLocator);

            if (setup.Transcoder != null)
            {
                _transcoder = setup.Transcoder ?? new DefaultTranscoder();
            }

            if (setup.ClusterEndPoint.HostName.IndexOf(".cfg", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (setup.ClusterNode != null)
                {
                    var tries = setup.ClusterNode.NodeTries > 0
                        ? setup.ClusterNode.NodeTries
                        : DiscoveryNode.DefaultTryCount;
                    var delay = setup.ClusterNode.NodeDelay >= 0
                        ? setup.ClusterNode.NodeDelay
                        : DiscoveryNode.DefaultTryDelay;
                    DiscoveryNode = new DiscoveryNode(this, setup.ClusterEndPoint.HostName, setup.ClusterEndPoint.Port,
                        tries, delay);
                }
                else
                    DiscoveryNode = new DiscoveryNode(this, setup.ClusterEndPoint.HostName, setup.ClusterEndPoint.Port);
            }
            else
            {
                throw new ArgumentException("The provided endpoint does not support auto discovery");
            }
        }

        #endregion

        #region Members

        /// <summary>
        /// Gets a list of <see cref="T:IPEndPoint"/> each representing a Memcached server in the pool.
        /// </summary>
        public IList<EndPoint> Servers { get; }

        /// <summary>
        /// Gets the configuration of the socket pool.
        /// </summary>
        public ISocketPoolConfiguration SocketPool { get; }

        /// <summary>
        /// Gets the authentication settings.
        /// </summary>
        public IAuthenticationConfiguration Authentication { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="T:Enyim.Caching.Memcached.IMemcachedKeyTransformer"/> which will be used to convert item keys for Memcached.
        /// </summary>
        public IMemcachedKeyTransformer KeyTransformer
        {
            get { return _keyTransformer ?? (_keyTransformer = new DefaultKeyTransformer()); }
            set { _keyTransformer = value; }
        }

        /// <summary>
        /// Gets or sets the Type of the <see cref="T:Enyim.Caching.Memcached.IMemcachedNodeLocator"/> which will be used to assign items to Memcached nodes.
        /// </summary>
        /// <remarks>If both <see cref="M:NodeLocator"/> and  <see cref="M:NodeLocatorFactory"/> are assigned then the latter takes precedence.</remarks>
        public Type NodeLocator
        {
            get { return _nodeLocator; }
            set
            {
                ConfigurationHelper.CheckForInterface(value, typeof(IMemcachedNodeLocator));
                _nodeLocator = value;
            }
        }

        /// <summary>
        /// Gets or sets the NodeLocatorFactory instance which will be used to create a new IMemcachedNodeLocator instances.
        /// </summary>
        /// <remarks>If both <see cref="M:NodeLocator"/> and  <see cref="M:NodeLocatorFactory"/> are assigned then the latter takes precedence.</remarks>
        public IProviderFactory<IMemcachedNodeLocator> NodeLocatorFactory { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="T:Enyim.Caching.Memcached.ITranscoder"/> which will be used serialize or deserialize items.
        /// </summary>
        public ITranscoder Transcoder
        {
            get { return _transcoder ?? (_transcoder = new DefaultTranscoder()); }
            set { _transcoder = value; }
        }

        /// <summary>
        /// Gets or sets the type of the communication between client and server.
        /// </summary>
        public MemcachedProtocol Protocol { get; set; }

        #endregion

        #region [ interface                     ]

        IList<EndPoint> IMemcachedClientConfiguration.Servers
        {
            get { return Servers; }
        }

        ISocketPoolConfiguration IMemcachedClientConfiguration.SocketPool
        {
            get { return SocketPool; }
        }

        IAuthenticationConfiguration IMemcachedClientConfiguration.Authentication
        {
            get { return Authentication; }
        }

        IMemcachedKeyTransformer IMemcachedClientConfiguration.CreateKeyTransformer()
        {
            return KeyTransformer;
        }

        IMemcachedNodeLocator IMemcachedClientConfiguration.CreateNodeLocator()
        {
            var f = NodeLocatorFactory;
            if (f != null) return f.Create();

            return NodeLocator == null
                ? new DefaultNodeLocator()
                : (IMemcachedNodeLocator) FastActivator.Create(NodeLocator);
        }

        ITranscoder IMemcachedClientConfiguration.CreateTranscoder()
        {
            return Transcoder;
        }

        IServerPool IMemcachedClientConfiguration.CreatePool()
        {
            switch (Protocol)
            {
                case MemcachedProtocol.Text:
                    Pool = new AutoServerPool(this, new Enyim.Caching.Memcached.Protocol.Text.TextOperationFactory(),
                        Logger);
                    break;
                case MemcachedProtocol.Binary:
                    Pool = new AutoBinaryPool(this, Logger);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unknown protocol: " + (int) Protocol);
            }

            return Pool;
        }


        public ILogger<MemcachedClient> Logger
        {
            get { return _logger ?? NullLogger<MemcachedClient>.Instance; }
            set { _logger = value; }
        }

        #endregion
    }
}
