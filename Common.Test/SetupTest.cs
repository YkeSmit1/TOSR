using NLog;
using System;
using BiddingLogic;

namespace Common.Test
{
    public static class SetupTest
    {
        public static void Setup(string testName, Logger logger)
        {
            if (testName is null)
                throw new ArgumentNullException(nameof(testName));
            logger.Info($"Executing test-case {testName}");
            PInvoke.Setup("Tosr.db3");
        }
    }
}
