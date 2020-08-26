using System;
using System.Collections.Generic;
using System.Text;

namespace Tosr
{
    public interface IPinvoke
    {
        int GetBidFromRuleEx(Fase fase, string hand, int lastBidId, out Fase newFase, StringBuilder description);
        int GetBidFromRule(Fase fase, string hand, int lastBidId, out Fase newFase);
        int Setup(string database);
    }
}
