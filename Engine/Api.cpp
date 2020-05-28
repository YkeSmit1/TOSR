#include "pch.h"
#include "Api.h"

#include <string>
#include <iostream>
#include <sstream>
#include "SQLiteOrmWrapper.h"
#include "Rule.h"
#include <unordered_map>
#include "SQLiteCppWrapper.h"
#include <algorithm>

HandCharacteristic GetHandCharacteristic(const std::string& hand)
{
    static HandCharacteristic handCharacteristic{};
    if (hand != handCharacteristic.hand)
    {
        handCharacteristic.Initialize(hand);
    }
    return handCharacteristic;
}

int GetBidFromRule(int faseId, const char* hand, int lastBidId)
{
    std::cout << "faseId:" << faseId << " hand:" << hand << '\n';

    std::unique_ptr<ISQLiteWrapper> sqliteWrapper = std::make_unique<SQLiteCppWrapper>();
    auto handCharacteristic = GetHandCharacteristic(hand);
    auto rules = sqliteWrapper->GetRules(handCharacteristic, faseId, lastBidId);
    if (rules.empty())
        return 0;

    std::sort(rules.begin(), rules.end(), [](auto tuple1, auto tuple2) {return std::get<0>(tuple1) < std::get<0>(tuple2);});

    int bidId;
    int nextFaseId;
    std::tie(bidId, nextFaseId) = rules.front();
    return bidId;
}

void GetBid(int bidId, int& rank, int& suit)
{
    std::unique_ptr<ISQLiteWrapper> sqliteWrapper = std::make_unique<SQLiteCppWrapper>();
    sqliteWrapper->GetBid(bidId, rank, suit);
}