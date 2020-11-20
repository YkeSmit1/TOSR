#pragma once

#include "ISQLiteWrapper.h"
#include "SQLiteCpp/SQLiteCpp.h"

class SQLiteCppWrapper : public ISQLiteWrapper
{
    constexpr static std::string_view shapeSql = R"(SELECT bidId, EndFase, Zoom, Id, IFNULL(Description, Distribution) FROM Rules 
        WHERE (bidId > ?)
        AND ? BETWEEN MinSpades AND MaxSpades
        AND ? BETWEEN MinHearts AND MaxHearts
        AND ? BETWEEN MinDiamonds AND MaxDiamonds
        AND ? BETWEEN MinClubs AND MaxClubs
        AND (Distribution IS NULL or Distribution = ?)
        AND (IsBalanced IS NULL or IsBalanced = ?)
        AND (IsReverse IS NULL or IsReverse = ?)
        AND (Shortage IS NULL or Shortage = ?)
        AND (IsThreeSuiter IS NULL or IsThreeSuiter = ?)
        AND (Is65Reverse IS NULL or Is65Reverse = ?)
        ORDER BY bidId ASC)";

    constexpr static std::string_view controlsSql = R"(SELECT RelBidId, EndFase, Id, Description FROM Controls 
        WHERE RelbidId > ?
        AND ? BETWEEN MinControls AND MaxControls
        AND ? BETWEEN MinHcp AND MaxHcp
        ORDER BY RelBidId ASC)";

    constexpr static std::string_view scanningControlsSql = R"(SELECT RelBidId, EndFase, Zoom, Id, Description FROM ScanningControls
        WHERE RelbidId > ?
        AND (Controls1Suit = ? or Controls1Suit is null)
        AND (Controls2Suit = ? or Controls2Suit is null)
        AND (Controls3Suit = ? or Controls3Suit is null)
        AND (Controls4Suit = ? or Controls4Suit is null)
        AND ((? BETWEEN MinShortages AND MaxShortages) OR (MinShortages IS NULL AND MaxShortages IS null))
        ORDER BY RelBidId ASC)";

    constexpr static std::string_view scanningOtherSql = R"(SELECT RelBidId, EndFase, Id, Description FROM ScanningOther 
        WHERE RelbidId > ?
        AND (Queen1Suit = ? or Queen1Suit is null)
        AND (Queen2Suit = ? or Queen2Suit is null)
        AND (Queen3Suit = ? or Queen3Suit is null)
        AND (Queen4Suit = ? or Queen4Suit is null)
        AND ((? BETWEEN MinShortages AND MaxShortages) OR (MinShortages IS NULL AND MaxShortages IS null))
        ORDER BY RelBidId ASC)";


    constexpr static std::string_view signOffsSql = R"(SELECT RelBidId, Zoom, Id, Description FROM SignOffs
        WHERE Fase = ?
        AND (Max = ? or Max is null)
        AND ((? BETWEEN MinHcp AND MaxHcp) OR (MinHcp is null AND MaxHcp is null))
        AND ((? BETWEEN MinQueens AND MaxQueens) OR (MinQueens is null AND MaxQueens is null)))";

    std::unique_ptr<SQLite::Database> db;
    std::unique_ptr<SQLite::Statement> queryShape;
    std::unique_ptr<SQLite::Statement> queryControls;
    std::unique_ptr<SQLite::Statement> queryScanningControls;
    std::unique_ptr<SQLite::Statement> queryScanningOther;
    std::unique_ptr<SQLite::Statement> querySignOffs;

public:
    SQLiteCppWrapper(const std::string& database);
private:
    void GetBid(int bidId, int& rank, int& suit) final;
    std::tuple<int, Fase, std::string, int> GetRule(const HandCharacteristic& hand, const Fase& fase, Fase previousFase, int lastBidId) final;
    Fase GetNextFase(bool endfase, Fase fase) noexcept;
    Fase NextFase(Fase previousFase) noexcept;
    std::tuple<int, bool, std::string, bool> GetRuleShape(const HandCharacteristic& hand, int lastBidId);
    std::tuple<int, bool, std::string> GetRuleControls(const HandCharacteristic& hand, int lastBidId);
    std::tuple<int, bool, std::string, bool> GetRuleScanningControls(const HandCharacteristic& hand, int lastBidId);
    std::tuple<int, bool, std::string> GetRuleScanningOther(const HandCharacteristic& hand, int lastBidId);
    std::tuple<int, bool, std::string> GetRuleSignOff(const HandCharacteristic& hand, Fase fase);
    void SetDatabase(const std::string& database) override;
};

