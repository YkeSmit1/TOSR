#pragma once

#include "ISQLiteWrapper.h"
#include "SQLiteCpp/SQLiteCpp.h"


class SQLiteCppWrapper : public ISQLiteWrapper
{
    static SQLite::Database db;
    static SQLite::Statement queryShape;
    static SQLite::Statement queryControls;
    static SQLite::Statement queryScanning;

    void GetBid(int bidId, int& rank, int& suit) final override;
    std::tuple<int, bool> GetRule(const HandCharacteristic& hand, const Fase& fase, int lastBidId) final override;
    static std::tuple<int, bool> GetRuleShape(const HandCharacteristic& hand, int lastBidId);
    static std::tuple<int, bool> GetRuleControls(const HandCharacteristic& hand, int lastBidId);
    static std::tuple<int, bool> GetRuleScanning(const HandCharacteristic& hand, int lastBidId);
};

