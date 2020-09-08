using Common;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tosr;
using Xunit;

namespace TosrGui.Test
{
    public class ScanningBugTest
    {
        [Fact()]
        public void ExecuteTest()
        {
            var fasesWithOffset = JsonConvert.DeserializeObject<Dictionary<Fase, bool>>(File.ReadAllText("FasesWithOffset.json"));
            var biddingState = new BiddingState(fasesWithOffset);

            // Controls --> Controls
            biddingState.fase = Fase.Controls;
            biddingState.UpdateBiddingState(3, Fase.Controls, "");
            Assert.Equal(4, biddingState.nextBidIdForRule);

            // Controls --> Scanning
            biddingState.fase = Fase.Controls;
            biddingState.UpdateBiddingState(6, Fase.Scanning, "");
            Assert.Equal(0, biddingState.nextBidIdForRule);

            // Scanning --> Scanning
            biddingState.fase = Fase.Scanning;
            biddingState.UpdateBiddingState(5, Fase.Scanning, "");
            Assert.Equal(5, biddingState.nextBidIdForRule);
        }
    }
}
