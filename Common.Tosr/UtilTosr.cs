using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Common.Tosr
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

    public static class UtilTosr
    {
        public static bool IsFreakHand(IEnumerable<int> handLength)
        {
            var handPattern = handLength.OrderByDescending(y => y).ToArray();
            return int.Parse(handPattern[0].ToString()) >= 8 ||
                   int.Parse(handPattern[0].ToString()) + int.Parse(handPattern[1].ToString()) >= 12;
        }

        public static string GetHandWithOnlyControlsAs4333(string handsString, string honors)
        {
            return new string(string.Join(',', handsString.Split(',').OrderByDescending(x => x.Length).
                Select((x, index) => Regex.Replace(x, $"[^{honors}]", string.Empty).PadRight(index == 0 ? 4 : 3, 'x'))));
        }

        public static bool TryAddQuacksTillHcp(int hcp, ref string[] suits, int[] suitLength)
        {
            if (hcp <= Util.GetHcpCount(suits))
                return true;

            var suitToAdd = 3;
            while (hcp - Util.GetHcpCount(suits) > 1)
            {
                if (suitToAdd == 0)
                    break;

                if (suits[suitToAdd].Length < suitLength[suitToAdd])
                    suits[suitToAdd] += 'Q';
                suitToAdd--;
            }

            suitToAdd = 3;
            while (hcp != Util.GetHcpCount(suits))
            {
                if (suitToAdd == 0)
                    return false;

                if (suits[suitToAdd].Length < suitLength[suitToAdd])
                    suits[suitToAdd] += 'J';

                suitToAdd--;
            }

            return hcp == Util.GetHcpCount(suits);
        }

        public static int NrOfShortages(string hand)
        {
            return hand.Select(x => int.Parse(x.ToString())).Count(y => y <= 1);
        }

        public static int GetDdsFirst(Player declarer)
        {
            var declarerDds = 3 - (int)declarer;
            return declarerDds == 3 ? 0 : declarerDds + 1;
        }

        public static int GetDdsSuit(Suit suit)
        {
            return suit == Suit.NoTrump ? (int)suit : 3 - (int)suit;
        }

        public static string ReadResource(string resourceName)
        {
            var assembly = Assembly.LoadFrom("BiddingLogic.dll");
            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream ?? throw new InvalidOperationException());
            return reader.ReadToEnd();
        }

        public static string HandWithX(string hand)
        {
            return Regex.Replace(hand, "[QJT98765432]", "x");
        }
    }
}