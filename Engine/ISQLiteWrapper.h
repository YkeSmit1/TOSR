#pragma once

#include <tuple>
#include <string>

struct HandCharacteristic;
enum class Fase;

class ISQLiteWrapper  // NOLINT(hicpp-special-member-functions, cppcoreguidelines-special-member-functions)
{
public:
    virtual ~ISQLiteWrapper() = default;
    virtual std::tuple<int, bool, std::string> GetRule(const HandCharacteristic& handCharacteristic, const Fase& fase, int lastBidId) = 0;
    virtual void GetBid(int bidId, int& rank, int& suit) = 0;
};

