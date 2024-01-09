using Xunit;
using BiddingLogic;
using System;
using System.Collections.Generic;
using Common;
using System.Linq;
// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace TosrGui.Test
{
    public class TestCaseProvider
    {
        public static IEnumerable<object[]> TestCasesNoPull()
        {
            // ♣♦♥♠
            yield return new object[] { "Simple", 0, new[]{
                "1♣", "3♦",
                "3♥", "3NT",
                "4♣", "4♦" },
            new[]{
                Phase.Unknown, Phase.Shape,
                Phase.Unknown, Phase.Controls,
                Phase.Unknown, Phase.ScanningControls },
            new[]{
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown },
                "3NT4♦" };


            yield return new object[] { "Use4ClAsRelay", 0, new[]{
                "1♣", "3♦",
                "3♥", "3♠",
                "4♣", "4♦" },
            new[]{
                Phase.Unknown, Phase.Shape,
                Phase.Unknown, Phase.Controls,
                Phase.Unknown, Phase.ScanningControls },
            new[]{
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown },
                "3♠4♣" };

            yield return new object[] { "OffSet", 0, new[]{
                "1♣", "3♣",
                "3♦", "3♥",
                "3♠", "3NT" },
            new[]{
                Phase.Unknown, Phase.Shape,
                Phase.Unknown, Phase.Controls,
                Phase.Unknown, Phase.ScanningControls },
            new[]{
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown },
                "3♠4♣" };

            yield return new object[] { "OffSetUse4ClAsRelay", 0, new[]{
                "1♣", "3♣",
                "3♦", "3♠",
                "4♣", "4♦" },
            new[]{
                Phase.Unknown, Phase.Shape,
                Phase.Unknown, Phase.Controls,
                Phase.Unknown, Phase.ScanningControls },
            new[]{
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown },
                "3NT4♦" };

            // Zoom test
            yield return new object[] { "SimpelZoom", 2, new[]{
                "1♣", "3♦",
                "3♥", "3NT",
                "4♣", "4♦" },
            new[]{
                Phase.Unknown, Phase.Shape,
                Phase.Unknown, Phase.Controls,
                Phase.Unknown, Phase.ScanningControls },
            new[]{
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown },
                "3♠4♦4♠" };

            yield return new object[] { "Use4ClAsRelayZoom", 2, new[]{
                "1♣", "3♦",
                "3♥", "3♠",
                "4♣", "4♦" },
            new[]{
                Phase.Unknown, Phase.Shape,
                Phase.Unknown, Phase.Controls,
                Phase.Unknown, Phase.ScanningControls },
            new[]{
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown },
                "3♠4♣4♥" };

            // ♣♦♥♠
            yield return new object[] { "OffSetZoom", 2, new[]{
                "1♣", "3♣",
                "3♦", "3♥",
                "3♠", "3NT" },
            new[]{
                Phase.Unknown, Phase.Shape,
                Phase.Unknown, Phase.Controls,
                Phase.Unknown, Phase.ScanningControls },
            new[]{
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown },
                "3♠4♣4♥" };
            // ♣♦♥♠
            yield return new object[] { "OffSetUse4ClAsRelayZoom", 2, new[]{
                "1♣", "3♣",
                "3♦", "3♠",
                "4♣", "4♦" },
            new[]{
                Phase.Unknown, Phase.Shape,
                Phase.Unknown, Phase.Controls,
                Phase.Unknown, Phase.ScanningControls },
            new[]{
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown },
                "3♠4♦4♠" };

            // Pull tests
        }
        public static IEnumerable<object[]> TestCasesPull3NT()
        {
            // ♣♦♥♠
            yield return new object[] { "Simple0", 0, new[]{
                "1♣", "3♦",
                "3NT", "4♦",
                "4♥", "4♠",
                "4NT", "5♣"},
            new[]{
                Phase.Unknown, Phase.Shape,
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Controls,
                Phase.Unknown, Phase.ScanningControls},
            new[]{
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Pull3NTNoAsk,
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown },
                "3♠4♣" };

            yield return new object[] { "Simple0_", 0, new[]{
                "1♣", "3♦",
                "3NT", "4NT",
                "5♣", "5♦",
                "5♥", "5♠"},
            new[]{
                Phase.Unknown, Phase.Shape,
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Controls,
                Phase.Unknown, Phase.ScanningControls },
            new[]{
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Pull3NTNoAsk,
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown },
                "3♠4♣" };

            yield return new object[] { "Simple1", 0, new[]{
                "1♣", "3♦",
                "3♥", "3♠",
                "3NT", "4♣" },
            new[]{
                Phase.Unknown, Phase.Shape,
                Phase.Unknown, Phase.Controls,
                Phase.Unknown, Phase.Controls },
            new[]{
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Pull3NTOneAskMin },
                "3♠4♣" };

            // ♣♦♥♠
            yield return new object[] { "OffSet0", 0, new[]{
                "1♣", "3♣",
                "3NT", "4♣",
                "4♦", "4NT",
                "5♣", "5NT"},
            new[]{
                Phase.Unknown, Phase.Shape,
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Controls,
                Phase.Unknown, Phase.ScanningControls },
            new[]{
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Pull3NTNoAsk,
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown },
                "4♣5♣" };

            yield return new object[] { "OffSet1", 0, new[]{
                "1♣", "3♣",
                "3♦", "3♥",
                "3NT", "4♣" },
            new[]{
                Phase.Unknown, Phase.Shape,
                Phase.Unknown, Phase.Controls,
                Phase.Unknown, Phase.Controls },
            new[]{
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Pull3NTOneAskMin },
                "3♠4♣" };

            // Zoom test
            yield return new object[] { "SimpelZoom0", 2, new[]{
                "1♣", "3♦",
                "3NT", "4♦",
                "4♥", "4♠",
                "4NT", "5♣" },
            new[]{
                Phase.Unknown, Phase.Shape,
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Controls,
                Phase.Unknown, Phase.ScanningControls },
            new[]{
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Pull3NTNoAsk,
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown },
                "3♠4♣4♥" };

            yield return new object[] { "SimpelZoom1", 2, new[]{
                "1♣", "3♦",
                "3♥", "3♠",
                "3NT", "4♣" },
            new[]{
                Phase.Unknown, Phase.Shape,
                Phase.Unknown, Phase.Controls,
                Phase.Unknown, Phase.Controls },
            new[]{
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Pull3NTOneAskMin },
                "3♠4♣4♥" };

            // ♣♦♥♠
            yield return new object[] { "OffSetZoom0", 2, new[]{
                "1♣", "3♣",
                "3NT", "4♣",
                "4♦", "4♥",
                "4♠", "5♦"},
            new[]{
                Phase.Unknown, Phase.Shape,
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Controls,
                Phase.Unknown, Phase.ScanningControls },
            new[]{
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Pull3NTNoAsk,
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown },
                "3♠4♣4NT" };

            yield return new object[] { "OffSetZoom1", 2, new[]{
                "1♣", "3♣",
                "3♦", "3♥",
                "3NT", "4♣" },
            new[]{
                Phase.Unknown, Phase.Shape,
                Phase.Unknown, Phase.Controls,
                Phase.Unknown, Phase.Controls },
            new[]{
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Pull3NTOneAskMin },
                "3♠4♣4♥" };

            // Pull tests
        }
        public static IEnumerable<object[]> TestCasesPull4Di()
        {
            // ♣♦♥♠
            yield return new object[] { "Simple0", 0, new[]{
                "1♣", "3♦",
                "4♦", "4NT",
                "5♣", "5♦" },
            new[]{
                Phase.Unknown, Phase.Shape,
                Phase.Unknown, Phase.Controls,
                Phase.Unknown, Phase.ScanningControls },
            new[]{
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Pull4DiamondsNoAsk,
                Phase.Unknown, Phase.Unknown },
                "3NT4♦" };

            yield return new object[] { "Simple1", 0, new[]{
                "1♣", "3♦",
                "3♥", "3NT",
                "4♦", "4♠" },
            new[]{
                Phase.Unknown, Phase.Shape,
                Phase.Unknown, Phase.Controls,
                Phase.Unknown, Phase.ScanningControls },
            new[]{
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Pull4DiamondsOneAskMax },
                "3NT4♦" };

            yield return new object[] { "OffSet0", 0, new[]{
                "1♣", "3♣",
                "4♦", "4♠",
                "4NT", "5♣" },
            new[]{
                Phase.Unknown, Phase.Shape,
                Phase.Unknown, Phase.Controls,
                Phase.Unknown, Phase.ScanningControls },
            new[]{
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Pull4DiamondsNoAsk,
                Phase.Unknown, Phase.Unknown },
                "3♠4♣" };

            yield return new object[] { "OffSet1", 0, new[]{
                "1♣", "3♣",
                "3♦", "3♥",
                "4♦", "4♠" },
            new[]{
                Phase.Unknown, Phase.Shape,
                Phase.Unknown, Phase.Controls,
                Phase.Unknown, Phase.Controls },
            new[]{
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Pull4DiamondsOneAskMin },
                "3♠4♣" };

            // Zoom test
            yield return new object[] { "SimpelZoom", 2, new[]{
                "1♣", "3♦",
                "4♦", "4NT",
                "5♣", "5♦" },
            new[]{
                Phase.Unknown, Phase.Shape,
                Phase.Unknown, Phase.Controls,
                Phase.Unknown, Phase.ScanningControls },
            new[]{
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Pull4DiamondsOneAskMax,
                Phase.Unknown, Phase.Unknown },
                "3♠4♦4♠" };

            yield return new object[] { "OffSetZoom", 2, new[]{
                "1♣", "3♣",
                "4♦", "4♠",
                "4NT", "5♣" },
            new[]{
                Phase.Unknown, Phase.Shape,
                Phase.Unknown, Phase.Controls,
                Phase.Unknown, Phase.ScanningControls },
            new[]{
                Phase.Unknown, Phase.Unknown,
                Phase.Unknown, Phase.Pull4DiamondsOneAskMin,
                Phase.Unknown, Phase.Unknown },
                "3♠4♣4♥" };
        }
    }

    [Collection("Sequential")]
    public class OffSetTests
    {
        [Theory]
        [MemberData(nameof(TestCaseProvider.TestCasesNoPull), MemberType = typeof(TestCaseProvider))]
        public void GetAuctionForPhaseWithOffsetNoPullTest(string testName, int zoomOffset, string[] bids, Phase[] phases, Phase[] pullPhases, string expected)
        {
            TestPhaseWithOffset(testName, zoomOffset, bids, phases, pullPhases, expected);
        }

        [Theory]
        [MemberData(nameof(TestCaseProvider.TestCasesPull3NT), MemberType = typeof(TestCaseProvider))]
        public void GetAuctionForPhaseWithOffsetPull3NTTest(string testName, int zoomOffset, string[] bids, Phase[] phases, Phase[] pullPhases, string expected)
        {
            TestPhaseWithOffset(testName, zoomOffset, bids, phases, pullPhases, expected);
        }

        [Theory]
        [MemberData(nameof(TestCaseProvider.TestCasesPull4Di), MemberType = typeof(TestCaseProvider))]
        public void GetAuctionForPhaseWithOffsetPull4DiTest(string testName, int zoomOffset, string[] bids, Phase[] phases, Phase[] pullPhases, string expected)
        {
            TestPhaseWithOffset(testName, zoomOffset, bids, phases, pullPhases, expected);
        }


        private static void TestPhaseWithOffset(string testName, int zoomOffset, string[] bids, Phase[] phases, Phase[] pullPhases, string expected)
        {
            if (testName is null)
            {
                throw new ArgumentNullException(nameof(testName));
            }
            var auction = new Auction();
            Assert.Equal(bids.Length, phases.Length);
            SetBids(auction, bids);
            var biddingState = new BiddingState(new Dictionary<Phase, bool>());
            var enumerable = phases.Zip(pullPhases, (x, y) => (x, y)).Zip(bids, (x, z) => (z, x.x, x.y));
            foreach (var x in enumerable)
            {
                if (x.x != Phase.Unknown)
                    biddingState.BidsPerPhase.Add((x.x, StringToBid(x.z)));
                if (x.y != Phase.Unknown)
                    biddingState.BidsPerPhase.Add((x.y, StringToBid(x.z)));
            }

            var actual = string.Join("", BiddingInformation.GetAuctionForPhaseWithOffset(auction, zoomOffset, biddingState, Phase.Controls, Phase.ScanningControls));
            Assert.Equal(expected, actual);
        }

        private static void SetBids(Auction auction, IEnumerable<string> bidsStr)
        {
            auction.Bids.Clear();
            var biddingRound = 1;
            var player = Player.North;
            var lbids = bidsStr.Select(bidstr => StringToBid(bidstr));
            foreach (var bid in lbids)
            {
                if (player == Player.North)
                {
                    auction.Bids[biddingRound] = new Dictionary<Player, Bid>(new List<KeyValuePair<Player, Bid>> { new(player, bid) });
                    player = Player.South;
                }
                else
                {
                    auction.Bids[biddingRound].Add(player, bid);
                    biddingRound++;
                    player = Player.North;
                }
            }
        }

        private static Bid StringToBid(string bidstr)
        {
            return new Bid(int.Parse(bidstr.Substring(0, 1)), Util.GetSuit(bidstr[1..]));
        }
    }
}