using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;
using Tosr;
using Common;
using ShapeDictionary = System.Collections.Generic.Dictionary<string, (System.Collections.Generic.List<string> pattern, bool zoom)>;
using ControlsDictionary = System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>;
using NLog;

namespace TosrIntegration.Test
{
    public class TestCaseProvider
    {
        public static IEnumerable<object[]> TestCases()
        {
            // ♣♦♥♠
            yield return new object[] { "FullTestCtlr2", "Axxx,Kxx,Kxx,xxx", "xxxx,Qxx,Ax,xxxx", "1♣1NT2♥3♥4♣4♥4NT5♦5♠6♦6♠Pass", "1♠2♦3♦3♠4♦4♠5♣5♥6♣6♥7♣" };
            yield return new object[] { "FullTestCtlr3", "Axxx,Kxx,Kxx,xxx", "Kxxx,Qxx,Ax,xxxx", "1♣1NT2♥3♥4♣4♠5♦5♠6♦6♠Pass", "1♠2♦3♦3♠4♥5♣5♥6♣6♥7♣" };
            yield return new object[] { "FullTestCtrl4", "Axxx,Kxx,Kxx,xxx", "Kxxx,xxx,Ax,Kxxx", "1♣1NT2♥3♥4♣4NT5♠6♦6♠7♣Pass", "1♠2♦3♦3♠4♠5♥6♣6♥6NT7♦" };
            yield return new object[] { "FullTestCtrl5", "Kxxx,Kxx,Kxx,xxx", "Axxx,xxx,Ax,Kxxx", "1♣1NT2♥3♥4♦5♣5♠6♣6♥Pass", "1♠2♦3♦4♣4NT5♥5NT6♦6♠" };
            yield return new object[] { "FullTestZoomCtrl3", "Axxx,Kxx,Kxx,xxx", "Kxxx,Ax,Qxx,xxxx", "1♣1NT2♥3♠4♦4NT5♦5NT6♦Pass", "1♠2♦3♥4♣4♠5♣5♠6♣6♠" };
            yield return new object[] { "FullTestZoomCtrl4", "Kxxx,Kxx,Kxx,xxx", "Axxx,Ax,Qxx,xxxx", "1♣1NT2♥3♠4♥5♣5♥6♣6♥Pass", "1♠2♦3♥4♦4NT5♦5NT6♦6NT" };
            yield return new object[] { "FullTestZoomCtrl5", "Axxx,Kxx,Kxx,xxx", "Kxxx,Ax,AQx,xxxx", "1♣1NT2♥4♣4♠5♥5NTPass", "1♠2♦3NT4♥5♦5♠6♦" };
            yield return new object[] { "FullTestZoomCtrl6", "Kxxx,Kxx,Kxx,xxx", "Axxx,Ax,AQx,xxxx", "1♣1NT2♥4♦4NT5♠6♣Pass", "1♠2♦4♣4♠5♥5NT6♥" };
            yield return new object[] { "FullTest_6430Ctrl3", "Kxxx,Axx,Kxx,xxx", "AQxxxx,KQxx,Qxx,", "1♣1♠2♣4♣4♥5♣5NT6♦Pass", "1♥1NT3♠4♦4NT5♠6♣7♣" };
            yield return new object[] { "FullTest_7330Ctrl3", "Kxxx,Axx,Kxx,xxx", "AQxxxxx,KQx,Qxx,", "1♣1♠3NT4♦4NT5♠6♣Pass", "1♥3♠4♣4♠5♥5NT6NT" };
            yield return new object[] { "FullTest_7231Ctrl3", "Kxxx,Axx,Kxx,xxx", "AQxxxxx,KQ,Qxx,x", "1♣1♠4♣4♥5♣5♠6♦Pass", "1♥3NT4♦4NT5♥6♣7♣" };
            yield return new object[] { "FullTest_7321Ctrl3", "Kxxx,Axx,Kxx,xxx", "AQxxxxx,KQx,Qx,x", "1♣1♠4♣4♥5♣5NT6♦Pass", "1♥3NT4♦4NT5♠6♣7♣" };
            yield return new object[] { "FullTest_4441Ctrl4<12", "Kxxx,Axx,Axx,AKx", "Axxx,Kxxx,Kxxx,x", "1♣1♠2♣2♥3♦4♣5♣5♥5NT6♦Pass", "1♥1NT2♦3♣3NT4NT5♦5♠6♣6♥" };
            yield return new object[] { "FullTest_4441Ctrl4>12", "Kxxx,Axx,Axx,AKx", "AQxx,KQxx,Kxxx,x", "1♣1♠2♣2♥3♥4♥5♦Pass", "1♥1NT2♦3♦4♦5♣5♥" };
        }
    }

    [Collection("Sequential")]
    public class FullTest
    {
        private readonly Dictionary<Fase, bool> fasesWithOffset;
        private readonly ShapeDictionary shapeAuctions;
        private readonly ControlsDictionary auctionsControls;

        private readonly ITestOutputHelper output;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public FullTest(ITestOutputHelper output)
        {
            fasesWithOffset = JsonConvert.DeserializeObject<Dictionary<Fase, bool>>(File.ReadAllText("FasesWithOffset.json"));
            shapeAuctions = Util.LoadAuctions("AuctionsByShape.txt", () => new GenerateReverseDictionaries(fasesWithOffset).GenerateAuctionsForShape());
            auctionsControls = Util.LoadAuctions("AuctionsByControls.txt", () => new GenerateReverseDictionaries(fasesWithOffset).GenerateAuctionsForControls());

            this.output = output;
        }

        [Theory]
        [MemberData(nameof(TestCaseProvider.TestCases), MemberType = typeof(TestCaseProvider))]
        public void TestAuctions(string testName, string northHand, string southHand, string expectedBidsNorth, string expectedBidsSouth)
        {
            if (testName is null) 
                throw new ArgumentNullException(nameof(testName));
            logger.Info($"Executing testcase {testName}");

            Pinvoke.Setup("Tosr.db3");
            BidManager bidManager = new BidManager(new BidGenerator(), fasesWithOffset, shapeAuctions, auctionsControls);
            var auction = bidManager.GetAuction(string.Empty, southHand);
            var actualBidsSouth = auction.GetBidsAsString(Player.South);
            Assert.Equal(expectedBidsSouth, actualBidsSouth);

            var actualBidsNorth = auction.GetBidsAsString(Player.North);
            Assert.Equal(expectedBidsNorth, actualBidsNorth);

            var constructedSouthHand = bidManager.ConstructSouthHand(northHand, auction);
            Assert.Equal(southHand, constructedSouthHand);
        }
    }
}
