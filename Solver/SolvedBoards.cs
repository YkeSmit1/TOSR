using System.Runtime.InteropServices;

namespace Solver
{
    public struct SolvedBoards
    {
        public int noOfBoards;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = BoardsPbn.MaxNoOfBoards)]
        public FutureTricks[] solvedBoards;
    }
}