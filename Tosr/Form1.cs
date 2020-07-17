using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Tosr
{
    public enum Fase
    {
        Shape,
        Controls,
        Scanning
    };

    public partial class Form1 : Form
    {
        private BiddingBox biddingBox;
        private AuctionControl auctionControl;
        private CardDto[] unOrderedCards;

        private readonly Dictionary<string, List<string>> handPerAuction = new Dictionary<string, List<string>>();
        private string[] hands;
        private int batchSize;
        private readonly ShuffleRestrictions shuffleRestrictions = new ShuffleRestrictions();
        private string handsString;
        readonly BiddingState biddingState = new BiddingState();

        public Form1()
        {
            InitializeComponent();
            ShowBiddingBox();
            ShowAuction();

            // Need to set in code because of a .net core bug
            numericUpDown1.Maximum = 100_000;
            numericUpDown1.Value = 1000;
            Shuffle();
            BidTillSouth(auctionControl.auction, biddingState);
        }

        private void ShowBiddingBox()
        {
            void handler(object x, EventArgs y)
            {
                var biddingBoxButton = (BiddingBoxButton)x;
                SouthBid(biddingState, handsString, true);
                if (biddingBoxButton.bid != biddingState.currentBid)
                {
                    MessageBox.Show($"Incorrect bid!. The correct bid is {biddingState.currentBid}. Description: {biddingState.currentBid.description}");
                }
                
                auctionControl.AddBid(biddingState.currentBid);
                BidTillSouth(auctionControl.auction, biddingState);
            }
            biddingBox = new BiddingBox(handler)
            {
                Parent = this,
                Left = 50,
                Top = 200
            };
            biddingBox.Show();
        }

        private void ShowAuction()
        {
            auctionControl = new AuctionControl
            {
                Parent = this,
                Left = 300,
                Top = 200,
                Width = 205,
                Height = 200
            };
            auctionControl.Show();
        }

        private void BidTillSouth(Auction auction, BiddingState biddingState)
        {
            // West
            auction.AddBid(Bid.PassBid);
            // North
            NorthBid(biddingState);
            auction.AddBid(biddingState.currentBid);
            // East
            auction.AddBid(Bid.PassBid);

            auctionControl.ReDraw();
            biddingBox.UpdateButtons(biddingState.currentBid, auctionControl.auction.currentPlayer);
        }

        private static void NorthBid(BiddingState biddingState)
        {
            biddingState.currentBid = Common.NextBid(biddingState.currentBid);
            biddingState.lastBidId = Common.GetBidId(biddingState.currentBid);
        }

        private static void SouthBid(BiddingState biddingState, string handsString, bool requestDescription)
        {
            var description = new StringBuilder(128);

            var bidFromRule = requestDescription ?
                    Pinvoke.GetBidFromRuleEx(biddingState.fase, handsString, biddingState.lastBidId - biddingState.relayBidIdLastFase, out Fase nextfase, description) :
                    Pinvoke.GetBidFromRule(biddingState.fase, handsString, biddingState.lastBidId - biddingState.relayBidIdLastFase, out nextfase);
            biddingState.bidId = bidFromRule + biddingState.relayBidIdLastFase;
            if (bidFromRule == 0)
            {
                biddingState.bidId = 0;
                return;
            }
            if (nextfase != biddingState.fase)
            {
                biddingState.relayBidIdLastFase = biddingState.bidId + 1;
                biddingState.fase = nextfase;
            }

            var currentBid = Common.GetBid(biddingState.bidId);
            currentBid.description = requestDescription ? description.ToString() : string.Empty;
            biddingState.currentBid = currentBid;
        }

        private void ButtonShuffleClick(object sender, EventArgs e)
        {
            Shuffle();
            biddingState.Init();
            Clear();
            BidTillSouth(auctionControl.auction, biddingState);
        }

        private void Shuffle()
        {
            foreach (var card in Controls.OfType<Card>())
            {
                card.Hide();
            }

            do
            {
                unOrderedCards = Shuffling.RandomizeDeck(13);
                var orderedCards = unOrderedCards.OrderByDescending(x => x.Suit).ThenByDescending(c => c.Face, new Card.FaceComparer());
                handsString = Common.GetDeckAsString(orderedCards);
            } while (!shuffleRestrictions.Match(handsString));

            var left = 20 * 13;
            var cardDtos = unOrderedCards.OrderBy(x => x.Suit, new Card.GuiSuitComparer()).ThenBy(c => c.Face, new Card.FaceComparer()).ToArray();
            foreach (var cardDto in cardDtos) 
            {
                var card = new Card(cardDto.Face, cardDto.Suit, Back.Crosshatch, false, false)
                {
                    Left = left, Top = 80, Parent = this
                };
                card.Show();
                left -= 20;
            }
        }

        private void Clear()
        {
            auctionControl.auction.Clear();
            auctionControl.ReDraw();
            biddingBox.Clear();
        }

        private static void GetAuctionFromRules(string handsString, Auction auction, bool requestDescription)
        {
            auction.Clear();

            BiddingState biddingState = new BiddingState();
            Player currentPlayer = Player.West;

            do
            {
                switch (currentPlayer)
                {
                    case Player.West:
                    case Player.East:
                        auction.AddBid(Bid.PassBid);
                        break;
                    case Player.North:
                        NorthBid(biddingState);
                        auction.AddBid(biddingState.currentBid);
                        break;
                    case Player.South:
                        SouthBid(biddingState, handsString, requestDescription);
                        if (biddingState.bidId == 0)
                            auction.AddBid(Bid.PassBid);
                        else
                            auction.AddBid(biddingState.currentBid);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (currentPlayer == Player.South)
                    currentPlayer = Player.West;
                else currentPlayer++;
            }
            while (biddingState.bidId != 0);
        }

        private void ButtonGetAuctionClick(object sender, EventArgs e)
        {
            Clear();
            var orderedCards = unOrderedCards.OrderByDescending(x => x.Suit).ThenByDescending(c => c.Face, new Card.FaceComparer());
            var handsString = Common.GetDeckAsString(orderedCards);
            GetAuctionFromRules(handsString, auctionControl.auction, true);
            auctionControl.ReDraw();
        }

        private void ButtonBatchBiddingClick(object sender, EventArgs e)
        {
            var oldCursor = Cursor.Current;
            try
            {
                Cursor.Current = Cursors.WaitCursor;

                handPerAuction.Clear();

                BatchBidding();
                SaveAuctions();
            }
            finally
            {
                Cursor.Current = oldCursor;
            }
        }

        private void SaveAuctions()
        {
            var multiHandPerAuction = handPerAuction.Where(x => x.Value.Count > 1).ToDictionary(x => x.Key, x => x.Value);
            File.WriteAllText("HandPerAuction.txt", JsonConvert.SerializeObject(multiHandPerAuction));
        }

        private bool IsFreakHand(string handLength)
        {
            var handPattern = new string(handLength.OrderByDescending(y => y).ToArray());
            return handPattern == "7321" || handPattern[0] == '8' || handPattern[0] == '9' || 
                   (handPattern[0] == '7' && handPattern[1] == '5') ||
                    (handPattern[0] == '6' && handPattern[1] == '6') ||
                    (handPattern[0] == '7' && handPattern[1] == '6');
        }

        private string[] GenerateHandStrings()
        {
            hands = new string[batchSize];

            for (int i = 0; i < batchSize; ++i)
            {
                do
                {
                    unOrderedCards = Shuffling.RandomizeDeck(13);
                    var orderedCards = unOrderedCards.OrderByDescending(x => x.Suit)
                        .ThenByDescending(c => c.Face, new Card.FaceComparer());
                    hands[i] = Common.GetDeckAsString(orderedCards);
                } while (hands[i].Count(x => x == 'A') * 2 + hands[i].Count(x => x == 'K') < 2);
            }

            return hands;
        }

        private void BatchBidding()
        {
            var stopwatch = Stopwatch.StartNew();
            var stringbuilder = new StringBuilder();

            for (int i = 0; i < batchSize; ++i)
            {
                try
                {
                    Auction auction = new Auction();
                    GetAuctionFromRules(hands[i], auction, false);
                    var bids = auction.GetBids(Player.South);
                    AddHandAndAuction(hands[i], bids[0..^4]);
                }

                catch (Exception exception)
                {
                    stringbuilder.AppendLine(exception.Message);
                }

            }
            stringbuilder.AppendLine($"Seconds elapsed: {stopwatch.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture)}");
            _ = MessageBox.Show(stringbuilder.ToString());
        }

        private void AddHandAndAuction(string strHand, string strAuction)
        {
            var str = string.Join("", strHand.Split(',').Select(x => x.Length));

            if (IsFreakHand(str))
                return;

            if (!handPerAuction.ContainsKey(strAuction))
                handPerAuction[strAuction] = new List<string>();
            if (!handPerAuction[strAuction].Contains(str))
                handPerAuction[strAuction].Add(str);
        }

        private void ButtonGenerateHandsClick(object sender, EventArgs e)
        {
            batchSize = (int) numericUpDown1.Value;
            var oldCursor = Cursor.Current;
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                hands = GenerateHandStrings();
            }
            finally
            {
                Cursor.Current = oldCursor;
            }
        }

        private void ToolStripButton4Click(object sender, EventArgs e)
        {
            ShuffleRestrictionsForm shuffleRestrictionsForm = new ShuffleRestrictionsForm(shuffleRestrictions);
            _ = shuffleRestrictionsForm.ShowDialog();
        }
    }
}
