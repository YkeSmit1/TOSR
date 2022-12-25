using Common;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Wpf.BidControls.Converters
{
    public class CardToImageRectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var card = (Card)value;
            Debug.Assert(card != null, nameof(card) + " != null");
            var settings = card.CardImageSettings;
            var suit = card.Suit;
            var face = settings.FirstCardIsAce ? (int)card.Face : card.Face == Face.Ace ? 12 : (int)card.Face - 1;
            var topY = suit switch
            {
                Suit.Clubs => settings.TopClubs,
                Suit.Diamonds => settings.TopDiamonds,
                Suit.Hearts => settings.TopHearts,
                Suit.Spades => settings.TopSpades,
                _ => throw new ArgumentException(nameof(suit)),
            };
            var topX = settings.XOffSet + (settings.CardWidth * face);
            return new Int32Rect(topX, topY, settings.CardWidth - settings.XCardPadding, settings.CardHeight);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
