using Common;
using System.Collections.Generic;
using Tosr;
using Xunit;

namespace TosrGui.Test
{
    public class ScanningBugTest
    {
        // Updates bidding state, without zoom
        private void updateBiddingState(BiddingState biddingState, int bidIdFromRule, Fase currentFase, Fase nextFase)
        {
            biddingState.Fase = currentFase;
            var bidId = biddingState.CalculateBid(bidIdFromRule, "", false);
            biddingState.UpdateBiddingState(bidIdFromRule, nextFase, bidId, () => 0);
        }
        [Fact()]
        public void ExecuteTest()
        {
            var fasesWithOffset = new Dictionary<Fase, bool> {
                { Fase.Shape, false },
                { Fase.Controls, false },
                { Fase.Scanning, true }
            };
            var biddingState = new BiddingState(fasesWithOffset);

            // Controls --> Controls
            // If bidIdFromRule = 3, then nextBidIdForRule should be 4, because of the relay bid
            updateBiddingState(biddingState, 3, Fase.Controls, Fase.Controls);
            Assert.Equal(4, biddingState.NextBidIdForRule);

            // Controls --> Scanning
            // When starting a next fase, the counting starts again at 0
            updateBiddingState(biddingState, 6, Fase.Controls, Fase.Scanning);
            Assert.Equal(0, biddingState.NextBidIdForRule);

            // Scanning --> Scanning
            // Here the relay bid is not part of the counting, because this is a fase with offset
            // Hence nextBidIdForRule should be equal to bidIdFromRule
            updateBiddingState(biddingState, 5, Fase.Scanning, Fase.Scanning);
            Assert.Equal(5, biddingState.NextBidIdForRule);
        }
    }
}
