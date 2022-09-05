using System.Runtime.InteropServices;

namespace Solver
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BoardsPbn
    {
        public const int MaxNoOfBoards = 200;

        public int noOfBoards;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = MaxNoOfBoards)]
        public DealPbn[] dealsPbn;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxNoOfBoards)]
        public int[] targets;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxNoOfBoards)]
        public int[] solutions;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxNoOfBoards)]
        public int[] modes;
    }
}