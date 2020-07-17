#include "pch.h"
#include "Api.h"

#include <string>
#include <iostream>
#include <sstream>
#include "Rule.h"
#include <unordered_map>
#include "SQLiteCppWrapper.h"

HandCharacteristic GetHandCharacteristic(const std::string& hand)
{
    static HandCharacteristic handCharacteristic{};
    if (hand != handCharacteristic.hand)
    {
        handCharacteristic.Initialize(hand);
    }
    return handCharacteristic;
}

int GetBidFromRuleInternal(Fase fase, const char* hand, int lastBidId, Fase* newFase, std::string& description)
{
    std::cout << "Fase:" << (int)fase << " hand:" << hand << '\n';

    std::unique_ptr<ISQLiteWrapper> sqliteWrapper = std::make_unique<SQLiteCppWrapper>();
    auto handCharacteristic = GetHandCharacteristic(hand);

    auto [bidId, endFase, descr] = sqliteWrapper->GetRule(handCharacteristic, fase, lastBidId);
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

int GetBidFromRuleEx(Fase fase, const char* hand, int lastBidId, Fase* newFase, LPSTR description)
{
    std::string descr;
    auto bidId = GetBidFromRuleInternal(fase, hand, lastBidId, newFase, descr);
    strncpy(description , descr.c_str(), descr.size());
    description[descr.size()] = '\0';
    return bidId;
}


void GetBid(int bidId, int& rank, int& suit)
{
    std::unique_ptr<ISQLiteWrapper> sqliteWrapper = std::make_unique<SQLiteCppWrapper>();
    sqliteWrapper->GetBid(bidId, rank, suit);
}