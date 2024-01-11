using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using BiddingLogic;
using Common.Tosr;

namespace Common.Test
{
    public abstract class BaseTestFixture
    {
        public readonly Dictionary<Phase, bool> phasesWithOffset;
        public readonly ReverseDictionaries reverseDictionaries;

        protected BaseTestFixture()
        {
            phasesWithOffset = JsonConvert.DeserializeObject<Dictionary<Phase, bool>>(File.ReadAllText("FasesWithOffset.json"));
            reverseDictionaries = new ReverseDictionaries(phasesWithOffset, new Progress<string>());

            BidManager.SetSystemParameters(UtilTosr.ReadResource("BiddingLogic.SystemParameters.json"));
            BidManager.SetOptimizationParameters(UtilTosr.ReadResource("BiddingLogic.OptimizationParameters.json"));
        }
    }
}
