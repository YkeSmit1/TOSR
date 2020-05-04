#include "pch.h"
#include "SQLiteCppWrapper.h"
#include "SQLiteCpp/SQLiteCpp.h"
#include <iostream>
#include "Rule.h"

void SQLiteCppWrapper::GetBid(int bidId, int& rank, int& suit)
{
    SQLite::Database db("Tosr.db3");
    SQLite::Statement query(db, "SELECT Rank, Suit, description FROM bids where id = ?");
    query.bind(1, bidId);

    if (query.executeStep())
    {
        rank = query.getColumn(0);
        suit = query.getColumn(1);
    }
}

std::string SQLiteCppWrapper::ConvertShortage(Shortage shortage)
{
    switch (shortage)
    {
    case Shortage::HighOne: return "H1";
    case Shortage::MiddleOne: return "M1";
    case Shortage::LowOne: return "L1";
    case Shortage::EqualOne: return "E1";
    case Shortage::HighTwo: return "H2";
    case Shortage::LowTwo: return "L2";
    case Shortage::EqualTwo: return "E2";
    default:
        return "";
    }
}

std::vector<std::tuple<int, int>> SQLiteCppWrapper::GetRules(const HandCharacteristic& hand, int faseId, int lastBidId)
{
    try
    {
        std::vector<std::tuple<int, int>> res;

        SQLite::Database    db("Tosr.db3");

        // Compile a SQL query, containing one parameter (index 1)
        SQLite::Statement   query(db, R"(SELECT bidId, nextFaseId FROM Rules 
        WHERE FaseId = ?
        AND MinSpades <= ?
        AND MaxSpades >= ?
        AND MinHearts <= ?
        AND MaxHearts >= ?
        AND MinDiamonds <= ?
        AND MaxDiamonds >= ?
        AND MinClubs <= ?
        AND MaxClubs >= ?
        AND MinControls <= ?
        AND MaxControls >= ?
        AND (Distribution IS NULL or Distribution = ?)
        AND (IsBalanced IS NULL or IsBalanced = ?)
        AND (IsReverse IS NULL or IsReverse = ?)
        AND (Shortage IS NULL or Shortage = ?)
        AND (bidId > ?))");

        // Bind parameters
        query.bind(1, faseId);
        query.bind(2, hand.Spades);
        query.bind(3, hand.Spades);
        query.bind(4, hand.Hearts);
        query.bind(5, hand.Hearts);
        query.bind(6, hand.Diamonds);
        query.bind(7, hand.Diamonds);
        query.bind(8, hand.Clubs);
        query.bind(9, hand.Clubs);
        query.bind(10, hand.Controls);
        query.bind(11, hand.Controls);

        query.bind(12, hand.distribution);
        query.bind(13, hand.isBalanced);
        query.bind(14, hand.isReverse);
        query.bind(15, ConvertShortage(hand.shortage));
        query.bind(16, lastBidId);

        // Loop to execute the query step by step, to get rows of result
        while (query.executeStep())
        {
            // Demonstrate how to get some typed column value
            int bidId = query.getColumn(0);
            int nextFaseId = query.getColumn(1);
       
            res.emplace_back(bidId, nextFaseId);
        }
        return res;
    }
    catch (std::exception& e)
    {
        std::cerr << e.what();
        throw;
    }
}
