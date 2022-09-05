using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Common;
using MvvmHelpers;

namespace Wpf.BidControls.ViewModels
{
    public class HandViewModel : ObservableObject
    {
        public ObservableCollection<Card> Cards { get; } = new();
        public CardImageSettings CardImageSettings { get; set; }
        public HandViewModel()
        {
            ShowHand("AQJ4,K32,843,QT9", true, "default");
        }

        public void ShowHand(string hand, bool alternateSuits, string cardProfile)
        {
            var cardImageSettings = CardImageSettings.GetCardImageSettings(cardProfile);

            CardImageSettings = cardImageSettings;
            Cards.Clear();
            var suitOrder = alternateSuits ?
                new List<Suit> { Suit.Spades, Suit.Hearts, Suit.Clubs, Suit.Diamonds } :
                new List<Suit> { Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs };
            var suits = hand.Split(',').Select((x, index) => (x, (Suit)(3 - index))).OrderBy(x => suitOrder.IndexOf(x.Item2));
            var index = 0;

            foreach (var suit in suits)
            {
                foreach (var card in suit.x)
                {
                    Cards.Add(new Card
                    {
                        Suit = suit.Item2,
                        Face = Util.GetFaceFromDescription(card),
                        Index = index * cardImageSettings.CardDistance,
                        CardImageSettings = cardImageSettings
                    });
                    index++;
                }
            }
        }
    }
}
