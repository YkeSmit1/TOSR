using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Tosr;
using Newtonsoft.Json;
using Common;
using System.IO;
using System.Reflection;
using NLog;

namespace TosrGui.Test
{

    public class TestCaseProviderParameters
    {
        public static IEnumerable<object[]> TestCasesSystemParameters()
        {
            yield return new object[] { "TestSystemParameters1", new int[] { 16, 17, 18, 19, 20 }, 30.0, 17, "SystemParameters1.json" };
            yield return new object[] { "TestSystemParameters2", new int[] { 16, 17, 18, 19 }, 35.0, 18, "SystemParameters2.json" };
        }

        public static IEnumerable<object[]> TestCasesOptimizationParameters()
        {
            yield return new object[] { "TestOptimizationParameters1", 60.0, 10, "OptimizationParameters1.json" };
            yield return new object[] { "TestOptimizationParameters2", 65.0, 15, "OptimizationParameters2.json" };
        }
    }

    [Collection("Sequential")]
    public class ParameterTests
    {

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static string directoryPath;

        [Theory]
        [MemberData(nameof(TestCaseProviderParameters.TestCasesSystemParameters), MemberType = typeof(TestCaseProviderParameters))]
        public void SystemParametersTest(string testName, int[] hcpsForNoControlAsk, double upperBoundForGameBid, int requiredMaxHxpToBid4Diamond, string parameterFileName)
        {
            SetupTest(testName);
            BidManager.SetSystemParameters(File.ReadAllText(Path.Combine(directoryPath, parameterFileName)));
            Assert.Equal(requiredMaxHxpToBid4Diamond, BidManager.systemParameters.requiredMaxHcpToBid4Diamond);
            Assert.Equal(hcpsForNoControlAsk, BidManager.systemParameters.hcpRelayerToSignOffInNT[0]);
            Assert.Equal(upperBoundForGameBid, BidManager.systemParameters.requirementsForRelayBid[0].ToTuple().Item1.ToTuple().Item2);
        }

        [Theory]
        [MemberData(nameof(TestCaseProviderParameters.TestCasesOptimizationParameters), MemberType = typeof(TestCaseProviderParameters))]
        public void OptimizationParametersTest(string testName, double requiredConfidenceToContinueRelaying, int numberOfHandsForSolver, string parameterFileName)
        {
            SetupTest(testName);
            BidManager.SetOptimizationParameters(File.ReadAllText(Path.Combine(directoryPath, parameterFileName)));
            Assert.Equal(requiredConfidenceToContinueRelaying, BidManager.optimizationParameters.requiredConfidenceToContinueRelaying);
            Assert.Equal(numberOfHandsForSolver, BidManager.optimizationParameters.numberOfHandsForSolver);
        }

        private static void SetupTest(string testName)
        {
            if (testName is null)
                throw new ArgumentNullException(nameof(testName));
            logger.Info($"Executing test-case {testName}");
            directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }
    }
}
