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

int GetBidFromRuleInternal(Fase fase, const char* hand, int lastBidId, Fase* newFase, std::string& description)
{
    std::cout << "Fase:" << (int)fase << " hand:" << hand << '\n';

    auto handCharacteristic = GetHandCharacteristic(hand);

    auto [bidId, endFase, descr] = GetSqliteWrapper()->GetRule(handCharacteristic, fase, lastBidId);
    description = descr;
    // Check if the fase has ended
    if (endFase)
        *newFase = (Fase)((int)fase + 1);
    else
        *newFase = fase;

    return bidId;
}

int GetBidFromRule(Fase fase, const char* hand, int lastBidId, Fase* newFase)
{
    std::string dummy;
    auto bidId = GetBidFromRuleInternal(fase, hand, lastBidId, newFase, dummy);
    return bidId;
}

int GetBidFromRuleEx(Fase fase, const char* hand, int lastBidId, Fase* newFase, char* description)
{
    std::string descr;
    auto bidId = GetBidFromRuleInternal(fase, hand, lastBidId, newFase, descr);
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