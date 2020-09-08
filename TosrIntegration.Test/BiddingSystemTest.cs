using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Xunit;
using Common;
using Tosr;

namespace TosrIntegration.Test
{
    public class BiddingSystemTest
    {
        [Fact]
        public static void ExecuteTest()
        {
            var fasesWithOffset = JsonConvert.DeserializeObject<Dictionary<Fase, bool>>(File.ReadAllText("FasesWithOffset.json"));
            var bidGenerator = new BidGenerator();
            var expectedSouthBids = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("expectedSouthBidsPerHand.json"));
            Pinvoke.Setup("Tosr.db3");
            BidManager bidManager = new BidManager(bidGenerator, fasesWithOffset);
            foreach (var (hand, expectedBids) in expectedSouthBids)
            {
                var generatedAuction = bidManager.GetAuction(string.Empty, hand);
                var generatedSouthBids = generatedAuction.GetBidsAsString(Player.South);
                Assert.Equal(expectedBids, generatedSouthBids);
            }
        }
    }
}