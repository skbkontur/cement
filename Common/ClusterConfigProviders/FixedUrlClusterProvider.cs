using System;
using System.Collections.Generic;
using Vostok.Clusterclient.Core.Topology;

namespace Common.ClusterConfigProviders
{
    internal sealed class FixedUrlClusterProvider : IClusterProvider
    {
        private readonly IList<Uri> urls;

        public FixedUrlClusterProvider(string url)
        {
            urls = new List<Uri> {new Uri(url)}.AsReadOnly();
        }

        public FixedUrlClusterProvider(Uri url)
        {
            urls = new List<Uri> {url}.AsReadOnly();
        }

        public IList<Uri> GetCluster()
        {
            return urls;
        }
    }
}