using System.Runtime.InteropServices;
using System.Text;

namespace Tosr
{
    class Pinvoke
    {
        [DllImport("Engine.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
        public static extern int GetBidFromRuleEx(Fase fase, string hand, int lastBidId, out Fase newFase, StringBuilder description);

        [DllImport("Engine.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
        public static extern int GetBidFromRule(Fase fase, string hand, int lastBidId, out Fase newFase);

        //[DllImport("Engine.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
        //private static extern void GetBid(int bidId, out int rank, out int suit);
    }
}
