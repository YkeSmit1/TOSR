using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Solver;

namespace TosrGui.Test
{
    public class SolverTests
    {
        [Fact()]
        public void ExecuteTest()
        {
            int trumpSuit = 0; // Spades
            int declarer = 0; // North
            var northHand = "KT98,AK96,AKJ9,4";
            var southHand = "Axxx,Qxxx,xx,KQx";

            var scores = SingleDummySolver.SolveSingleDummy(trumpSuit, declarer, northHand, southHand);
            foreach (var score in scores)
            {
                Assert.InRange(score, 10, 12);
            }
        }
    }
}
