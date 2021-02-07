using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Solver;
using Common;

namespace TosrGui.Test
{
    [Collection("Sequential")]
    public class SolverTests
    {
        [Fact()]
        public void ExecuteTest()
        {
            var trumpSuit = Suit.Spades;
            var declarer = Player.North;
            var northHand = "KT98,AK96,AKJ9,4";
            var southHand = "Axxx,Qxxx,xx,KQx";

            var scores = SingleDummySolver.SolveSingleDummy(trumpSuit, declarer, northHand, southHand, null, "NYNY", 10);
            foreach (var score in scores)
            {
                Assert.InRange(score, 10, 12);
            }
        }
    }
}
