/*
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
using System.Net;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;
using Enyim.Caching.Memcached.Protocol.Binary;
using Microsoft.Extensions.Logging;

namespace Amazon.ElastiCacheCluster.Pools
{
    /// <summary>
    /// Server pool implementing the binary protocol.
    /// </summary>
    internal class AutoBinaryPool : AutoServerPool
    {
        private readonly ISaslAuthenticationProvider _authenticationProvider;
        private readonly IMemcachedClientConfiguration _configuration;
        private readonly ILogger _logger;

        public AutoBinaryPool(IMemcachedClientConfiguration configuration, ILogger logger)
            : base(configuration, new BinaryOperationFactory(logger), logger)
        {
            _authenticationProvider = GetProvider(configuration);
            _configuration = configuration;
            _logger = logger;
        }

        protected override IMemcachedNode CreateNode(EndPoint endpoint)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            return new BinaryNode(endpoint, _configuration.SocketPool, _authenticationProvider, _logger);
        }

        private static ISaslAuthenticationProvider GetProvider(IMemcachedClientConfiguration configuration)
        {
            // create&initialize the authenticator, if any
            // we'll use this single instance everywhere, so it must be thread safe
            IAuthenticationConfiguration auth = configuration.Authentication;

            if (auth != null)
            {
                var t = auth.Type;
                var provider = t == null
                    ? null
                    : Enyim.Reflection.FastActivator.Create(t) as ISaslAuthenticationProvider;

                if (provider != null)
                {
                    provider.Initialize(auth.Parameters);
                    return provider;
                }
            }

            return null;
        }

    }
}