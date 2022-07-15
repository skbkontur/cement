using System;

namespace Common
{
    public static class Platform
    {
        public static bool IsUnix()
        {
            return OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();
        }
    }
}
