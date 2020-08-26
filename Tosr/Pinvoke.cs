using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Tosr
{
    public class Pinvoke : IPinvoke
    {
        public int GetBidFromRuleEx(Fase fase, string hand, int lastBidId, out Fase newFase, StringBuilder description)
        {
            return StaticPinvoke.GetBidFromRuleEx(fase, hand, lastBidId, out newFase, description);
        }
        public int GetBidFromRule(Fase fase, string hand, int lastBidId, out Fase newFase)
        {
            return StaticPinvoke.GetBidFromRule(fase, hand, lastBidId, out newFase);
        }
        public int Setup(string database)
        {
            return StaticPinvoke.Setup(database);
        }

        private class StaticPinvoke
        {
            [DllImport("Engine.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
            public static extern int GetBidFromRuleEx(Fase fase, string hand, int lastBidId, out Fase newFase, StringBuilder description);

            [DllImport("Engine.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
            public static extern int GetBidFromRule(Fase fase, string hand, int lastBidId, out Fase newFase);

            [DllImport("Engine.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
            public static extern int Setup(string database);

            //[DllImport("Engine.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
            //private static extern void GetBid(int bidId, out int rank, out int suit);
        }
    }

}
