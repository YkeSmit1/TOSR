﻿using Common;
using Common.Test;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BiddingLogic;
using Xunit;
using Xunit.Abstractions;

namespace TosrIntegration.Test
{
    public class TestCaseProviderRelayBid
    {
        public static IEnumerable<object[]> TestCases3NT()
        {
            // ♣♦♥♠
            // 3(4)NT tests no pull
            yield return new object[] { "TestNoAsk18HCP", "AK32,AK2,A32,432", "x,xxxx,KQxxx,Axx", "1♣1NT2♦2♠3♣3NT", "1♠2♣2♥2NT3♦Pass" };
            yield return new object[] { "TestOneAsk21HCP", "AK32,AK2,A32,K32", "x,xxxx,KQxxx,Axx", "1♣1NT2♦2♠3♣3♥3NT", "1♠2♣2♥2NT3♦3♠Pass" };
            yield return new object[] { "TestTwoAsk23HCP", "AK32,AK2,A32,KQ2", "x,xxxx,Kxxxx,Axx", "1♣1NT2♦2♠3♣3♥4♣4NT", "1♠2♣2♥2NT3♦3♠4♥Pass" };
        }

        public static IEnumerable<object[]> TestCases3NTPull()
        {
            // ♣♦♥♠
            // 3(4)NT tests with pull
            yield return new object[] { "TestNoAskCtrl2", "AKQ2,AK2,A32,432", "x,QJxx,KQJxx,KQJ", "1♣1NT2♦2♠3♣3NT4♥4NT5♦5NTPass", "1♠2♣2♥2NT3♦4♦4♠5♣5♠6NT" };
            yield return new object[] { "TestNoAskCtrl3", "AKQ2,AK2,A32,432", "x,Qxxx,KQxxx,AQx", "1♣1NT2♦2♠3♣3NT4♦4♠5♦5NTPass", "1♠2♣2♥2NT3♦4♣4♥5♣5♠6NT" };
            yield return new object[] { "TestNoAskCtrl3_", "AKQ2,AK2,A32,432", "x,Qxxx,KQxxx,AQx", "1♣1NT2♦2♠3♣3NT4♦4♠5♦5NTPass", "1♠2♣2♥2NT3♦4♣4♥5♣5♠6NT" };
            yield return new object[] { "TestNoAskCtrl5", "KJx,KJxxx,Kx,AJx", "AQ,Qx,Axxxx,KQxx", "1♣3NT4♠5♥6♥6NTPass", "3♣4♥5♦6♦6♠7♠" };
            yield return new object[] { "TestOneAskCtrl3", "AKQ2,AK2,A32,K32", "x,Qxxx,KQxxx,AQx", "1♣1NT2♦2♠3♣3♥4♣4NT5♥Pass", "1♠2♣2♥2NT3♦3♠4♥5♦6♥" };
            yield return new object[] { "TestOneAskCtrl5ZoomScan", "AQJ2,AQJ,A32,K32", "K,Kxxx,KQxxx,Axx", "1♣1NT2♦2♠3♣3♥4NT6♣Pass", "1♠2♣2♥2NT3♦4♣5NT6♦" };
            yield return new object[] { "TestOneAskCtrl5", "AQJ2,AQJ,AK2,432", "K,Kxxx,Qxxxx,AKx", "1♣1NT2♦2♠3♣3♥4NT5♦5NT6♥Pass", "1♠2♣2♥2NT3♦4♣5♣5♠6♦6♠" };
            yield return new object[] { "TestTwoAskCtrl3", "AKQ2,AK2,A32,KQ2", "x,xxxx,KQxxx,Axx", "1♣1NT2♦2♠3♣3♥4♣4♠5♦6♣Pass", "1♠2♣2♥2NT3♦3♠4♥5♣5NT6♦" };
            yield return new object[] { "TestTwoAskCtrl5", "KQ32,AKQ,A32,KQ2", "A,xxxx,KQxxx,Axx", "1♣1NT2♦2♠3♣3♥4♦4NT5♠Pass", "1♠2♣2♥2NT3♦4♣4♠5♥5NT" };

            // TODO fix this
            yield return new object[] { "TestNoAskZoomShapeMin", "AKQ2,AK2,A32,432", "xxx,QJ,KQJx,KQJx", "1♣1NT2♠3NT4♥4NT5♠6♣6NTPass", "1♠2♥3♥4♦4♠5♥5NT6♠7♦" };
            yield return new object[] { "TestOneAskZoomShapeMin", "AKQ2,AK2,A32,K32", "xxx,QJ,KQJx,AQJx", "1♣1NT2♠3♠4♦5♣5♥6♦Pass", "1♠2♥3♥4♣4NT5♦6♣6♠" };
            yield return new object[] { "TestTwoAskZoomShapeMin", "AKQ2,AK2,A32,KQ2", "xxx,QJ,KQJx,AJxx", "1♣1NT2♠3♠4♦5♣5♥6♣6♥Pass", "1♠2♥3♥4♣4NT5♦5NT6♦6NT" };

            yield return new object[] { "TestNoAskZoomShapeMax", "AKQ2,AK2,A32,432", "xxx,QJ,KQJx,AKQJ", "1♣1NT2♠3NT4NT5♥5NT6♦7♣Pass", "1♠2♥3♠4♠5♦5♠6♣6NT7♥" };
            // TODO investigate why opener doesn't bid 3NT
            yield return new object[] { "TestOneAskZoomShapeMax", "AKQ2,AK2,A32,Q32", "xxx,QJ,KQJx,AKJx", "1♣1NT2♠4♣4♠5♣5♥6♣6♥Pass", "1♠2♥3♠4♥4NT5♦5NT6♦6NT" };
            yield return new object[] { "TestTwoAskZoomShapeMax", "AKQ2,AK2,AQ2,Q32", "xxx,QJ,KJxx,AKJx", "1♣1NT2♠4♣4♠5♣5♥5NT6♦6♠Pass", "1♠2♥3♠4♥4NT5♦5♠6♣6♥7♣" };
        }

        public static IEnumerable<object[]> TestCases4Diamond()
        {
            // ♣♦♥♠
            // Test with major fit no pull
            yield return new object[] { "Test17HCP", "AK32,A5432,A2,Q2", ",xxxx,KQxxxx,Kxx", "1♣1NT2♦2♠3♣4♦Pass", "1♠2♣2♥2NT3♠4♥" };
            yield return new object[] { "Test20HCPMin", "AQ32,A432,A2,AQ2", "xx,xxxx,KQxxx,Kx", "1♣1NT2♦2♠3♦4♦Pass", "1♠2♣2♥3♣3♥4♥" };
            yield return new object[] { "Test18HCPMax", "Q432,Q432,AJ,AKQ", "Kx,AJxx,Kxxxx,Jx", "1♣1NT2♦2♠3♦4♦Pass", "1♠2♣2♥3♣3♠4♥" };
        }

        public static IEnumerable<object[]> TestCases4DiamondPull()
        {
            // ♣♦♥♠
            // Test with major fit with pull
            yield return new object[] { "Test17HCP", "AKQ2,A432,A32,32", ",Kxxx,KQxxxx,AKx", "1♣1NT2♦2♠3♣4♦5♦6♣6♠Pass", "1♠2♣2♥2NT3♠5♣5NT6♥6NT" };
            yield return new object[] { "Test18HCPMax", "QJ32,KQJ2,AJ,A32", "Kx,Axxx,KQxxx,xx", "1♣1NT2♦2♠3♦4♦5♥6♣6♥Pass", "1♠2♣2♥3♣3♠5♦5NT6♦6♠" };
            yield return new object[] { "Test18HCPMin", "AQ32,A432,A2,A32", "xx,Kxxx,KQxxx,Kx", "1♣1NT2♦2♠3♦4♦5♣5NT6♠7♣Pass", "1♠2♣2♥3♣3♥4NT5♠6♥6NT7♦" };
            yield return new object[] { "Test20HCPMin", "AQ32,A432,K2,AK2", "xx,KQxx,AQxxx,xx", "1♣1NT2♦2♠3♦4♦5♣5NT6♦7♣Pass", "1♠2♣2♥3♣3♥4NT5♠6♣6NT7♦" };

            // Test with minor fit. TODO

            // Test with zoom
            yield return new object[] { "TestFitWithZoomMin", "K8743,A,A84,AQT4", "QJxx,KQ,Qxx,KJxx", "1♣1NT2♥3♠4♦4NT5♥6♦Pass", "1♠2♦3♥3NT4♠5♦6♣6NT" };
            yield return new object[] { "TestFitWithZoomMax", "KJ743,K,A84,AQT4", "Axxx,AQ,Qxx,Kxxx", "1♣1NT2♥4♦5♦5NT6♦Pass", "1♠2♦3NT5♣5♠6♣6NT" };

            // Special case, double zoom
            yield return new object[] { "TestFitWithDoubleZoom", "AJT3,KQ74,AQT5,5", "Kxx,Axxx,J,KQxxx", "1♣2♣2♠3♥4♦5♠Pass", "1NT2♥3♦3NT5♥5NT" };
        }
    }

    [Collection("Sequential")]
    public class TestRelayBid : IClassFixture<BaseTestFixture>
    {
        private readonly Dictionary<Fase, bool> fasesWithOffset;
        private readonly ReverseDictionaries reverseDictionaries;
        private readonly ITestOutputHelper output;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public TestRelayBid(BaseTestFixture fixture, ITestOutputHelper output)
        {
            fasesWithOffset = fixture.fasesWithOffset;
            reverseDictionaries = fixture.reverseDictionaries;
            this.output = output;
        }

        [Theory]
        [MemberData(nameof(TestCaseProviderRelayBid.TestCases3NT), MemberType = typeof(TestCaseProviderRelayBid))]
        public void TestAuctions3NT(string testName, string northHand, string southHand, string expectedBidsNorth, string expectedBidsSouth)
        {
            SetupTest.setupTest(testName, logger);
            var bidManager = new BidManager(new BidGeneratorDescription(), fasesWithOffset, reverseDictionaries, false);
            var auction = bidManager.GetAuction(northHand, southHand);
            AssertMethods.AssertAuction(expectedBidsNorth, expectedBidsSouth, auction);
        }

        [Theory]
        [MemberData(nameof(TestCaseProviderRelayBid.TestCases3NTPull), MemberType = typeof(TestCaseProviderRelayBid))]
        public void TestAuctions3NTPull(string testName, string northHand, string southHand, string expectedBidsNorth, string expectedBidsSouth)
        {
            SetupTest.setupTest(testName, logger);
            var bidManager = new BidManager(new BidGeneratorDescription(), fasesWithOffset, reverseDictionaries, false);
            var auction = bidManager.GetAuction(northHand, southHand);
            AssertMethods.AssertAuction(expectedBidsNorth, expectedBidsSouth, auction);
            AssertMethods.AssertHand(bidManager.biddingInformation, auction, northHand, southHand, reverseDictionaries);
        }

        [Theory]
        [MemberData(nameof(TestCaseProviderRelayBid.TestCases4Diamond), MemberType = typeof(TestCaseProviderRelayBid))]
        public void TestAuctions4Diamond(string testName, string northHand, string southHand, string expectedBidsNorth, string expectedBidsSouth)
        {
            SetupTest.setupTest(testName, logger);
            var bidManager = new BidManager(new BidGeneratorDescription(), fasesWithOffset, reverseDictionaries, (auction, northHand, sourceInformation) => { return BidManager.RelayBidKind.fourDiamondEndSignal; });
            var auction = bidManager.GetAuction(northHand, southHand);
            AssertMethods.AssertAuction(expectedBidsNorth, expectedBidsSouth, auction);
        }

        [Theory]
        [MemberData(nameof(TestCaseProviderRelayBid.TestCases4DiamondPull), MemberType = typeof(TestCaseProviderRelayBid))]
        public void TestAuctions4DiamondPull(string testName, string northHand, string southHand, string expectedBidsNorth, string expectedBidsSouth)
        {
            SetupTest.setupTest(testName, logger);
            var bidManager = new BidManager(new BidGeneratorDescription(), fasesWithOffset, reverseDictionaries, (auction, northHand, sourceInformation) => { return BidManager.RelayBidKind.fourDiamondEndSignal; });
            var auction = bidManager.GetAuction(northHand, southHand);
            AssertMethods.AssertAuction(expectedBidsNorth, expectedBidsSouth, auction);
            AssertMethods.AssertHand(bidManager.biddingInformation, auction, northHand, southHand, reverseDictionaries);
        }
    }
}
