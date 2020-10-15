using System.Runtime.InteropServices;
using System.Text;
using Common;

namespace Tosr
{
    public class Pinvoke
    {
        [DllImport("Engine.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
        public static extern int GetBidFromRuleEx(Fase fase, Fase previousFase, string hand, int lastBidId, out Fase newFase, StringBuilder description);

        [DllImport("Engine.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
        public static extern int GetBidFromRule(Fase fase, Fase previousFase, string hand, int lastBidId, out Fase newFase, out bool zoom);

        [DllImport("Engine.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
        public static extern int Setup(string database);

        //[DllImport("Engine.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
        //private static extern void GetBid(int bidId, out int rank, out int suit);
    }
}
