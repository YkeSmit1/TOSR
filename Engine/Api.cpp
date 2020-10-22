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

int GetBidFromRuleInternal(Fase fase, Fase previousFase, const char* hand, int lastBidId, Fase* newFase, std::string& description, int& zoomOffset)
{
    auto handCharacteristic = GetHandCharacteristic(hand);

    auto [bidId, lNewfase, descr, lZoomOffset] = GetSqliteWrapper()->GetRule(handCharacteristic, fase, previousFase, lastBidId);
    description = descr;
    *newFase = lNewfase;
    zoomOffset = lZoomOffset;

    return bidId;
}

int GetBidFromRule(Fase fase, Fase previousFase, const char* hand, int lastBidId, Fase* newFase, int* zoomOffset)
{
    std::string dummy;
    int lZoomOffset;
    auto bidId = GetBidFromRuleInternal(fase, previousFase, hand, lastBidId, newFase, dummy, lZoomOffset);
    *zoomOffset = lZoomOffset;
    return bidId;
}

int GetBidFromRuleEx(Fase fase, Fase previousFase, const char* hand, int lastBidId, Fase* newFase, char* description)
{
    std::string descr;
    int dummy;
    auto bidId = GetBidFromRuleInternal(fase, previousFase, hand, lastBidId, newFase, descr, dummy);
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