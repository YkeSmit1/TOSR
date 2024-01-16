#pragma once

#define SQLITECPP_COMPILE_DLL

#include "ISQLiteWrapper.h"
#include "SQLiteCpp/SQLiteCpp.h"


class SQLiteCppWrapper : public ISQLiteWrapper
{
    constexpr static std::string_view shapeSql = R"(SELECT bidId, EndPhase, Zoom, Id, IFNULL(Description, Distribution) FROM Rules 
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

    constexpr static std::string_view controlsSql = R"(SELECT RelBidId, EndPhase, Id, Description FROM Controls 
        WHERE RelBidId > ?
        AND ? BETWEEN MinControls AND MaxControls
        AND ? BETWEEN MinHcp AND MaxHcp
        ORDER BY RelBidId ASC)";

    constexpr static std::string_view scanningControlsSql = R"(SELECT RelBidId, EndPhase, Zoom, Id, Description FROM ScanningControls
        WHERE RelBidId > ?
        AND (Controls1Suit = ? or Controls1Suit is null)
        AND (Controls2Suit = ? or Controls2Suit is null)
        AND (Controls3Suit = ? or Controls3Suit is null)
        AND (Controls4Suit = ? or Controls4Suit is null)
        AND ((? BETWEEN MinShortages AND MaxShortages) OR (MinShortages IS NULL AND MaxShortages IS null))
        ORDER BY RelBidId ASC)";

    constexpr static std::string_view scanningOtherSql = R"(SELECT RelBidId, EndPhase, Id, Description FROM ScanningOther 
        WHERE RelBidId > ?
        AND (Queen1Suit = ? or Queen1Suit is null)
        AND (Queen2Suit = ? or Queen2Suit is null)
        AND (Queen3Suit = ? or Queen3Suit is null)
        AND (Queen4Suit = ? or Queen4Suit is null)
        AND ((? BETWEEN MinShortages AND MaxShortages) OR (MinShortages IS NULL AND MaxShortages IS null))
        ORDER BY RelBidId ASC)";


    constexpr static std::string_view signOffsSql = R"(SELECT RelBidId, Zoom, Id, Description FROM SignOffs
        WHERE Phase = ?
        AND ((? BETWEEN MinHcp AND MaxHcp) OR (MinHcp is null AND MaxHcp is null))
        AND ((? BETWEEN MinQueens AND MaxQueens) OR (MinQueens is null AND MaxQueens is null)))";

    std::unique_ptr<SQLite::Database> db;
    std::unique_ptr<SQLite::Statement> queryShape;
    std::unique_ptr<SQLite::Statement> queryControls;
    std::unique_ptr<SQLite::Statement> queryScanningControls;
    std::unique_ptr<SQLite::Statement> queryScanningOther;
    std::unique_ptr<SQLite::Statement> querySignOffs;

public:
    explicit SQLiteCppWrapper(const std::string& database);
private:
    void GetBid(int bidId, int& rank, int& suit) final;
    std::tuple<int, Phase, std::string, int> GetRule(const HandCharacteristic& hand, const Phase& phase, Phase previousPhase, int lastBidId) final;
    static Phase GetNextPhase(bool endPhase, Phase phase) noexcept;
    static Phase NextPhase(Phase previousPhase) noexcept;
    [[nodiscard]] std::tuple<int, bool, std::string, bool> GetRuleShape(const HandCharacteristic& hand, int lastBidId) const;
    [[nodiscard]] std::tuple<int, bool, std::string> GetRuleControls(const HandCharacteristic& hand, int lastBidId) const;
    [[nodiscard]] std::tuple<int, bool, std::string, bool> GetRuleScanningControls(const HandCharacteristic& hand, int lastBidId) const;
    [[nodiscard]] std::tuple<int, bool, std::string> GetRuleScanningOther(const HandCharacteristic& hand, int lastBidId) const;
    [[nodiscard]] std::tuple<int, bool, std::string> GetRuleSignOff(const HandCharacteristic& hand, Phase phase) const;
    void SetDatabase(const std::string& database) override;
};

