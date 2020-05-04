#pragma once

#include <vector>
#include <string>

struct HandCharacteristic;

class ISQLiteWrapper
{
public:
    virtual ~ISQLiteWrapper() = default;
    virtual std::vector<std::tuple<int, int>> GetRules(const HandCharacteristic& handCharacteristic, int faseId, int lastBidId) = 0;
    virtual void GetBid(int bidId, int& rank, int& suit) = 0;
};

