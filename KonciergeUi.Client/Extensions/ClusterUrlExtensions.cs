using System;

namespace KonciergeUi.Client.Extensions
{
    public static class ClusterUrlExtensions
    {
        public static string ToClusterHost(this string? clusterUrl)
        {
            if (string.IsNullOrWhiteSpace(clusterUrl))
            {
                return string.Empty;
            }

            if (Uri.TryCreate(clusterUrl, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
            {
                return uri.Host;
            }

            return clusterUrl;
        }
    }
}

