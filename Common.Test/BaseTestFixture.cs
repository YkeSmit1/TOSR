using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tosr;

namespace Common.Test
{
    public class BaseTestFixture
    {
        public readonly Dictionary<Fase, bool> fasesWithOffset;
        public readonly ReverseDictionaries reverseDictionaries;
        public BaseTestFixture()
        {
            fasesWithOffset = JsonConvert.DeserializeObject<Dictionary<Fase, bool>>(File.ReadAllText("FasesWithOffset.json"));
            reverseDictionaries = new ReverseDictionaries(fasesWithOffset, new Progress<string>());

            BidManager.SetSystemParameters(Util.ReadResource("Tosr.SystemParameters.json"));
            BidManager.SetOptimizationParameters(Util.ReadResource("Tosr.OptimizationParameters.json"));
        }
    }
}
