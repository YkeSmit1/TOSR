using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Common;

namespace Solver
{
    public class SingleDummySolver
    {
        public static List<int> SolveSingleDummy(Suit trumpSuit, Player declarer, string northHand, string southHand)
        {
            var handsForSolver = GetHandsForSolver(northHand, southHand).ToArray();
            return Api.SolveAllBoards(handsForSolver, Util.GetDDSSuit(trumpSuit), Util.GetDDSFirst(declarer)).ToList();
        }

        private static IEnumerable<string> GetHandsForSolver(string northHandStr, string southHandStr)
        {
            var southHand = southHandStr.Split(',');
            var queens = new string(southHand.Select(x => x.Contains('Q') ? 'Y' : 'N').ToArray());
            var controlsSpecific = southHand.Select(x => Regex.Match(x, "[AK]").ToString()).ToArray();
            var controls = Util.GetControlCount(southHandStr);

            var shuflingDeal = new ShufflingDeal
            {
                North = new North { Hand = northHandStr.Split(',') },
                South = new South { Shape = string.Join("", southHand.Select(x => x.Length.ToString())), 
                    Controls = new MinMax(controls, controls), SpecificControls = controlsSpecific, Queens = queens}
            };
            return shuflingDeal.Execute();
        }

        public static List<int> SolveSingleDummy(Suit trumpSuit, Player declarer, string northHand, string southHandShape, int minControls, int maxControls)
        {
            var handsForSolver = GetHandsForSolver(northHand, southHandShape, minControls, maxControls).ToArray();
            return Api.SolveAllBoards(handsForSolver, Util.GetDDSSuit(trumpSuit), Util.GetDDSFirst(declarer)).ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="northHandStr">Whole northhand. Cannot contain x's</param>
        /// <param name="southHandShape">For example 5431</param>
        /// <param name="minControls">Number of controls in the southhand</param>
        /// <param name="nrOfHands"></param>
        /// <returns></returns>
        private static IEnumerable<string> GetHandsForSolver(string northHandStr, string southHandShape, int minControls, int maxControls)
        {
            var shufflingDeal = new ShufflingDeal
            {
                North = new North { Hand = northHandStr.Split(',')},
                South = new South { Shape = southHandShape, Controls = new MinMax(minControls, maxControls) },
            };

            return shufflingDeal.Execute().ToArray();
        }
    }
}
