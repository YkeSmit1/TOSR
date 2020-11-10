using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;
using Common;
using Tosr;

namespace TosrIntegration.Test
{
    public class TestCaseProviderReverseDictionaries
    {        
        public static IEnumerable<object[]> TestCasesPull3NT()
        {
            // ♣♦♥♠
            yield return new object[] { "Test1", "xxxx,Qxx,Axx,AQJ", 0, "4♣4♠5♣5♥6♦" };
            yield return new object[] { "Test2", "xxxx,xxx,AQx,AKQ", 0, "4♦5♣5♥5NT6♥" };

            yield return new object[] { "Test3", "xxxx,xxx,Axx,AQJ", 1, "3♠4♥4NT5♦6♣" };
            yield return new object[] { "Test4", "xxxx,KQx,Axx,KQJ", 1, "3NT4♣5♣" };
            yield return new object[] { "Test5", "Axxx,Axx,Axx,AKx", 1, "4NT4♠" };            

            yield return new object[] { "Test6", "xxxx,xxx,Axx,KQJ", 2, "3♠4♦4♣4♥5♦" };
            yield return new object[] { "Test7", "xxxx,xxx,AKx,AQJ", 2, "4♣4♥4♣4♥5♣" };
            yield return new object[] { "Test8", "Axxx,Kxx,Kxx,xxx", 2, "3♠4♥4♠" };
        }

        public static IEnumerable<object[]> TestCasesPull4Di()
        {
            // ♣♦♥♠
            yield return new object[] { "Test1", "xxxx,Qxx,AQx,KQJ", 0, "4♠5♦5♠6♣6NT" };
            yield return new object[] { "Test2", "xxxx,xxx,AQx,AKQ", 0, "5♣5♥5NT6♥" };

            yield return new object[] { "Test3", "xxxx,xxx,Axx,AQJ", 1, "3♠5♣5♥5NT6♠" };
            yield return new object[] { "Test4", "xxxx,Kxx,Axx,AKQ", 1, "4♦4♠5♥" };
            yield return new object[] { "Test5", "Axxx,Axx,Kxx,Qxx", 1, "4♣5♦" };
        }
    }

    [Collection("Sequential")]
    public class TestReverseDictionaries
    {
        private readonly Dictionary<Fase, bool> fasesWithOffset;
        private readonly ITestOutputHelper output;
        private static readonly List<Fase> fasesControlScanning = new List<Fase> { Fase.Controls, Fase.ScanningControls };


        public TestReverseDictionaries(ITestOutputHelper output)
        {
            fasesWithOffset = JsonConvert.DeserializeObject<Dictionary<Fase, bool>>(File.ReadAllText("FasesWithOffset.json"));
            this.output = output;
        }

        [Theory]
        [MemberData(nameof(TestCaseProviderReverseDictionaries.TestCasesPull3NT), MemberType = typeof(TestCaseProviderReverseDictionaries))]
        public void TestAuctionsPull3NT(string testName, string southHand, int controlBidCount, string expectedBidsSouth)
        {
            Setup(testName);
            AssertAuction(southHand, expectedBidsSouth, controlBidCount, Bid.threeNTBid);
        }

        [Theory]
        [MemberData(nameof(TestCaseProviderReverseDictionaries.TestCasesPull4Di), MemberType = typeof(TestCaseProviderReverseDictionaries))]
        public void TestAuctionsPull4Di(string testName, string southHand, int controlBidCount, string expectedBidsSouth)
        {
            Setup(testName);
            AssertAuction(southHand, expectedBidsSouth, controlBidCount, Bid.fourDiamondBid);
        }

        private static void Setup(string testName)
        {
            if (testName is null)
                throw new ArgumentNullException(nameof(testName));
            Pinvoke.Setup("Tosr.db3");
        }

        private void AssertAuction(string southHand, string expectedBidsSouth, int controlBidCount, Bid signOffBid)
        {
            var bidManager = new BidManager(new BidGenerator(), fasesWithOffset,
                (biddingState, auction, northHand) => ReverseDictionaries.GetRelayBidFunc(biddingState, auction, fasesControlScanning.ToArray(), controlBidCount, signOffBid));

            var auction = bidManager.GetAuction("", southHand);
            output.WriteLine(auction.GetPrettyAuction(Environment.NewLine));

            ReverseDictionaries.CheckWithNoPullAuction(southHand, auction, fasesWithOffset);

            var fases = fasesControlScanning.Concat(BidManager.signOffFases).ToArray();
            var actualBidsSouth = auction.GetBidsAsString(fases);
            Assert.Equal(expectedBidsSouth, actualBidsSouth);
        }
    }
}
