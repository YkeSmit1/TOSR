using Xunit;
using Tosr;
using System;
using System.Collections.Generic;
using System.Text;
using Moq;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Newtonsoft.Json;
using System.IO;
using Xunit.Abstractions;
using Xunit.Sdk;
using System.Security.Cryptography.X509Certificates;
using System.Linq;

namespace TosrGui.Test
{
    public class FullBiddingTests
    {
        [Fact()]
        public void BidManagerTest()
        {
            var bidGenerator = new Mock<IBidGenerator>();
            var pinvoke = new Pinvoke();
            var fasesWithOffset = JsonConvert.DeserializeObject<Dictionary<Fase, bool>>(File.ReadAllText("FasesWithOffset.json"));

            // 1Sp
            bidGenerator.SetupSequence(x => x.GetBid(It.IsAny<BiddingState>(), It.IsAny<string>(), It.IsAny<IPinvoke>())).
               // 1Sp
                Returns(() => (4, Fase.Shape, "")).
                // 2Di
                Returns(() => (7, Fase.Shape, "")).
                // 3NT
                Returns(() => (15, Fase.Controls, "")).
                // 4NT
                Returns(() => (5, Fase.Scanning, "")).
                // 5NT
                Returns(() => (5, Fase.Scanning, "")).
                // Pass
                Returns(() => (0, Fase.Scanning, ""));

            var auction = BidManager.GetAuction("", bidGenerator.Object, pinvoke, fasesWithOffset);

            Assert.Equal("1♣1NT2♥4♣5♦6♥", auction.GetBidsAsString(Player.North));
            Assert.Equal("1♠2♦3NT5♣6♦Pass", auction.GetBidsAsString(Player.South));
            Assert.Equal("1♠2♦3NT", auction.GetBidsAsString(Player.South, x => x.Value[Player.South].fase == Fase.Shape));
            Assert.Equal("5♣", auction.GetBidsAsString(Player.South, x => x.Value[Player.South].fase == Fase.Controls));
            Assert.Equal("6♦", auction.GetBidsAsString(Player.South, x => x.Value[Player.South].fase == Fase.Scanning));
        }

        delegate int GetBidReturns(Fase fase, string hand, int lastBidId, out Fase nextfase);
        private void SetBidsForFase(Mock<IPinvoke> pinvoke, Fase fase, List<int> relBidIds)
        {
            Fase nextFase;
            pinvoke.Setup(x => x.GetBidFromRule(fase, It.IsAny<string>(), It.IsAny<int>(), out nextFase)).
            Returns(new GetBidReturns((Fase currentFase, string _, int lastBidId, out Fase nextFase) =>
            {
                nextFase = currentFase;
                var nextBids = relBidIds.FindAll(x => x > lastBidId);
                if (nextBids.Count <= 1 && currentFase != Fase.Scanning)
                    nextFase = currentFase + 1;
                return nextBids.Count > 0 ? nextBids.Min() : 0;
            }));
        }

        [Fact()]
        public void BidGeneratorTest()
        {
            var handString = "AQ32,K32,5432,Q2";

            var pinvoke = new Mock<IPinvoke>();
            var fasesWithOffset = JsonConvert.DeserializeObject<Dictionary<Fase, bool>>(File.ReadAllText("FasesWithOffset.json"));

            SetBidsForFase(pinvoke, Fase.Shape, new List<int> { 4, 11 });
            SetBidsForFase(pinvoke, Fase.Controls, new List<int> { 1, 4 });
            SetBidsForFase(pinvoke, Fase.Scanning, new List<int> { 2, 4, 6, 7, 9 });
            
            var auction = BidManager.GetAuction(handString, new BidGenerator(), pinvoke.Object, fasesWithOffset);
            
            Assert.Equal("1♣1NT3♦3♠4♦4NT5♥6♣6♥7♣", auction.GetBidsAsString(Player.North));
            Assert.Equal("1♠3♣3♥4♣4♠5♦5NT6♦6NTPass", auction.GetBidsAsString(Player.South));
        }
    }
}