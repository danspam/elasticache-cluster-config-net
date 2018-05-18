﻿/*
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
using System.Diagnostics;
using System.Linq;
using System.Timers;

namespace Amazon.ElastiCacheCluster
{
    /// <summary>
    /// A poller used to reconfigure the client servers when updates occur to the cluster configuration
    /// </summary>
    internal class ConfigurationPoller
    {
        private static readonly Enyim.Caching.ILog Log = Enyim.Caching.LogManager.GetLogger(typeof(ConfigurationPoller));

        #region Defaults

        // Poll once every minute
        private static readonly int DefaultIntervalDelay = 60000;

        #endregion

        private readonly Timer _timer;
        private readonly ElastiCacheClusterConfig _config;

        #region Constructors

        /// <summary>
        /// Creates a poller for Auto Discovery with the default intervals
        /// </summary>
        /// <param name="config">The config</param>
        public ConfigurationPoller(ElastiCacheClusterConfig config)
            : this(config, DefaultIntervalDelay) { }

        /// <summary>
        /// Creates a poller for Auto Discovery with the defined itnerval, delay, tries, and try delay for polling
        /// </summary>
        /// <param name="config">The config</param>
        /// <param name="intervalDelay">The amount of time between polling operations in miliseconds</param>
        public ConfigurationPoller(ElastiCacheClusterConfig config, int intervalDelay)
        {
            var timerInterval = intervalDelay < 0 ? DefaultIntervalDelay : intervalDelay;

            _config = config;

            _timer = new Timer(timerInterval);
            _timer.Elapsed += PollOnTimedEvent;
        }

        #endregion

        #region Polling Methods

        internal void StartTimer()
        {
            Log.Debug("Starting timer");
            PollOnTimedEvent(null, null);
            _timer.Start();
        }

        /// <summary>
        /// Used by the poller's timer to update the cluster configuration if a new version is available
        /// </summary>
        internal void PollOnTimedEvent(Object source, ElapsedEventArgs evnt)
        {
            Log.Debug("Polling...");
            try
            {
                var oldVersion = _config.DiscoveryNode.ClusterVersion;
                var endPoints = _config.DiscoveryNode.GetEndPointList();
                if (oldVersion != _config.DiscoveryNode.ClusterVersion ||
                    (_config.Pool.NodeLocator != null && endPoints.Count != _config.Pool.NodeLocator.GetWorkingNodes().Count()))
                {
                    Debug.WriteLine("Updating endpoints to have {0} nodes", endPoints.Count);
                    Log.DebugFormat("Updating endpoints to have {0} nodes", endPoints.Count);
                    _config.Pool.UpdateLocator(endPoints);
                }
            }
            catch(Exception e)
            {
                try
                {
                    Log.Debug("Error updating endpoints, going to attempt to reresolve configuration endpoint.", e);
                    _config.DiscoveryNode.ResolveEndPoint();

                    var oldVersion = _config.DiscoveryNode.ClusterVersion;
                    var endPoints = _config.DiscoveryNode.GetEndPointList();
                    if (oldVersion != _config.DiscoveryNode.ClusterVersion ||
                        (_config.Pool.NodeLocator != null && endPoints.Count != _config.Pool.NodeLocator.GetWorkingNodes().Count()))
                    {
                        Log.DebugFormat("Updating endpoints to have {0} nodes", endPoints.Count);
                        _config.Pool.UpdateLocator(endPoints);
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug("Error updating endpoints. Setting endpoints to empty collection of nodes.", ex);

                    /*
                     * We were not able to retrieve the current node configuration. This is most likely because the application
                     * is running in development outside of EC2. ElastiCache clusters are only accessible from an EC2 instance
                     * with the right security permissions.
                     */
                    _config.Pool.UpdateLocator(new List<System.Net.IPEndPoint>());
                }
            }
        }

        #endregion

        /// <summary>
        /// Disposes the background thread that is used for polling the configs
        /// </summary>
        public void StopPolling()
        {
            Log.Debug("Destroying poller thread");
            _timer?.Dispose();
        }
    }
}
