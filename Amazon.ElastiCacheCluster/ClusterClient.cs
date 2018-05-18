/*
 * Copyright 2014 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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
using System.Linq;
using System.Text;
using Enyim.Caching;
using Microsoft.Extensions.Logging;

namespace Amazon.ElastiCacheCluster
{
    /// <summary>
    /// Used to instantiate MemcachedClients with auto discovery enabled.
    /// Only use these for easy creation because the ability to get information from the config object is lost
    /// </summary>
    public static class ClusterClient
    {
        /// <summary>
        /// Creates a MemcachedClient using the default settings with the endpoint and port specified
        /// </summary>
        /// <param name="endpoint">The url for the cluster endpoint containing .cfg.</param>
        /// <param name="port">The port to access the cluster on</param>
        /// <param name="loggerFactory">Factory to create logger</param>
        /// <returns>A new MemcachedClient configured for auto discovery</returns>
        public static MemcachedClient CreateClient(string endpoint, int port, ILoggerFactory loggerFactory)
        {
            return new MemcachedClient(loggerFactory, new ElastiCacheClusterConfig(endpoint, port));
        }

        /// <summary>
        /// Creates a MemcachedClient using the Client config provided
        /// </summary>
        /// <param name="config">The config to instantiate the client with</param>
        /// <param name="loggerFactory">Factory to create logger</param>
        /// <returns>A new MemcachedClient configured for auto discovery</returns>
        public static MemcachedClient CreateClient(ElastiCacheClusterConfig config, ILoggerFactory loggerFactory)
        {
            return new MemcachedClient(loggerFactory, config);
        }
    }
}
