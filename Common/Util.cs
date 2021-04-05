using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Common
{
    public static class DictionaryExtension 
    {
        public static void AddOrUpdateDictionary<T>(this IDictionary<T, int> dictionary, T item)
        {
            if (!dictionary.ContainsKey(item))
                dictionary.Add(item, 1);
            else
                dictionary[item]++;
        }
    }

    public enum Player
    {
        West,
        North,
        East,
        South,
        UnKnown
    };

    public enum Suit
    {
        Clubs = 0,
        Diamonds = 1,
        Hearts = 2,
        Spades = 3,
        NoTrump = 4
    }

    public enum Face
    {
        Ace,
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Ten,
        Jack,
        Queen,
        King
    }

    public enum Fase
    {
        Unknown,
        Shape,
        Controls,
        ScanningControls,
        ScanningOther,
        End,
        Pull3NTNoAsk,
        Pull3NTOneAskMin,
        Pull3NTOneAskMax,
        Pull3NTTwoAsks,
        Pull4DiamondsNoAsk,
        Pull4DiamondsOneAskMin,
        Pull4DiamondsOneAskMax,
        BidGame,
    };

    public enum ExpectedContract
    {
        Game,
        SmallSlam,
        GrandSlam,
    }

    public static class Util
    {
        public static readonly List<Fase> signOffFasesFor3NT = new List<Fase> { Fase.Pull3NTNoAsk, Fase.Pull3NTOneAskMin, Fase.Pull3NTOneAskMax, Fase.Pull3NTTwoAsks };
        public static readonly List<Fase> signOffFasesFor4Di = new List<Fase> { Fase.Pull4DiamondsNoAsk, Fase.Pull4DiamondsOneAskMin, Fase.Pull4DiamondsOneAskMax };
        public static readonly List<Fase> signOffFases = signOffFasesFor3NT.Concat(signOffFasesFor4Di).ToList();
        public static readonly List<Fase> signOffFasesWithout3NTNoAsk = signOffFasesFor4Di.Concat(new[] { Fase.Pull3NTOneAskMin, Fase.Pull3NTOneAskMax, Fase.Pull3NTTwoAsks }).ToList();

        public static string GetSuitDescription(Suit suit)
        {
            return suit switch
            {
                Suit.Clubs => "♣", // \u2663
                Suit.Diamonds => "♦", // \u2666
                Suit.Hearts => "♥", // \u2665
                Suit.Spades => "♠", // \u2660
                Suit.NoTrump => "NT",
                _ => throw new ArgumentOutOfRangeException(nameof(suit), suit, null),
            };
        }

        public static string GetSuitDescriptionASCII(Suit suit)
        {
            return suit switch
            {
                Suit.Clubs => "C",
                Suit.Diamonds => "D",
                Suit.Hearts => "H",
                Suit.Spades => "S",
                Suit.NoTrump => "NT",
                _ => throw new ArgumentOutOfRangeException(nameof(suit), suit, null),
            };
        }

        public static Suit GetSuit(string suit)
        {
            return suit switch
            {
                "♣" => Suit.Clubs, // \u2663
                "♦" => Suit.Diamonds, // \u2666
                "♥" => Suit.Hearts, // \u2665
                "♠" => Suit.Spades, // \u2660
                "NT" => Suit.NoTrump,
                _ => throw new ArgumentOutOfRangeException(nameof(suit), suit, null),
            };
        }

        public static Suit GetSuitASCII(string suit)
        {
            return suit switch
            {
                "C" => Suit.Clubs,
                "D" => Suit.Diamonds,
                "H" => Suit.Hearts,
                "S" => Suit.Spades,
                "NT" => Suit.NoTrump,
                _ => throw new ArgumentOutOfRangeException(nameof(suit), suit, null),
            };
        }

        public static char GetFaceDescription(Face face)
        {
            return face switch
            {
                Face.Ace => 'A',
                Face.Two => '2',
                Face.Three => '3',
                Face.Four => '4',
                Face.Five => '5',
                Face.Six => '6',
                Face.Seven => '7',
                Face.Eight => '8',
                Face.Nine => '9',
                Face.Ten => 'T',
                Face.Jack => 'J',
                Face.Queen => 'Q',
                Face.King => 'K',
                _ => throw new ArgumentOutOfRangeException(nameof(face), face, null),
            };
        }

        public static Face GetFaceFromDescription(char c)
        {
            return c switch
            {
                'A' => Face.Ace,
                '2' => Face.Two,
                '3' => Face.Three,
                '4' => Face.Four,
                '5' => Face.Five,
                '6' => Face.Six,
                '7' => Face.Seven,
                '8' => Face.Eight,
                '9' => Face.Nine,
                'T' => Face.Ten,
                'J' => Face.Jack,
                'Q' => Face.Queen,
                'K' => Face.King,
                _ => throw new ArgumentOutOfRangeException(nameof(c), c, null),
            };
        }

        public static bool IsSameTeam(Player player1, Player player2)
        {
            return Math.Abs(player1 - player2) == 2 || 
                (player1 == player2) || 
                (player2 == Player.UnKnown || player1 == Player.UnKnown);
        }

        public static (Suit, int) GetLongestSuit(string northHand, string southHand)
        {
            var suitLengthNorth = northHand.Split(',').Select(x => x.Length);
            Debug.Assert(suitLengthNorth.Count() == 4);
            var suitLengthSouth = southHand.Split(',').Select(x => x.Length);
            Debug.Assert(suitLengthSouth.Count() == 4);
            var suitLengthNS = suitLengthNorth.Zip(suitLengthSouth, (x, y) => x + y);
            var maxSuitLength = suitLengthNS.Max();
            var longestSuit = suitLengthNS.ToList().IndexOf(maxSuitLength);
            return ((Suit)(3 - longestSuit), maxSuitLength);
        }

        public static (Suit, int) GetLongestSuitShape(string northHand, string southHandShape)
        {
            var suitLengthNorth = northHand.Split(',').Select(x => x.Length);
            Debug.Assert(suitLengthNorth.Count() == 4);
            var suitLengthSouth = southHandShape.Select(x => int.Parse(x.ToString()));
            Debug.Assert(suitLengthSouth.Count() == 4);
            var suitLengthNS = suitLengthNorth.Zip(suitLengthSouth, (x, y) => x + y);
            var maxSuitLength = suitLengthNS.Max();
            var longestSuit = suitLengthNS.ToList().IndexOf(maxSuitLength);
            return ((Suit)(3 - longestSuit), maxSuitLength);
        }


        public static IEnumerable<(Suit suit, int length)> GetSuitsWithFit(string northHand, string southHand)
        {
            var suitLengthNorth = northHand.Split(',').Select(x => x.Length);
            Debug.Assert(suitLengthNorth.Count() == 4);
            var suitLengthSouth = southHand.Split(',').Select(x => x.Length);
            Debug.Assert(suitLengthSouth.Count() == 4);
            var suitLengthNS = suitLengthNorth.Zip(suitLengthSouth, (x, y) => x + y);
            return suitLengthNS.Select((length, index) => ((Suit)(3 - index), length)).OrderByDescending(x => x.length).TakeWhile(x => x.length >= 8);
        }

        public static int GetNumberOfTrumps(Suit suit, string northHand, string southHand)
        {
            Debug.Assert(suit != Suit.NoTrump);
            var suitLengthNorth = northHand.Split(',')[3 - (int)suit].Length;
            var suitLengthSouth = southHand.Split(',')[3 - (int)suit].Length;
            return suitLengthNorth + suitLengthSouth;
        }

        public static bool IsFreakHand(IEnumerable<int> handLength)
        {
            var handPattern = handLength.OrderByDescending(y => y).ToArray();
            return int.Parse(handPattern[0].ToString()) >= 8 ||
                int.Parse(handPattern[0].ToString()) + int.Parse(handPattern[1].ToString()) >= 12;
        }

        public static int GetHcpCount(string hand)
        {
            return hand.Count(x => x == 'J') + hand.Count(x => x == 'Q') * 2 + hand.Count(x => x == 'K') * 3 + hand.Count(x => x == 'A') * 4;
        }

        public static int GetHcpCount(params string[] suits)
        {
            return suits.Sum(suit => suit.Count(x => x == 'J') + suit.Count(x => x == 'Q') * 2 + suit.Count(x => x == 'K') * 3 + suit.Count(x => x == 'A') * 4);
        }

        public static int GetControlCount(string hand)
        {
            return hand.Count(x => x == 'K') + hand.Count(x => x == 'A') * 2;
        }

        public static Suit GetTrumpSuitShape(string northHand, string southHandShape)
        {
            var southHand = string.Join(',', southHandShape.Select(x => new string('x', int.Parse(x.ToString()))));
            return GetTrumpSuit(northHand, southHand);
        }

        public static Suit GetTrumpSuit(string northHand, string southHand)
        {
            Debug.Assert(northHand.Length == 16);
            Debug.Assert(southHand.Length == 16);
            // TODO Use single dummy analyses to find out the best trump suit
            var (longestSuit, suitLength) = GetLongestSuit(northHand, southHand);
            // If we have a major fit return the major
            if (new List<Suit> { Suit.Spades, Suit.Hearts }.Contains(longestSuit))
                return (suitLength < 8) ? Suit.NoTrump : longestSuit;
            // Only wants to play a minor if we have a singleton and 9 or more trumps
            if (suitLength > 8 && (northHand.Split(',').Select(x => x.Length).Min() <= 1 || southHand.Split(',').Select(x => x.Length).Min() <= 1))
                return longestSuit;
            return Suit.NoTrump;
        }

        public static string GetHandWithOnlyControlsAs4333(string handsString, string honors)
        {
            return new string(string.Join(',', handsString.Split(',').OrderByDescending(x => x.Length).
                Select((x, index) => Regex.Replace(x, $"[^{honors}]", string.Empty).PadRight(index == 0 ? 4 : 3, 'x'))));
        }

        public static bool TryAddQuacksTillHCP(int hcp, ref string[] suits, int[] suitLength)
        {
            if (hcp <= GetHcpCount(suits))
                return true;

            int suitToAdd = 3;
            while (hcp - GetHcpCount(suits) > 1)
            {
                if (suitToAdd == 0)
                    break;

                if (suits[suitToAdd].Length < suitLength[suitToAdd])
                    suits[suitToAdd] += 'Q';
                suitToAdd--;
            };

            suitToAdd = 3;
            while (hcp != GetHcpCount(suits))
            {
                if (suitToAdd == 0)
                    return false;

                if (suits[suitToAdd].Length < suitLength[suitToAdd])
                    suits[suitToAdd] += 'J';

                suitToAdd--;
            };

            return hcp == GetHcpCount(suits);
        }

        public static bool TryAddJacksTillHCP(int hcp, ref string[] suits, int[] suitLength)
        {
            if (hcp <= GetHcpCount(suits))
                return true;

            int suitToAdd = 3;
            while (hcp != GetHcpCount(suits))
            {
                if (suitToAdd == 0)
                    return false;

                if (suits[suitToAdd].Length < suitLength[suitToAdd])
                    suits[suitToAdd] += 'J';

                suitToAdd--;
            };

            return hcp == GetHcpCount(suits);
        }

        public static int NrOfShortages(string hand)
        {
            return hand.Select(x => int.Parse(x.ToString())).Count(y => y <= 1);
        }

        public static int GetDDSFirst(Player declarer)
        {
            int declarerDDS = 3 - (int)declarer;
            return declarerDDS == 3 ? 0 : declarerDDS + 1;            
        }

        public static int GetDDSSuit(Suit suit)
        {
            return suit == Suit.NoTrump ? (int)suit : 3 - (int)suit;
        }

        public static (ExpectedContract expectedContract, Dictionary<ExpectedContract, int> confidence) GetExpectedContract(IEnumerable<int> scores)
        {
            ExpectedContract expectedContract;
            if ((double)scores.Count(x => x == 13) / (double)scores.Count() > .6)
                expectedContract = ExpectedContract.GrandSlam;
            else if ((double)scores.Count(x => x == 12) / (double)scores.Count() > .6)
                expectedContract = ExpectedContract.SmallSlam;
            else if ((double)scores.Count(x => x == 12 || x == 13) / (double)scores.Count() > .6)
                expectedContract = scores.Count(x => x == 12) >= scores.Count(x => x == 13) ? ExpectedContract.SmallSlam : ExpectedContract.GrandSlam;
            else expectedContract = ExpectedContract.Game;

            return (expectedContract, new Dictionary<ExpectedContract, int> { 
                {ExpectedContract.GrandSlam, scores.Count(x => x == 13) }, 
                { ExpectedContract.SmallSlam, scores.Count(x => x == 12) }, 
                { ExpectedContract.Game, scores.Count(x => x < 12)}});
        }

        public static Player GetPlayer(string player) => player switch
        {
            "N" => Player.North,
            "E" => Player.East,
            "S" => Player.South,
            "W" => Player.West,
            _ => Player.UnKnown,
        };

        public static string GetPlayerString(Player player) => player switch
        {
            Player.North => "N",
            Player.East => "E",
            Player.South => "S",
            Player.West => "W",
            _ => throw new ArgumentException("Unknown player"),
        };

        public static string[] GetBoardsTosr(string board)
        {
            var boardNoDealer = board[2..].Replace('.', ',');
            var suits = boardNoDealer.Split(" ");
            var suitsNFirst = suits.ToList().Rotate(3);
            return suitsNFirst.ToArray();
        }

        public static string HandWithx(string hand)
        {
            return Regex.Replace(hand, "[QJT98765432]", "x");
        }

        public static string ReadResource(string resourceName)
        {
            var assembly = Assembly.LoadFrom("Tosr.dll");
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}