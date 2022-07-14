using System;

namespace Common
{
    public static class Platform
    {
        public static bool IsUnix()
        {
            return Environment.OSVersion.Platform == PlatformID.Unix;
        }
    }
}