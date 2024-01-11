#include "pch.h"
#include "Api.h"

#include <string>
#include <iostream>
#include <sstream>
#include "Rule.h"
#include <unordered_map>
#include "SQLiteCppWrapper.h"
#include <filesystem>

using std::filesystem::path;

HandCharacteristic GetHandCharacteristic(const std::string& hand)
{
    static HandCharacteristic handCharacteristic{};
    if (hand != handCharacteristic.hand)
    {
        handCharacteristic.Initialize(hand);
    }
    return handCharacteristic;
}

ISQLiteWrapper* GetSqliteWrapper()
{
    static std::unique_ptr<ISQLiteWrapper> sqliteWrapper = std::make_unique<SQLiteCppWrapper>("Tosr.db3");
    return sqliteWrapper.get();
}

int GetBidFromRuleInternal(Phase phase, Phase previousPhase, const char* hand, int lastBidId, Phase* newPhase, std::string& description, int& zoomOffset)
{
    auto handCharacteristic = GetHandCharacteristic(hand);

    auto [bidId, lNewphase, descr, lZoomOffset] = GetSqliteWrapper()->GetRule(handCharacteristic, phase, previousPhase, lastBidId);
    description = descr;
    *newPhase = lNewphase;
    zoomOffset = lZoomOffset;

    return bidId;
}

int GetBidFromRule(Phase phase, Phase previousPhase, const char* hand, int lastBidId, Phase* newPhase, int* zoomOffset)
{
    std::string dummy;
    int lZoomOffset;
    const auto bidId = GetBidFromRuleInternal(phase, previousPhase, hand, lastBidId, newPhase, dummy, lZoomOffset);
    *zoomOffset = lZoomOffset;
    return bidId;
}

int GetBidFromRuleEx(Phase phase, Phase previousPhase, const char* hand, int lastBidId, Phase* newPhase, char* description)
{
    std::string descr;
    int dummy;
    const auto bidId = GetBidFromRuleInternal(phase, previousPhase, hand, lastBidId, newPhase, descr, dummy);
    strncpy(description , descr.c_str(), descr.size());
    description[descr.size()] = '\0';
    return bidId;
}

int Setup(const char* database)
{
    if (!exists(path(database)))
        return -1;
    GetSqliteWrapper()->SetDatabase(database);
    return 0;
}

void GetBid(int bidId, int& rank, int& suit)
{
    GetSqliteWrapper()->GetBid(bidId, rank, suit);
}