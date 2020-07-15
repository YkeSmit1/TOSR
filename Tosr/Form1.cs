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
    enum Fase
    {
        Shape,
        Controls,
        Scanning
    };

    public partial class Form1 : Form
    {
        readonly BiddingBox biddingBox;
        private readonly AuctionControl auctionControl;
        private CardDto[] unOrderedCards;

        private readonly Dictionary<string, List<string>> handPerAuction = new Dictionary<string, List<string>>();
        private string[] hands;
        private int batchSize;
        private readonly ShuffleRestrictions shuffleRestrictions = new ShuffleRestrictions();

        public Form1()
        {
            InitializeComponent();
            void handler(object x, EventArgs y)
            {
                var biddingBoxButton = (BiddingBoxButton)x;
                auctionControl.AddBid(biddingBoxButton.bid);
                biddingBox.UpdateButtons(biddingBoxButton.bid, auctionControl.auction.currentPlayer);
            }
            biddingBox = new BiddingBox(handler)
            {
                Parent = this,
                Left = 50,
                Top = 200
            };
            biddingBox.Show();

            auctionControl = new AuctionControl
            {
                Parent = this,
                Left = 300,
                Top = 200,
                Width = 205,
                Height = 200
            };
            auctionControl.Show();
            // Need to set in code because of a .net core bug
            numericUpDown1.Maximum = 100_000;
            numericUpDown1.Value = 1000;
            Shuffle();
        }

        private void ButtonShuffleClick(object sender, EventArgs e)
        {
            Shuffle();
        }

        private void Shuffle()
        {
            bool HasCorrectControls(string s)
            {
                var controls = s.Count(x => x == 'A') * 2 + s.Count(x => x == 'K');
                return (!shuffleRestrictions.restrictControls && controls >= 1) || controls == shuffleRestrictions.controls;
            }

            bool HasCorrectDistribution(string s)
            {
                return !shuffleRestrictions.restrictShape || string.Join("", s.Split(',').Select(x => x.Length)) == shuffleRestrictions.shape;
            }

            foreach (var card in Controls.OfType<Card>())
            {
                card.Hide();
            }

            string handsString;
            do
            {
                unOrderedCards = Shuffling.RandomizeDeck(13);
                var orderedCards = unOrderedCards.OrderByDescending(x => x.Suit).ThenByDescending(c => c.Face, new Card.FaceComparer());
                handsString = GetDeckAsString(orderedCards);
            } while (!HasCorrectDistribution(handsString) || !HasCorrectControls(handsString));

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

        private void ClearAuction()
        {
            auctionControl.auction.Clear();
            auctionControl.ReDraw();
            biddingBox.Clear();
        }

        private void GetAuctionFromRules(string handsString, Auction auction)
        {
            auction.Clear();

            var fase = Fase.Shape;
            int lastBidId = 1;
            int bidId = int.MaxValue;
            var currentPlayer = Player.West;
            Bid currentBid = Bid.PassBid;
            int relayBidIdLastFase = 0;

            do
            {
                switch (currentPlayer)
                {
                    case Player.West:
                    case Player.East:
                        auction.AddBid(Bid.PassBid);
                        break;
                    case Player.North:
                        currentBid = Common.NextBid(currentBid);
                        auction.AddBid(currentBid);
                        lastBidId = Common.GetBidId(currentBid);
                        break;
                    case Player.South:
                        var bidFromRule = Pinvoke.GetBidFromRule(fase, handsString, lastBidId - relayBidIdLastFase, out var nextfase);
                        bidId = bidFromRule + relayBidIdLastFase;
                        if (bidFromRule == 0)
                        {
                            auction.AddBid(Bid.PassBid);
                            return;
                        }
                        if (nextfase != fase)
                        {
                            relayBidIdLastFase = bidId + 1;
                            fase = nextfase;
                        }

                        currentBid = Common.GetBid(bidId);
                        auction.AddBid(currentBid);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (currentPlayer == Player.South)
                    currentPlayer = Player.West;
                else currentPlayer++;
            }
            while (bidId != 0);
        }

        private string GetDeckAsString(IEnumerable<CardDto> orderedCards)
        {
            var listCards = orderedCards.ToList();
            var suitAsString = SuitAsString(listCards, Suit.Spades) + "," + 
                               SuitAsString(listCards, Suit.Hearts) + "," + 
                               SuitAsString(listCards, Suit.Diamonds) + "," + 
                               SuitAsString(listCards, Suit.Clubs);
            return suitAsString;
        }

        private static string SuitAsString(List<CardDto> listCards, Suit suit)
        {
            return listCards.Where(c => c.Suit == suit).Aggregate("", (x, y) => x + Common.GetFaceDescription(y.Face));
        }

        private void ButtonGetAuctionClick(object sender, EventArgs e)
        {
            ClearAuction();
            var orderedCards = unOrderedCards.OrderByDescending(x => x.Suit).ThenByDescending(c => c.Face, new Card.FaceComparer());
            var handsString = GetDeckAsString(orderedCards);
            GetAuctionFromRules(handsString, auctionControl.auction);
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
                    hands[i] = GetDeckAsString(orderedCards);
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
                    GetAuctionFromRules(hands[i], auction);
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

        private void ToolStripButton4_Click(object sender, EventArgs e)
        {
            ShuffleRestrictionsForm shuffleRestrictionsForm = new ShuffleRestrictionsForm(shuffleRestrictions);
            _ = shuffleRestrictionsForm.ShowDialog();
        }
    }
}
