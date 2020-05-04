using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Tosr
{
    class Pinvoke
    {
        [DllImport("Engine.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
        public static extern int GetBidFromRule(int faceId, string hand, int lastBidId);

        [DllImport("Engine.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
        public static extern void GetBid(int bidId, out int rank, out int suit);
    }
}
