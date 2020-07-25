#pragma once

#include "ISQLiteWrapper.h"
#include "SQLiteCpp/SQLiteCpp.h"

class SQLiteCppWrapper : public ISQLiteWrapper
{
    constexpr static std::string_view shapeSql = R"(SELECT bidId, EndFase, Zoom, Id, IFNULL(Description, Distribution) FROM Rules 
        WHERE (bidId > ?)
        AND MinSpades <= ?
        AND MaxSpades >= ?
        AND MinHearts <= ?
        AND MaxHearts >= ?
        AND MinDiamonds <= ?
        AND MaxDiamonds >= ?
        AND MinClubs <= ?
        AND MaxClubs >= ?
        AND (Distribution IS NULL or Distribution = ?)
        AND (IsBalanced IS NULL or IsBalanced = ?)
        AND (IsReverse IS NULL or IsReverse = ?)
        AND (Shortage IS NULL or Shortage = ?)
        AND (IsThreeSuiter IS NULL or IsThreeSuiter = ?)
        AND (Is65Reverse IS NULL or Is65Reverse = ?)
        ORDER BY bidId ASC)";

    constexpr static std::string_view controlsSql = R"(SELECT RelBidId, EndFase, Id, Description FROM Controls 
        WHERE RelbidId > ?
        AND MinControls <= ?
        AND MaxControls >= ?
        AND MinHcp <= ?
        AND MaxHcp >= ?
        ORDER BY RelBidId ASC)";

    constexpr static std::string_view scanningSql = R"(SELECT RelBidId, Id, Description FROM Scanning 
        WHERE RelbidId > ?
        AND (Controls1Suit = ? or Controls1Suit is null)
        AND (Controls2Suit = ? or Controls2Suit is null)
        AND (Controls3Suit = ? or Controls3Suit is null)
        AND (Controls4Suit = ? or Controls4Suit is null)
        AND (Queen1Suit = ? or Queen1Suit is null)
        AND (Queen2Suit = ? or Queen2Suit is null)
        AND (Queen3Suit = ? or Queen3Suit is null)
        AND (Queen4Suit = ? or Queen4Suit is null)
        ORDER BY RelBidId ASC)";

    std::unique_ptr<SQLite::Database> db;
    std::unique_ptr<SQLite::Statement> queryShape;
    std::unique_ptr<SQLite::Statement> queryControls;
    std::unique_ptr<SQLite::Statement> queryScanning;

public:
    SQLiteCppWrapper(const std::string& database);
private:
    void GetBid(int bidId, int& rank, int& suit) final override;
    std::tuple<int, bool, std::string> GetRule(const HandCharacteristic& hand, const Fase& fase, int lastBidId) final override;
    std::tuple<int, bool, std::string> GetRuleShape(const HandCharacteristic& hand, int lastBidId);
    std::tuple<int, bool, std::string> GetRuleControls(const HandCharacteristic& hand, int lastBidId);
    std::tuple<int, bool, std::string> GetRuleScanning(const HandCharacteristic& hand, int lastBidId);
    void SetDatabase(const std::string& database) override;
};

