using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Tosr
{
    public class BatchBidding
    {
        readonly IBidGenerator bidGenerator;

        public BatchBidding(IBidGenerator bidGenerator)
        {
            this.bidGenerator = bidGenerator;
        }

        private readonly Dictionary<string, List<string>> handPerAuction = new Dictionary<string, List<string>>();
        public void Execute(string[] hands)
        {
            handPerAuction.Clear();

            var stopwatch = Stopwatch.StartNew();
            var stringbuilder = new StringBuilder();

            for (int i = 0; i < hands.Length; ++i)
            {
                try
                {
                    Auction auction = new Auction();
                    BidManager.GetAuction(hands[i], auction, false, bidGenerator);
                    var bids = auction.GetBids(Player.South);
                    AddHandAndAuction(hands[i], bids[0..^4]);
                }
                catch (Exception exception)
                {
                    stringbuilder.AppendLine(exception.Message);
                }
            }
            stringbuilder.AppendLine($"Seconds elapsed: {stopwatch.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture)}");
            SaveAuctions();

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

        private static bool IsFreakHand(string handLength)
        {
            var handPattern = new string(handLength.OrderByDescending(y => y).ToArray());
            return handPattern == "7321" || handPattern[0] == '8' || handPattern[0] == '9' ||
                   (handPattern[0] == '7' && handPattern[1] == '5') ||
                    (handPattern[0] == '6' && handPattern[1] == '6') ||
                    (handPattern[0] == '7' && handPattern[1] == '6');
        }

        private void SaveAuctions()
        {
            var multiHandPerAuction = handPerAuction.Where(x => x.Value.Count > 1).ToDictionary(x => x.Key, x => x.Value);
            File.WriteAllText("HandPerAuction.txt", JsonConvert.SerializeObject(multiHandPerAuction));
        }
    }
}
