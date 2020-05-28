#pragma once

#include "ISQLiteWrapper.h"
#include "Rule.h"
#include "SQLiteCpp/SQLiteCpp.h"


class SQLiteCppWrapper : public ISQLiteWrapper
{
    static SQLite::Database db;
    static SQLite::Statement query;

    void GetBid(int bidId, int& rank, int& suit) override;
    static std::string ConvertShortage(Shortage shortage);
    std::vector<std::tuple<int, int>> GetRules(const HandCharacteristic& hand, int faseId, int lastBidId) override;
};

