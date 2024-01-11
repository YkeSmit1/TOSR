using Common.Test;
using NLog;
using System.Collections.Generic;
using BiddingLogic;
using Xunit;
using Xunit.Abstractions;
// ReSharper disable NotAccessedField.Local

namespace TosrIntegration.Test
{
    public class TestCaseProviderParameters
    {
        public static IEnumerable<object[]> TestCasesSystemParameters()
        {
            const string systemParameters1 = """
                                             {
                                                           "hcpRelayerToSignOffInNT": {
                                                             "0": [ 16, 17, 18, 19, 20 ],
                                                             "1": [ 21, 22 ],
                                                             "2": [ 23, 24 ]
                                                           },
                                                           "requirementsForRelayBid": [
                                                             [
                                                               [ -0.1, 100.1 ],
                                                               "gameBid"
                                                             ],
                                                             [
                                                               [ -0.1, -0.1 ],
                                                               "fourDiamondEndSignal"
                                                             ],
                                                             [
                                                               [ -0.1, -0.1 ],
                                                               "Relay"
                                                             ]
                                                           ],
                                                           "requiredMaxHcpToBid4Diamond": 17
                                                         }
                                             """;

            string systemParameters2 = """
                                       {
                                                     "hcpRelayerToSignOffInNT": {
                                                       "0": [ 16, 17, 18, 19 ],
                                                       "1": [ 20, 21, 22 ],
                                                       "2": [ 23, 24 ]
                                                     },
                                                     "requirementsForRelayBid": [
                                                       [
                                                         [ -0.1, -0.1 ],
                                                         "gameBid"
                                                       ],
                                                       [
                                                         [ -0.1, 100.1 ],
                                                         "fourDiamondEndSignal"
                                                       ],
                                                       [
                                                         [ -0.1, -0.1 ],
                                                         "Relay"
                                                       ]
                                                     ],
                                                     "requiredMaxHcpToBid4Diamond": 18
                                                   }
                                       """;

            // ♣♦♥♠
            // Test hcpRelayerToSignOffInNT
            yield return ["TestNoAsk20HCP", "AK32,AK2,AQ2,432", "x,xxxx,KQxxx,Axx", "1♣1NT2♦2♠3♣3NT", "1♠2♣2♥2NT3♦Pass", systemParameters1];
            yield return ["TestOneAsk20HCP", "AK32,AK2,AQ2,432", "x,xxxx,KQxxx,Axx", "1♣1NT2♦2♠3♣3♥3NT", "1♠2♣2♥2NT3♦3♠Pass", systemParameters2];

            // Test requirementsForRelayBid
            yield return ["TestBidGame", "AK32,AK2,AJ2,432", "xxxx,x,KQxxx,Kxx", "1♣1♠2♦2♠3♣3♥4♠", "1♥2♣2♥2NT3♦3♠Pass", systemParameters1];
            yield return ["TestBidEndSignal", "AK32,AK2,AJ2,432", "xxxx,x,KQxxx,Kxx", "1♣1♠2♦2♠3♣3♥4♦4♠", "1♥2♣2♥2NT3♦3♠4♥Pass", systemParameters2];

            // Test requiredMaxHcpToBid4Diamond
            // TODO fix this test
            //yield return new object[] { "TestRelay18HCP", "AK32,AK2,A32,432", "xxxx,,KQxxx,Kxxx", "1♣2♦2♠3♦4♣4♠", "2♣2♥3♣3♠4♦Pass", systemParameters1 };
            yield return ["Test4Diamond18HCP", "AK32,AK2,A32,432", "xxxx,,KQxxx,Kxxx", "1♣2♦2♠3♦4♦4♠", "2♣2♥3♣3♠4♥Pass", systemParameters2];
        }
    }

    [Collection("Sequential")]
    public class ParametersTest(BaseTestFixture fixture, ITestOutputHelper output) : IClassFixture<BaseTestFixture>
    {
        private readonly ITestOutputHelper output = output;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<Phase, bool> phasesWithOffset = fixture.phasesWithOffset;
        private readonly ReverseDictionaries reverseDictionaries = fixture.reverseDictionaries;

        [Theory]
        [MemberData(nameof(TestCaseProviderParameters.TestCasesSystemParameters), MemberType = typeof(TestCaseProviderParameters))]
        public void TestAuctionsSystemParameters(string testName, string northHand, string southHand, string expectedBidsNorth, string expectedBidsSouth, string parameters)
        {
            SetupTest.Setup(testName, Logger);
            var bidManager = new BidManager(new BidGeneratorDescription(), phasesWithOffset, reverseDictionaries, true);
            BidManager.SetSystemParameters(parameters);
            var auction = bidManager.GetAuction(northHand, southHand);
            AssertMethods.AssertAuction(expectedBidsNorth, expectedBidsSouth, auction);
        }
    }
}
