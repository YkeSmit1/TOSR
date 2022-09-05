using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Tosr;

namespace Solver
{
    public class SouthInformation
    {
        public IEnumerable<string> Shapes { get; init; }
        public MinMax Hcp { get; init; }
        public MinMax Controls { get; set; }
        public IEnumerable<string[]> SpecificControls { get; set; }
        public string Queens { get; set; }
        public int ControlBidCount { get; init; }
        public int ControlsScanningBidCount { get; init; }

    }

    public class SingleDummySolver
    {
        public static List<int> SolveSingleDummyExactHands(Suit trumpSuit, Player declarer, string northHand, string southHand)
        {
            var handsForSolver = GetHandsForSolverExactHands(northHand, southHand).ToArray();
            return Api.SolveAllBoards(handsForSolver, UtilTosr.GetDdsSuit(trumpSuit), UtilTosr.GetDdsFirst(declarer)).ToList();
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
                    ShuffleAndUpdate(shape);
                else
                    foreach (var specificControls in southInformation.SpecificControls)
                    {
                        shufflingDeal.South.SpecificControls = specificControls;
                        ShuffleAndUpdate(shape);
                    }
            }
            return tricksPerContract;

            void ShuffleAndUpdate(string shape)
            {
                var handsForSolver = shufflingDeal.Execute();
                // TODO extend for multiple trump suits and for NT
                var (suit, length) = Util.GetLongestSuitShape(northHand, shape);
                CalculateAndUpdateDictionary(handsForSolver, length >= 8 ? suit : Suit.NoTrump);
            }

            void CalculateAndUpdateDictionary(IEnumerable<string> handsForSolver, Suit suit)
            {
                foreach (var trick in Api.SolveAllBoards(handsForSolver, UtilTosr.GetDdsSuit(suit), UtilTosr.GetDdsFirst(declarers[suit])))
                    tricksPerContract.AddOrUpdateDictionary(new Bid(trick - 6, suit));
            }
        }
    }
}
