#include "pch.h"
#include "SQLiteCppWrapper.h"
#include <iostream>
#include "Rule.h"
#include <algorithm>
#include <vector>
#include "Utils.h"
#include "Api.h"

SQLiteCppWrapper::SQLiteCppWrapper(const std::string& database)
{
    SetDatabase(database);
}

void SQLiteCppWrapper::GetBid(int bidId, int& rank, int& suit)
{
    auto query = std::make_unique<SQLite::Statement>(*db, "SELECT Rank, Suit, description FROM bids where id = ?");
    query->bind(1, bidId);

    if (query->executeStep())
    {
        rank = query->getColumn(0);
        suit = query->getColumn(1);
    }
}
std::tuple<int, bool, std::string> SQLiteCppWrapper::GetRule(const HandCharacteristic& hand, const Fase& fase, int lastBidId)
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

std::tuple<int, bool, std::string> SQLiteCppWrapper::GetRuleShape(const HandCharacteristic& hand, int lastBidId)
{
    try
    {
        // Bind parameters
        queryShape->reset();
        queryShape->bind(1, lastBidId);
        queryShape->bind(2, hand.Spades);
        queryShape->bind(3, hand.Spades);
        queryShape->bind(4, hand.Hearts);
        queryShape->bind(5, hand.Hearts);
        queryShape->bind(6, hand.Diamonds);
        queryShape->bind(7, hand.Diamonds);
        queryShape->bind(8, hand.Clubs);
        queryShape->bind(9, hand.Clubs);

        queryShape->bindNoCopy(10, hand.distribution);
        queryShape->bind(11, hand.isBalanced);
        queryShape->bind(12, hand.isReverse);
        queryShape->bindNoCopy(13, hand.shortageString);
        queryShape->bind(14, hand.isThreeSuiter);
        queryShape->bind(15, hand.is65Reverse);

        // Loop to execute the query step by step, to get rows of result
        std::vector<std::tuple<int, bool, std::string>> res;
        while (queryShape->executeStep())
        {
            // Demonstrate how to get some typed column value
            int bidId = queryShape->getColumn(0);
            bool endfase = queryShape->getColumn(1).getInt();
            auto id = queryShape->getColumn(3).getInt();
            auto str = queryShape->getColumn(4).getString();

            if (res.empty())
                DBOUT("Shape. Rule Id:" << id << '\n');

            res.emplace_back(bidId, endfase, str);
        }

        std::string emptystring;
        return res.empty() ? std::make_tuple(0, false, emptystring) : res.front();
    }
    catch (std::exception& e)
    {
        std::cerr << e.what();
        throw;
    }
}


std::tuple<int, bool, std::string> SQLiteCppWrapper::GetRuleControls(const HandCharacteristic& hand, int lastBidId)
{
    try
    {
        // Bind parameters
        queryControls->reset();

        queryControls->bind(1, lastBidId);

        queryControls->bind(2, hand.Controls);
        queryControls->bind(3, hand.Controls);

        queryControls->bind(4, hand.Hcp);
        queryControls->bind(5, hand.Hcp);

        // Loop to execute the query step by step, to get rows of result
        std::vector<std::tuple<int, bool, std::string>> res;
        while (queryControls->executeStep())
        {
            // Demonstrate how to get some typed column value
            int bidId = queryControls->getColumn(0);
            bool endFase = queryControls->getColumn(1).getInt();
            auto id = queryControls->getColumn(2).getInt();
            auto str = queryControls->getColumn(3).getString();

            if (res.empty())
                DBOUT("Controls. Rule Id:" << id << '\n');

            res.emplace_back(bidId, endFase, str);
        }
        std::string emptystring;
        return res.empty() ? std::make_tuple(0, false, emptystring) : res.front();
    }
    catch (std::exception& e)
    {
        std::cerr << e.what();
        throw;
    }
}

std::tuple<int, bool, std::string> SQLiteCppWrapper::GetRuleScanning(const HandCharacteristic& hand, int lastBidId)
{
    try
    {
        // Bind parameters
        queryScanning->reset();

        queryScanning->bind(1, lastBidId);

        queryScanning->bind(2, hand.ControlsSuit[0]);
        queryScanning->bind(3, hand.ControlsSuit[1]);
        queryScanning->bind(4, hand.ControlsSuit[2]);
        queryScanning->bind(5, hand.ControlsSuit[3]);
        queryScanning->bind(6, hand.QueensSuit[0]);
        queryScanning->bind(7, hand.QueensSuit[1]);
        queryScanning->bind(8, hand.QueensSuit[2]);
        queryScanning->bind(9, hand.QueensSuit[3]);

        // Loop to execute the query step by step, to get rows of result
        std::vector<std::tuple<int, bool, std::string>> res;
        while (queryScanning->executeStep())
        {
            int bidId = queryScanning->getColumn(0);
            bool endFase = queryScanning->getColumn(1).getInt();
            auto id = queryScanning->getColumn(2).getInt();
            auto str = queryScanning->getColumn(3).getString();

            if (res.empty())
                DBOUT("Scanning. Rule Id:" << id << '\n');

            res.emplace_back(bidId, endFase, str);
        }

        std::string emptystring;
        return res.empty() ? std::make_tuple(0, false, emptystring) : res.front();
    }
    catch (std::exception& e)
    {
        std::cerr << e.what();
        throw;
    }
}

void SQLiteCppWrapper::SetDatabase(const std::string& database)
{
    db.release();
    db = std::make_unique<SQLite::Database>(database);

    queryShape = std::make_unique<SQLite::Statement>(*db, shapeSql.data());
    queryControls = std::make_unique<SQLite::Statement>(*db, controlsSql.data());
    queryScanning = std::make_unique<SQLite::Statement>(*db, scanningSql.data());
}