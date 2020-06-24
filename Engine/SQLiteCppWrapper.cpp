#include "pch.h"
#include "SQLiteCppWrapper.h"
#include <iostream>
#include "Rule.h"
#include <algorithm>
#include <vector>
#include "Utils.h"
#include "Api.h"

SQLite::Database SQLiteCppWrapper::db("Tosr.db3");
SQLite::Statement SQLiteCppWrapper::queryShape(db, R"(SELECT bidId, EndFase, Zoom, Id FROM Rules 
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
        ORDER BY bidId ASC)");

SQLite::Statement SQLiteCppWrapper::queryControls(db, R"(SELECT RelBidId, EndFase, Id FROM Controls 
        WHERE RelbidId > ?
        AND MinControls <= ?
        AND MaxControls >= ?
        AND MinHcp <= ?
        AND MaxHcp >= ?
        ORDER BY RelBidId ASC)");

SQLite::Statement SQLiteCppWrapper::queryScanning(db, R"(SELECT RelBidId, Id FROM Scanning 
        WHERE RelbidId > ?
        AND (Controls1Suit = ? or Controls1Suit is null)
        AND (Controls2Suit = ? or Controls2Suit is null)
        AND (Controls3Suit = ? or Controls3Suit is null)
        AND (Controls4Suit = ? or Controls4Suit is null)
        AND (Queen1Suit = ? or Queen1Suit is null)
        AND (Queen2Suit = ? or Queen2Suit is null)
        AND (Queen3Suit = ? or Queen3Suit is null)
        AND (Queen4Suit = ? or Queen4Suit is null)
        ORDER BY RelBidId ASC)");

void SQLiteCppWrapper::GetBid(int bidId, int& rank, int& suit)
{
    SQLite::Statement query(db, "SELECT Rank, Suit, description FROM bids where id = ?");
    query.bind(1, bidId);

    if (query.executeStep())
    {
        rank = query.getColumn(0);
        suit = query.getColumn(1);
    }
}
std::tuple<int, bool> SQLiteCppWrapper::GetRule(const HandCharacteristic& hand, const Fase& fase, int lastBidId)
{
    switch (fase)
    {
    case Fase::Shape: return GetRuleShape(hand, lastBidId);
    case Fase::Controls: return GetRuleControls(hand, lastBidId);
    case Fase::Scanning: return GetRuleScanning(hand, lastBidId);
    default:
        throw std::invalid_argument(std::to_string((int)fase));
    }
}

std::tuple<int, bool> SQLiteCppWrapper::GetRuleShape(const HandCharacteristic& hand, int lastBidId)
{
    try
    {
        // Bind parameters
        queryShape.reset();
        queryShape.bind(1, lastBidId);
        queryShape.bind(2, hand.Spades);
        queryShape.bind(3, hand.Spades);
        queryShape.bind(4, hand.Hearts);
        queryShape.bind(5, hand.Hearts);
        queryShape.bind(6, hand.Diamonds);
        queryShape.bind(7, hand.Diamonds);
        queryShape.bind(8, hand.Clubs);
        queryShape.bind(9, hand.Clubs);

        queryShape.bindNoCopy(10, hand.distribution);
        queryShape.bind(11, hand.isBalanced);
        queryShape.bind(12, hand.isReverse);
        queryShape.bindNoCopy(13, hand.shortageString);
        queryShape.bind(14, hand.isThreeSuiter);
        queryShape.bind(15, hand.is65Reverse);

        // Loop to execute the query step by step, to get rows of result
        std::vector<std::tuple<int, bool>> res;
        while (queryShape.executeStep())
        {
            // Demonstrate how to get some typed column value
            int bidId = queryShape.getColumn(0);
            bool endfase = queryShape.getColumn(1).getInt();
            if (res.empty())
                DBOUT("Shape. Rule Id:" << queryShape.getColumn(3).getInt() << '\n');
       
            res.emplace_back(bidId, endfase);
        }
        std::sort(res.begin(), res.end());

        return res.empty() ? std::make_pair(0, false) : res.front();
    }
    catch (std::exception& e)
    {
        std::cerr << e.what();
        throw;
    }
}


std::tuple<int, bool> SQLiteCppWrapper::GetRuleControls(const HandCharacteristic& hand, int lastBidId)
{
    try
    {
        // Bind parameters
        queryControls.reset();

        queryControls.bind(1, lastBidId);

        queryControls.bind(2, hand.Controls);
        queryControls.bind(3, hand.Controls);

        queryControls.bind(4, hand.Hcp);
        queryControls.bind(5, hand.Hcp);

        // Loop to execute the query step by step, to get rows of result
        std::vector<std::tuple<int, bool>> res;
        while (queryControls.executeStep())
        {
            // Demonstrate how to get some typed column value
            int bidId = queryControls.getColumn(0);
            bool endFase = queryControls.getColumn(1).getInt();

            if (res.empty())
                DBOUT("Controls. Rule Id:" << queryControls.getColumn(2).getInt() << '\n');

            res.emplace_back(bidId, endFase);
        }
        std::sort(res.begin(), res.end());
        return res.empty() ? std::make_pair(0, false) : res.front();
    }
    catch (std::exception& e)
    {
        std::cerr << e.what();
        throw;
    }
}

std::tuple<int, bool> SQLiteCppWrapper::GetRuleScanning(const HandCharacteristic& hand, int lastBidId)
{
    try
    {
        // Bind parameters
        queryScanning.reset();

        queryScanning.bind(1, lastBidId);

        queryScanning.bind(2, hand.ControlsSuit[0]);
        queryScanning.bind(3, hand.ControlsSuit[1]);
        queryScanning.bind(4, hand.ControlsSuit[2]);
        queryScanning.bind(5, hand.ControlsSuit[3]);
        queryScanning.bind(6, hand.QueensSuit[0]);
        queryScanning.bind(7, hand.QueensSuit[1]);
        queryScanning.bind(8, hand.QueensSuit[2]);
        queryScanning.bind(9, hand.QueensSuit[3]);

        // Loop to execute the query step by step, to get rows of result
        std::vector<std::tuple<int, bool>> res;
        while (queryScanning.executeStep())
        {
            int bidId = queryScanning.getColumn(0);

            if (res.empty())
                DBOUT("Scanning. Rule Id:" << queryScanning.getColumn(1).getInt() << '\n');

            res.emplace_back(bidId, false);
        }
        std::sort(res.begin(), res.end());

        return res.empty() ? std::make_pair(0, false) : res.front();
    }
    catch (std::exception& e)
    {
        std::cerr << e.what();
        throw;
    }
}

