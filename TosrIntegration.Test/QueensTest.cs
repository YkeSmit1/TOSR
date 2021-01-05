using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NLog;
using Xunit;
using Newtonsoft.Json;
using Common;
using Tosr;

namespace TosrIntegration.Test
{
    public class TestCaseProviderQueens
    {
        public static IEnumerable<object[]> TestCases()
        {
            // ♣♦♥♠
            yield return new object[] { "5NT", "AKQ2,AK2,AKQ,KQ2", "xxxx,Qxx,xx,Axxx", "1♣1NT2♥3♥4♣4♥4NT5♥5NT6♦6♠7♠", "1♠2♦3♦3♠4♦4♠5♦5♠6♣6♥7♣" };
            yield return new object[] { "Zoom5NT", "AKQ2,AK2,KQ2,AKQ", "xxxx,Qxx,Ax,xxxx", "1♣1NT2♥3♥4♣4♥4NT5♦5♠6♦6♠7♠", "1♠2♦3♦3♠4♦4♠5♣5♥6♣6♥7♣" };
            yield return new object[] { "6C", "AK32,AKQ,AKQ,KQ2", "Qxxx,xxx,xx,Axxx", "1♣1NT2♥3♥4♣4♥4NT5♥5NT6♥6NT7♠", "1♠2♦3♦3♠4♦4♠5♦5♠6♦6♠7♣" };
            yield return new object[] { "Zoom6C", "AKJ2,AKQ,K32,AKQ", "Qxxx,xxx,AQ,xxxx", "1♣1NT2♥3♥4♣4♥4NT5♦5♠6♥6NT7♠", "1♠2♦3♦3♠4♦4♠5♣5♥6♦6♠7♦" };
            // With Singleton
            yield return new object[] { "1Singleton5NT", "AKQ2,AK2,AKQ,KQ2", "xxxx,Qxxx,x,Axxx", "1♣1♠2♣2♥2NT3♥4♣4♥4NT5♥7♠", "1♥1NT2♦2♠3♦3♠4♦4♠5♦5NT" };
            yield return new object[] { "1SingletonZoom5NT", "AKQ2,AK2,KQ2,AKQ", "xxxx,Qxx,A,xxxxx", "1♣1♠2♠3♥4♣4♥4NT5♦5♠6♣6♥7♠", "1♥2♥3♦3♠4♦4♠5♣5♥5NT6♦6NT" };
            yield return new object[] { "1Singleton6C", "AK32,AKQ,AKQ,KQ2", "Qxxx,xxx,x,Axxxx", "1♣1♠2♠3♥4♣4♥5♣5♥5NT7♠", "1♥2♥3♦3♠4♦4NT5♦5♠6♦" };
            yield return new object[] { "1SingletonZoom6C", "AKJ2,AKQ,K32,AKQ", "Qxxx,x,AQxx,xxxx", "1♣2♦2♠3♥4♣4♥5♣7♠", "2♣2♥3♦3♠4♦4NT5♠" };

            // With two singletons
            yield return new object[] { "2Singleton5NT", "AKQ2,AK2,AKQ,KQ2", "x,Qxxxxx,x,Axxxx", "1♣2♣2♥2NT3♥4♣4♥4NT5♦7♥", "1NT2♦2♠3♦3NT4♦4♠5♣5NT" };
            yield return new object[] { "2SingletonZoom5NT", "AKQ2,AK2,KQ2,AKQ", "x,Qxxxxx,A,xxxxx", "1♣2♣2♥2NT3♥4♣4♥4NT5♦5♠7♥", "1NT2♦2♠3♦3NT4♦4♠5♣5♥6♣" };
            yield return new object[] { "2Singleton6C", "AK32,AKQ,AKQ,KQ2", "Qxxxxx,x,x,Axxxx", "1♣1♠2♥2NT3♥4♣4♥4NT5♦7♠", "1♥2♦2♠3♦3NT4♦4♠5♣5NT" };
            yield return new object[] { "2SingletonZoom6C", "AKJ2,AKQ,K32,AKQ", "Qxxxxx,x,AQxxx,x", "1♣1♠2♦2NT3♥4♣4♥4NT5♦7♠", "1♥2♣2♠3♦3NT4♦4♠5♣6♣" };
        }
    }

    [Collection("Sequential")]
    public class QueensTest
    {
        private readonly Dictionary<Fase, bool> fasesWithOffset;
        private readonly ReverseDictionaries reverseDictionaries;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public QueensTest()
        {
            fasesWithOffset = JsonConvert.DeserializeObject<Dictionary<Fase, bool>>(File.ReadAllText("FasesWithOffset.json"));
            reverseDictionaries = new ReverseDictionaries(fasesWithOffset, new Progress<string>());
        }

        [Theory]
        [MemberData(nameof(TestCaseProviderQueens.TestCases), MemberType = typeof(TestCaseProviderQueens))]
        public void TestAuctionsQueens(string testName, string northHand, string southHand, string expectedBidsNorth, string expectedBidsSouth)
        {
            // This test can fail because it relies on the sampling and dds.
            if (testName is null)
                throw new ArgumentNullException(nameof(testName));
            logger.Info($"Executing testcase {testName}");

            Pinvoke.Setup("Tosr.db3");
            BidManager bidManager = new BidManager(new BidGenerator(), fasesWithOffset, reverseDictionaries, true);
            var auction = bidManager.GetAuction(northHand, southHand);
            var actualBidsSouth = auction.GetBidsAsString(Player.South);
            Assert.Equal(expectedBidsSouth, actualBidsSouth);

            var actualBidsNorth = auction.GetBidsAsString(Player.North);
            Assert.Equal(expectedBidsNorth, actualBidsNorth);

            var constructedSouthHand = bidManager.ConstructSouthHand(northHand, auction);
            Assert.Equal(southHand, constructedSouthHand.First());

            var queens = BidManager.GetQueensFromAuction(auction, reverseDictionaries, bidManager.shape.Value.shapes.First(), bidManager.controlsScanning.Value.zoomOffset);
            var zip = queens.Zip(southHand.Split(',').Select(x => x.Contains('Q') ? 'Y' : 'N'), (q1, q2) => (q1, q2));
            Assert.All(zip, (x) => Assert.True(x.q1 == x.q2 || x.q1 == 'X'));
        }
    }
}
