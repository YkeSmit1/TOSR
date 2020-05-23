using System.Runtime.InteropServices;

namespace Tosr
{
    class Pinvoke
    {
        [DllImport("Engine.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
        public static extern int GetBidFromRule(int faceId, string hand, int lastBidId);

        //[DllImport("Engine.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
        //private static extern void GetBid(int bidId, out int rank, out int suit);
    }
}
