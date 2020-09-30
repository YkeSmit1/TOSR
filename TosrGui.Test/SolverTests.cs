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

        [Fact(Skip = "Dealer.exe is not available. Accidental commit")]
        public void Test2()
        {
            int trumpSuit = 0; // Spades
            int declarer = 0; // North
            //var northHand = "KT98,AK96,AKJ9,4";
            //var southHand = "Axxx,Qxxx,xx,KQx";
            List<int> scores = null;
            for (int i = 0; i < 10; i++)
            {
                scores = SingleDummySolver.SolveSingleDummy2(trumpSuit, declarer);
            }
            foreach (var score in scores)
            {
                Assert.InRange(score, 10, 12);
            }
        }
    }
}
