using System.Runtime.InteropServices;
using System.Text;

namespace BiddingLogic
{
    public static class PInvoke
    {
        [DllImport("Engine.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
        public static extern int GetBidFromRuleEx(Fase fase, Fase previousFase, string hand, int lastBidId, out Fase newFase, StringBuilder description);

        [DllImport("Engine.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
        public static extern int GetBidFromRule(Fase fase, Fase previousFase, string hand, int lastBidId, out Fase newFase, out int zoomOffset);

        [DllImport("Engine.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
        public static extern int Setup(string database);

        //[DllImport("Engine.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
        //private static extern void GetBid(int bidId, out int rank, out int suit);
    }
}
