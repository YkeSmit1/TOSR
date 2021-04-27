using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Common;

namespace Solver
{
    public class SouthInformation
    {
        public IEnumerable<string> Shapes { get; set; }
        public MinMax Hcp { get; set; }
        public MinMax Controls { get; set; }
        public IEnumerable<string[]> SpecificControls { get; set; }
        public string Queens { get; set; }
        public int ControlBidCount { get; set; }
        public int ControlsScanningBidCount { get; set; }

    }

    public class SingleDummySolver
    {
        public static List<int> SolveSingleDummyExactHands(Suit trumpSuit, Player declarer, string northHand, string southHand)
        {
            var handsForSolver = GetHandsForSolverExactHands(northHand, southHand).ToArray();
            return Api.SolveAllBoards(handsForSolver, Util.GetDDSSuit(trumpSuit), Util.GetDDSFirst(declarer)).ToList();
        }

        private static IEnumerable<string> GetHandsForSolverExactHands(string northHandStr, string southHandStr)
        {
            var shufflingDeal = new ShufflingDeal
            {
                North = new North { Hand = northHandStr.Split(',') },
                South = new South { Hand = southHandStr.Split(',') }
            };
            return shufflingDeal.Execute();
        }

        public static Dictionary<Bid, int> SolveSingleDummy(string northHand, SouthInformation southInformation, int numberOfHands, Dictionary<Suit, Player> declarers)
        {
            var tricksPerContract = new Dictionary<Bid, int>();
            var shufflingDeal = new ShufflingDeal
            {
                North = new North { Hand = northHand.Split(',') },
                South = new South { Controls = southInformation.Controls, Hcp = southInformation.Hcp, Queens = southInformation.Queens },
                NrOfHands = numberOfHands
            };

            foreach (var shape in southInformation.Shapes)
            {
                shufflingDeal.South.Shape = shape;

                if (southInformation.SpecificControls == null)
                    ShuffleAndUpdate(shufflingDeal, shape);
                else
                    foreach (var specificControls in southInformation.SpecificControls)
                    {
                        shufflingDeal.South.SpecificControls = specificControls;
                        ShuffleAndUpdate(shufflingDeal, shape);
                    }
            }
            return tricksPerContract;

            void ShuffleAndUpdate(ShufflingDeal shufflingDeal, string shape)
            {
                var handsForSolver = shufflingDeal.Execute();
                // TODO extend for multiple trump suits and for NT
                var (suit, length) = Util.GetLongestSuitShape(northHand, shape);
                CalculateAndUpdateDictionary(handsForSolver, length >= 8 ? suit : Suit.NoTrump);
            }

            void CalculateAndUpdateDictionary(IEnumerable<string> handsForSolver, Suit suit)
            {
                foreach (var trick in Api.SolveAllBoards(handsForSolver, Util.GetDDSSuit(suit), Util.GetDDSFirst(declarers[suit])))
                    tricksPerContract.AddOrUpdateDictionary(new Bid(trick - 6, suit));
            }
        }
    }
}
