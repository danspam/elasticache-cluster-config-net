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
using System.Diagnostics;
using System.Text;
using Enyim.Caching.Memcached;
using System.Net;
using System.Threading.Tasks;
using Amazon.ElastiCacheCluster.Operations;
using Enyim.Caching.Memcached.Results;

namespace LocalSimulationTests
{
    public class TestNode : IMemcachedNode
    {
        public IPEndPoint End;

        private static int requestNum = 1;

        public IOperationResult Execute(IOperation op)
        {
            var getOp = op as IConfigOperation;

            byte[] bytes;

            Debug.WriteLine("request: " + requestNum);

            switch (requestNum)
            {
                case 1:
                    bytes = Encoding.UTF8.GetBytes(
                        $"{requestNum}\r\ncluster.0001.use1.cache.amazon.aws.com|10.10.10.1|11211 cluster.0002.use1.cache.amazon.aws.com|10.10.10.2|11211 cluster.0003.use1.cache.amazon.aws.com|10.10.10.3|11211\r\n");
                    break;
                case 2:
                    bytes = Encoding.UTF8.GetBytes(
                        $"{requestNum}\r\ncluster.0002.use1.cache.amazon.aws.com|10.10.10.2|11211 cluster.0003.use1.cache.amazon.aws.com|10.10.10.3|11211\r\n");
                    break;
                default:
                    bytes = Encoding.UTF8.GetBytes(
                        $"{requestNum}\r\ncluster.0001.use1.cache.amazon.aws.com|10.10.10.1|11211\r\n");
                    break;
            }

            requestNum++;


            var arr = new ArraySegment<byte>(bytes);
            getOp.ConfigResult = new CacheItem(0, arr);

            var result = new PooledSocketResult();
            result.Success = true;

            return result;
        }


        public Task<IOperationResult> ExecuteAsync(IOperation op)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return "TestingAWSInternal";
        }

        public bool ExecuteAsync(IOperation op, Action<bool> next)
        {
            throw new NotImplementedException();
        }

        EndPoint IMemcachedNode.EndPoint { get { return End; } }

        public event Action<IMemcachedNode> Failed;

        public bool IsAlive
        {
            get { throw new NotImplementedException(); }
        }

        public bool Ping()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
