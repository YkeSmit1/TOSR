using System;
using System.Runtime.InteropServices;

namespace Solver
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BoardsPBN
    {
        public const int MAXNOOFBOARDS = 2;

        public int noOfBoards;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = MAXNOOFBOARDS)]
        public DealPbn[] dealsPBN;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXNOOFBOARDS)]
        public int[] targets;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXNOOFBOARDS)]
        public int[] solutions;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXNOOFBOARDS)]
        public int[] modes;
    }
}