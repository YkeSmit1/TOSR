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
std::tuple<int, Fase, std::string, bool> SQLiteCppWrapper::GetRule(const HandCharacteristic& hand, const Fase& fase, Fase previousFase, int lastBidId)
{
    switch (fase)
    {
        case Fase::Shape:
        {
            auto [bidId, endfase, str, zoom] = GetRuleShape(hand, lastBidId);
            if (zoom)
            {
                auto [bidIdCtrls, endfaseCtrls, strCtrl] = GetRuleControls(hand, 0);
                return std::make_tuple(bidId + bidIdCtrls - 1, endfaseCtrls ? Fase::Scanning : Fase::Controls, str + "\n" + strCtrl, zoom);
            }
            return std::make_tuple(bidId, endfase ? Fase::Controls : fase, str, zoom);
        }
        case Fase::Controls:
        {
            auto [bidId, endfase, str] = GetRuleControls(hand, lastBidId);
            return std::make_tuple(bidId, endfase ? Fase::Scanning : fase, str, false);
        }
        case Fase::Scanning: 
        {
            auto [bidId, endfase, str] = GetRuleScanning(hand, lastBidId);
            return std::make_tuple(bidId, endfase ? Fase::End : fase, str, false);
        }
        case Fase::Pull3NTNoAsk:
        case Fase::Pull3NTOneAsk:
        case Fase::Pull3NTTwoAsks:
        case Fase::Pull4DiamondsNoAsk:
        case Fase::Pull4DiamondsOneAsk:
        {
            auto [bidId, zoom, str] = GetRuleSignOff(hand, fase);
            if (zoom)
            {
                auto [bidIdCtrls, endfaseCtrls, strCtrl] = GetRuleControls(hand, 0);
                return std::make_tuple(bidId + bidIdCtrls - 1, endfaseCtrls ? previousFase : (Fase)((int)previousFase + 1), str + "\n" + strCtrl, zoom);
            }
            auto nextFase = bidId == 1 && (fase == Fase::Pull4DiamondsNoAsk || fase == Fase::Pull4DiamondsOneAsk) ? Fase::BidGame : previousFase;
            return std::make_tuple(bidId, nextFase, str, zoom);
        }

        default:
            throw std::invalid_argument(std::to_string((int)fase));
    }
}

std::tuple<int, bool, std::string, bool> SQLiteCppWrapper::GetRuleShape(const HandCharacteristic& hand, int lastBidId)
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

        if (!queryShape->executeStep())
            throw std::runtime_error("No row found in rules table.");

        int bidId = queryShape->getColumn(0);
        bool endfase = queryShape->getColumn(1).getInt();
        bool zoom = queryShape->getColumn(2).getInt();
        auto id = queryShape->getColumn(3).getInt();
        auto str = queryShape->getColumn(4).getString();

        DBOUT("Shape. Rule Id:" << id << '\n');

        return std::make_tuple(bidId, endfase, str, zoom);
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

        if (!queryControls->executeStep())
        {
            std::string emptystring;
            return std::make_tuple(1, true, emptystring);
            //throw std::runtime_error("No row found in controls table.");
        }

        int bidId = queryControls->getColumn(0);
        bool endFase = queryControls->getColumn(1).getInt();
        auto id = queryControls->getColumn(2).getInt();
        auto str = queryControls->getColumn(3).getString();

        DBOUT("Controls. Rule Id:" << id << '\n');

        return std::make_tuple(bidId, endFase, str);
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

        if (!queryScanning->executeStep())
            throw std::runtime_error("No row found in scanning table.");

        int bidId = queryScanning->getColumn(0);
        bool endFase = queryScanning->getColumn(1).getInt();
        auto id = queryScanning->getColumn(2).getInt();
        auto str = queryScanning->getColumn(3).getString();

        DBOUT("Scanning. Rule Id:" << id << '\n');

        return std::make_tuple(bidId, endFase, str);
    }
    catch (std::exception& e)
    {
        std::cerr << e.what();
        throw;
    }
}

std::tuple<int, bool, std::string> SQLiteCppWrapper::GetRuleSignOff(const HandCharacteristic& hand, Fase fase)
{
    try
    {
        // Bind parameters
        querySignOffs->reset();

        querySignOffs->bind(1, (int)fase);

        querySignOffs->bind(2, hand.isMax);
        querySignOffs->bind(3, hand.Hcp);
        querySignOffs->bind(4, hand.Hcp);
        querySignOffs->bind(5, hand.Queens);
        querySignOffs->bind(6, hand.Queens);

        if (!querySignOffs->executeStep())
            throw std::runtime_error("No row found in SignOff table.");

        int bidId = querySignOffs->getColumn(0);
        bool zoom = querySignOffs->getColumn(1).getInt();
        auto id = querySignOffs->getColumn(2).getInt();
        auto str = querySignOffs->getColumn(3).getString();

        DBOUT("SignOff. Rule Id:" << id << '\n');

        return std::make_tuple(bidId, zoom, str);
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
    querySignOffs = std::make_unique<SQLite::Statement>(*db, signOffsSql.data());
}