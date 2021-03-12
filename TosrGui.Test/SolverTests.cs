using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
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
            var northHand = "KT98,AK96,AKJ9,4";
            var southHand = "Axxx,Qxxx,xx,KQx";

            var declarers = new Dictionary<Suit, Player> { { Suit.Spades, Player.North } };
            var southInformation = new SouthInformation
            {
                Shapes = new [] { "4423" },
                Controls = new MinMax(3, 3),
                Hcp = null,
                SpecificControls = new List<string[]> { new[] { "A", "", "", "K" } },
                Queens = "NYNY"
            };

            var scores = SingleDummySolver.SolveSingleDummy(northHand, southInformation, 10, declarers);

            foreach (var contracts in scores.Keys)
            {
                Assert.InRange(contracts.rank + 6, 10, 12);
            }
        }
    }
}
