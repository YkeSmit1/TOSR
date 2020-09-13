using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using Xunit;
using Newtonsoft.Json;
using Tosr;
using Common;

namespace TosrIntegration.Test
{
    public class TestCaseProvider
    {
        public static IEnumerable<object[]> TestCases()
        {
            // ♣♦♥♠
            yield return new object[] { "FullTestCtlr2", "Axxx,Kxx,Kxx,xxx", "xxxx,Qxx,Ax,xxxx", "1♠2♦3♦3♠4♣4♥4NT5♦5NT6♦6NT" };
            yield return new object[] { "FullTestCtlr3", "Axxx,Kxx,Kxx,xxx", "Kxxx,Qxx,Ax,xxxx", "1♠2♦3♦3♠4♦4NT5♦5NT6♦6NT" };
            yield return new object[] { "FullTestCtrl4", "Axxx,Kxx,Kxx,xxx", "Kxxx,xxx,Ax,Kxxx", "1♠2♦3♦3♠4♥5♦5NT6♦6♠7♣" };
            yield return new object[] { "FullTestZoomCtrl3", "Axxx,Kxx,Kxx,xxx", "Kxxx,Ax,Qxx,xxxx", "1♠2♦3♥4♣4♠5♣5♠6♣6♠", };
            yield return new object[] { "FullTestZoomCtrl4", "Kxxx,Kxx,Kxx,xxx", "Axxx,Ax,Qxx,xxxx", "1♠2♦3♥4♦4NT5♦5NT6♦6NT", };
            yield return new object[] { "FullTestZoomCtrl5", "Axxx,Kxx,Kxx,xxx", "Kxxx,Ax,AQx,xxxx", "1♠2♦3NT4♥5♦5♠6♦" };
            yield return new object[] { "FullTestZoomCtrl6", "Kxxx,Kxx,Kxx,xxx", "Axxx,Ax,AQx,xxxx", "1♠2♦4♣4♠5♥5NT6♥" };
        }
    }

    [Collection("Sequential")]
    public class FullTest
    {
        private readonly Dictionary<Fase, bool> fasesWithOffset;
        private readonly Dictionary<string, Tuple<string, bool>> shapeAuctions;
        private readonly Dictionary<string, List<string>> auctionsControls;

        public FullTest()
        {
            fasesWithOffset = JsonConvert.DeserializeObject<Dictionary<Fase, bool>>(File.ReadAllText("FasesWithOffset.json"));
            shapeAuctions = Util.LoadAuctions<Tuple<string, bool>>("AuctionsByShape.txt", () => new GenerateReverseDictionaries(fasesWithOffset).GenerateAuctionsForShape());
            auctionsControls = Util.LoadAuctions<List<string>>("AuctionsByControls.txt", () => new GenerateReverseDictionaries(fasesWithOffset).GenerateAuctionsForControls());
        }

        [Theory]
        [MemberData(nameof(TestCaseProvider.TestCases), MemberType = typeof(TestCaseProvider))]
        public void TestAuctions(string testName, string northHand, string southHand, string expectedAuction)
        {
            if (testName is null) 
                throw new ArgumentNullException(nameof(testName));

            Pinvoke.Setup("Tosr.db3");
            BidManager bidManager = new BidManager(new BidGenerator(), fasesWithOffset, shapeAuctions, auctionsControls);
            var auction = bidManager.GetAuction(string.Empty, southHand);
            var generatedSouthBids = auction.GetBidsAsString(Player.South);
            Assert.Equal(expectedAuction, generatedSouthBids);
            var constructedSouthHand = bidManager.ConstructSouthHand(northHand, auction);
            Assert.Equal(southHand, constructedSouthHand);
        }


    }
}
