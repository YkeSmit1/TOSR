using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tosr
{
    public class ShuffleRestrictions
    {
        public bool restrictControls = false;
        public int controls;
        public bool restrictShape = false;
        public string shape;

        private bool HasCorrectDistribution(string s)
        {
            return !restrictShape || string.Join("", s.Split(',').Select(x => x.Length)) == shape;
        }

        private bool HasCorrectControls(string s)
        {
            var controlsInHand = s.Count(x => x == 'A') * 2 + s.Count(x => x == 'K');
            return (!restrictControls && controlsInHand >= 2) || (restrictControls && controlsInHand == controls);
        }

        public bool Match(string hand)
        {
            return HasCorrectDistribution(hand) && HasCorrectControls(hand);
        }
    }
}
