using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Xunit;
using Common;
using BiddingLogic;
using Common.Test;

namespace TosrIntegration.Test
{
    [Collection("Sequential")]
    public class BiddingSystemTest : IClassFixture<BaseTestFixture>
    {
        public BiddingSystemTest(BaseTestFixture fixture)
        {
            Fixture = fixture;
        }

        private BaseTestFixture Fixture { get; }

        [Fact]
        public void ExecuteTest()
        {
            var expectedSouthBids = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("expectedSouthBidsPerHand.json"));
            _ = Pinvoke.Setup("Tosr.db3");
            var bidManager = new BidManager(new BidGenerator(), Fixture.fasesWithOffset, Fixture.reverseDictionaries, false);
            foreach (var (hand, expectedBids) in expectedSouthBids)
            {
                var generatedAuction = bidManager.GetAuction("", hand);
                var generatedSouthBids = generatedAuction.GetBidsAsString(Player.South);
                Assert.Equal(expectedBids, generatedSouthBids);
            }
        }
    }
}