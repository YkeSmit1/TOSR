#pragma once

#include "ISQLiteWrapper.h"
#include "Rule.h"

class SQLiteCppWrapper : public ISQLiteWrapper
{
    void GetBid(int bidId, int& rank, int& suit) override;
    static std::string ConvertShortage(Shortage shortage);
    std::vector<std::tuple<int, int>> GetRules(const HandCharacteristic& hand, int faseId, int lastBidId) override;
};

