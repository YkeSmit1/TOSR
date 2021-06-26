using NLog;
using System;
using System.Collections.Generic;
using System.Text;
using BiddingLogic;

namespace Common.Test
{
    public class SetupTest
    {
        public static void setupTest(string testName, Logger logger)
        {
            if (testName is null)
                throw new ArgumentNullException(nameof(testName));
            logger.Info($"Executing test-case {testName}");
            Pinvoke.Setup("Tosr.db3");
        }
    }
}
