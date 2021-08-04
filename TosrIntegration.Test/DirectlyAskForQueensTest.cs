using BiddingLogic;
using Common.Test;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace TosrIntegration.Test
{
    [Collection("Sequential")]

    public class DirectlyAskForQueensTest : IClassFixture<BaseTestFixture>
    {
        private BaseTestFixture Fixture { get; }
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();


        public DirectlyAskForQueensTest(BaseTestFixture fixture)
        {
            Fixture = fixture;
        }

        public static IEnumerable<object[]> TestCases()
        {
            yield return new object[] { "OneMatchFound", "AK85,K,AQ42,QJT2", "QT642,AJ54,K7,AK", "1♣1♠2♣3♦4♥5♣5♥Pass", "1♥1NT3♣4♣4NT5♦5♠" };
            yield return new object[] { "MultipleMatchesFound", "KQJT43,AQ,KJ,KQJ", "A6,8752,AQ83,A64", "1♣1NT2♥3♣4♣4♥5♥6♣Pass", "1♠2♦2NT3NT4♦5♦5NT6♦" };
            yield return new object[] { "OneMatchFoundPull4Di", "T3,AK532,Q2,AK85", "AKQ954,,AKJ4,Q62", "1♣1♠2♦3♣4♦5♠6♦Pass", "1♥2♣2NT3♠5♦6♣6♠" };
            yield return new object[] { "MultipleMatchesFoundPull4Di", "J86,KT,AQT95,AQT", "AQ7,AJ98654,K,K4", "1♣2♣3♣4♦5♥6♥Pass", "1NT2NT3NT5♦6♦6NT" };
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void TestAuctionsQueens(string testName, string northHand, string southHand, string expectedBidsNorth, string expectedBidsSouth)
        {
            SetupTest.setupTest(testName, logger);
            var bidManager = new BidManager(new BidGenerator(), Fixture.fasesWithOffset, Fixture.reverseDictionaries, false);
            var auction = bidManager.GetAuction(northHand, southHand);
            AssertMethods.AssertAuction(expectedBidsNorth, expectedBidsSouth, auction);
        }
    }
}
