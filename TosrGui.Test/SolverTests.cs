using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Solver;
using static Solver.DealHands;
using Xunit.Abstractions;

namespace TosrGui.Test
{
    public class SolverTests
    {
        private readonly ITestOutputHelper output;

        public SolverTests(ITestOutputHelper output)
        {
            this.output = output;
        }

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

        [Fact()]
        public void Test2()
        {
            int trumpSuit = 0; // Spades
            int declarer = 0; // North
            var northHandStr = "KT98,AK96,AKJ9,4";
            SouthHandInfo southHandInfo = new SouthHandInfo
            {
                shape = "4423",
                minControls = 2,
                maxControls = 4,
                DCB1 = new int?[] { 1, 0, 0, 1 },
                DCB2 = new int?[] { 0, 1, 0, 1 }
            };
            var scores = SingleDummySolver.SolveSingleDummy2(trumpSuit, declarer, northHandStr, southHandInfo);
            foreach (var score in scores)
            {
                Assert.InRange(score, 10, 12);
            }
        }

        [Fact()]
        public void DealerTest()
        {
            SouthHandInfo southHandInfo = new SouthHandInfo();
            southHandInfo.shape = "4432";
            southHandInfo.minControls = 4;
            southHandInfo.maxControls = 5;
            southHandInfo.DCB1 = new int?[] { 1, 0, 1, 1 };
            southHandInfo.DCB2 = new int?[] { 0, 0, 1, 0 };
            output.WriteLine(getTCLInput("5432, 32, 32, 65432", southHandInfo));
        }
    }
}