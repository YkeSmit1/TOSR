using System.Runtime.InteropServices;

namespace Solver
{
    public struct DealPbn
    {
        // Spades = 0 Hearts = 1 Diamonds = 2 Clubs = 3 NT = 4
        public int trump;

        // North = 0 East = 1 South = 2 West 3
        public int first;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public int[] currentTrickSuit;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public int[] currentTrickRank;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
        public char[] remainCards;
    }
}
