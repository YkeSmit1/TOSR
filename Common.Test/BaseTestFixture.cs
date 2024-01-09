using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using BiddingLogic;
using Common.Tosr;

namespace Common.Test
{
    public class BaseTestFixture
    {
        public readonly Dictionary<Phase, bool> fasesWithOffset;
        public readonly ReverseDictionaries reverseDictionaries;
        public BaseTestFixture()
        {
            fasesWithOffset = JsonConvert.DeserializeObject<Dictionary<Phase, bool>>(File.ReadAllText("FasesWithOffset.json"));
            reverseDictionaries = new ReverseDictionaries(fasesWithOffset, new Progress<string>());

            BidManager.SetSystemParameters(UtilTosr.ReadResource("BiddingLogic.SystemParameters.json"));
            BidManager.SetOptimizationParameters(UtilTosr.ReadResource("BiddingLogic.OptimizationParameters.json"));
        }
    }
}
