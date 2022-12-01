using System;

namespace Cement.Cli.Common;

public static class TimeoutHelper
{
    private const int TimesForUseBigDefault = 1;
    private static readonly TimeSpan SmallTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan BigTimeout = TimeSpan.FromMinutes(10);

    private static int badTimes;

    public static TimeSpan IncreaseTimeout(TimeSpan was)
    {
        badTimes++;
        return was < BigTimeout ? BigTimeout : was;
    }

    public static TimeSpan GetStartTimeout()
    {
        if (badTimes > TimesForUseBigDefault)
            return BigTimeout;
        return SmallTimeout;
    }
}
