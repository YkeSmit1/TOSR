using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Solver
{
    internal class Pinvoke
    {
        [DllImport("dds.dll")]
        public static extern int SolveBoardPbn(DealPbn dealPbn, int target, int solutions, int mode, out FutureTricks futureTricks, int threadIndex);
        [DllImport("dds.dll")]
        public static extern int SolveAllBoards(IntPtr boardsPbn, out SolvedBoards solvedBoard);

        [DllImport("dds.dll")]
        public static extern void ErrorMessage(int code, StringBuilder line);
    }
}
