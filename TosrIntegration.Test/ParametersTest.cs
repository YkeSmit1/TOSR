using Common;
using Common.Test;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Tosr;
using Xunit;
using Xunit.Abstractions;

namespace TosrIntegration.Test
{
    public class TestCaseProviderParameters
    {
        public static IEnumerable<object[]> TestCasesSystemParameters()
        {
            // ♣♦♥♠
            // Test hcpRelayerToSignOffInNT
            yield return new object[] { "TestNoAsk20HCP", "AK32,AK2,AQ2,432", "x,xxxx,KQxxx,Axx", "1♣1NT2♦2♠3♣3NT", "1♠2♣2♥2NT3♦Pass", "SystemParameters1.json" };
            yield return new object[] { "TestOneAsk20HCP", "AK32,AK2,AQ2,432", "x,xxxx,KQxxx,Axx", "1♣1NT2♦2♠3♣3♥3NT", "1♠2♣2♥2NT3♦3♠Pass", "SystemParameters2.json" };

            // Test requiredMaxHcpToBid4Diamond
            yield return new object[] { "TestRelay18HCP", "AK32,AK2,A32,432", "xxxx,,KQxxx,Kxxx", "1♣2♦2♠3♦4♣4♠", "2♣2♥3♣3♠4♦Pass", "SystemParameters1.json" };
            yield return new object[] { "Test4Diamond18HCP", "AK32,AK2,A32,432", "xxxx,,KQxxx,Kxxx", "1♣2♦2♠3♦4♦4♠", "2♣2♥3♣3♠4♥Pass", "SystemParameters2.json" };
        }
    }

    [Collection("Sequential")]
    public class ParametersTest : IClassFixture<BaseTestFixture>
    {
        private readonly ITestOutputHelper output;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<Fase, bool> fasesWithOffset;
        private readonly ReverseDictionaries reverseDictionaries;

        public ParametersTest(BaseTestFixture fixture, ITestOutputHelper output)
        {
            fasesWithOffset = fixture.fasesWithOffset;
            reverseDictionaries = fixture.reverseDictionaries;
            this.output = output;
        }

        [Theory]
        [MemberData(nameof(TestCaseProviderParameters.TestCasesSystemParameters), MemberType = typeof(TestCaseProviderParameters))]
        public void TestAuctionsSystemParameters(string testName, string northHand, string southHand, string expectedBidsNorth, string expectedBidsSouth, string parametersFileName)
        {
            SetupTest.setupTest(testName, logger);
            string directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var bidManager = new BidManager(new BidGeneratorDescription(), fasesWithOffset, reverseDictionaries, false);
            BidManager.SetSystemParameters(File.ReadAllText(Path.Combine(directoryPath, parametersFileName)));
            var auction = bidManager.GetAuction(northHand, southHand);
            AssertMethods.AssertAuction(expectedBidsNorth, expectedBidsSouth, auction);
        }
    }
}
