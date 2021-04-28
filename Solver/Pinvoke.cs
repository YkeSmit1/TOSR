using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Solver
{
    internal class Pinvoke
    {
        [DllImport("dds.dll")]
        public static extern int SolveBoardPBN(DealPbn dealPBN, int target, int solutions, int mode, out FutureTricks futureTricks, int threadIndex);
        [DllImport("dds.dll")]
        public static extern int SolveAllBoards(IntPtr boardsPBN, out SolvedBoards solvedBoard);

        [DllImport("dds.dll")]
        public static extern void ErrorMessage(int code, StringBuilder line);
    }
}
