using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tosr;
using Xunit;

namespace TosrIntegration.Test
{
    public class BiddingSystemTest
    {
        [Fact]
        public static void ExecuteTest()
        {
            var fasesWithOffset = JsonConvert.DeserializeObject<Dictionary<Fase, bool>>(File.ReadAllText("FasesWithOffset.json"));
            var bidGenerator = new BidGenerator();
            var pinvoke = new Pinvoke();
            var expectedSouthBids = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("expectedSouthBidsPerHand.json"));
            pinvoke.Setup("Tosr.db3");
            foreach (var (hand, expectedBids) in expectedSouthBids)
            {
                var generatedAuction = BidManager.GetAuction(hand, bidGenerator, pinvoke, fasesWithOffset);
                var generatedSouthBids = generatedAuction.GetBidsAsString(Player.South);
                Assert.Equal(expectedBids, generatedSouthBids);
            }
        }
    }
}