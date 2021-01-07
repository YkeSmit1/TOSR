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
    SQLite::Statement query(*db, "SELECT Rank, Suit, description FROM bids where id = ?");
    query.bind(1, bidId);

    if (query.executeStep())
    {
        rank = query.getColumn(0);
        suit = query.getColumn(1);
    }
}
std::tuple<int, Fase, std::string, int> SQLiteCppWrapper::GetRule(const HandCharacteristic& hand, const Fase& fase, Fase previousFase, int lastBidId)
{
    switch (fase)
    {
        case Fase::Shape:
        {
            auto [bidId, endfase, str, zoom] = GetRuleShape(hand, lastBidId);
            if (zoom)
            {
                auto [bidIdCtrls, endfaseCtrls, strCtrl] = GetRuleControls(hand, 0);
                return std::make_tuple(bidId + bidIdCtrls - 1, GetNextFase(endfaseCtrls, NextFase(fase)), str + "\n" + strCtrl, bidIdCtrls);
            }
            return std::make_tuple(bidId, GetNextFase(endfase, fase), str, 0);
        }
        case Fase::Controls:
        {
            auto [bidId, endfase, str] = GetRuleControls(hand, lastBidId);
            return std::make_tuple(bidId, GetNextFase(endfase, fase), str, 0);
        }
        case Fase::ScanningControls: 
        {
            auto [bidId, endfase, str, zoom] = GetRuleScanningControls(hand, lastBidId);
            if (zoom)
            {
                auto [bidIdCtrls, endfaseCtrls, strCtrl] = GetRuleScanningOther(hand, 0);
                return std::make_tuple(bidId + bidIdCtrls - 1, GetNextFase(endfaseCtrls, NextFase(fase)), str + "\n" + strCtrl, bidIdCtrls);
            }

            return std::make_tuple(bidId, GetNextFase(endfase, fase), str, 0);
        }
        case Fase::ScanningOther:
        {
            auto [bidId, endfase, str] = GetRuleScanningOther(hand, lastBidId);
            return std::make_tuple(bidId, GetNextFase(endfase, fase), str, 0);
        }
        case Fase::Pull3NTNoAsk:
        case Fase::Pull3NTOneAskMin:
        case Fase::Pull3NTOneAskMax:
        case Fase::Pull3NTTwoAsks:
        case Fase::Pull4DiamondsNoAsk:
        case Fase::Pull4DiamondsOneAskMin:
        case Fase::Pull4DiamondsOneAskMax:
        {
            auto [bidId, zoom, str] = GetRuleSignOff(hand, fase);
            if (zoom)
            {
                switch (previousFase)
                {
                case Fase::Controls:
                {
                    auto [bidIdCtrls, endfaseCtrls, strCtrl] = GetRuleControls(hand, lastBidId);
                    return std::make_tuple(bidId + bidIdCtrls - 1, GetNextFase(endfaseCtrls, previousFase), str + "\n" + strCtrl, 0);
                }
                case Fase::ScanningControls:
                {
                    auto [bidIdCtrlsScanning, endfaseCtrlsScanning, strCtrlScanning, zoom] = GetRuleScanningControls(hand, lastBidId);
                    if (zoom)
                    {
                        auto [bidIdScanningOther, endfaseScanningOther, strScanningOther] = GetRuleScanningOther(hand, 0);
                        return std::make_tuple(bidId + (bidIdCtrlsScanning - 1) + (bidIdScanningOther - 1), 
                            GetNextFase(endfaseScanningOther, NextFase(previousFase)), str + "\n" + strCtrlScanning + "\n" + strScanningOther, bidIdScanningOther);
                    }
                    return std::make_tuple(bidId + bidIdCtrlsScanning - 1, GetNextFase(endfaseCtrlsScanning, previousFase), str + "\n" + strCtrlScanning, 0);
                }
                case Fase::ScanningOther:
                {
                    auto [bidIdScanningOther, endfaseScanningOther, strScanningOther] = GetRuleScanningOther(hand, lastBidId);
                    return std::make_tuple(bidId + bidIdScanningOther - 1, GetNextFase(endfaseScanningOther, previousFase), str + "\n" + strScanningOther, 0);
                }

                default:
                    throw std::invalid_argument(std::to_string((int)previousFase));
                }
            }
            auto nextFase = bidId == 0 ? Fase::BidGame : previousFase;
            return std::make_tuple(bidId, nextFase, str, 0);
        }

        default:
            throw std::invalid_argument(std::to_string((int)fase));
    }
}

Fase SQLiteCppWrapper::GetNextFase(bool endfase, Fase fase) noexcept
{
    return endfase ? NextFase(fase) : fase;
}

Fase SQLiteCppWrapper::NextFase(Fase fase) noexcept
{
    return (Fase)((int)fase + 1);
}

std::tuple<int, bool, std::string, bool> SQLiteCppWrapper::GetRuleShape(const HandCharacteristic& hand, int lastBidId)
{
    try
    {
        // Bind parameters
        queryShape->reset();
        queryShape->bind(1, lastBidId);
        queryShape->bind(2, hand.Spades);
        queryShape->bind(3, hand.Hearts);
        queryShape->bind(4, hand.Diamonds);
        queryShape->bind(5, hand.Clubs);

        queryShape->bindNoCopy(6, hand.distribution);
        queryShape->bind(7, hand.isBalanced);
        queryShape->bind(8, hand.isReverse);
        queryShape->bindNoCopy(9, hand.shortageString);
        queryShape->bind(10, hand.isThreeSuiter);
        queryShape->bind(11, hand.is65Reverse);

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
    catch (const std::exception& e)
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
        queryControls->bind(3, hand.Hcp);

        if (!queryControls->executeStep())
        {
            //return std::make_tuple(1, true, "");
            throw std::runtime_error("No row found in controls table.");
        }

        int bidId = queryControls->getColumn(0);
        bool endFase = queryControls->getColumn(1).getInt();
        auto id = queryControls->getColumn(2).getInt();
        auto str = queryControls->getColumn(3).getString();

        DBOUT("Controls. Rule Id:" << id << '\n');

        return std::make_tuple(bidId, endFase, str);
    }
    catch (const std::exception& e)
    {
        std::cerr << e.what();
        throw;
    }
}

std::tuple<int, bool, std::string, bool> SQLiteCppWrapper::GetRuleScanningControls(const HandCharacteristic& hand, int lastBidId)
{
    try
    {
        // Bind parameters
        queryScanningControls->reset();

        queryScanningControls->bind(1, lastBidId);

        queryScanningControls->bind(2, hand.ControlsSuit[0]);
        queryScanningControls->bind(3, hand.ControlsSuit[1]);
        queryScanningControls->bind(4, hand.ControlsSuit[2]);
        queryScanningControls->bind(5, hand.ControlsSuit[3]);
        queryScanningControls->bind(6, hand.Shortages);

        if (!queryScanningControls->executeStep())
            throw std::runtime_error("No row found in scanning table.");

        int bidId = queryScanningControls->getColumn(0);
        bool endFase = queryScanningControls->getColumn(1).getInt();
        bool zoom = queryScanningControls->getColumn(2).getInt();
        auto id = queryScanningControls->getColumn(3).getInt();
        auto str = queryScanningControls->getColumn(4).getString();

        DBOUT("Scanning controls. Rule Id:" << id << '\n');

        return std::make_tuple(bidId, endFase, str, zoom);
    }
    catch (const std::exception& e)
    {
        std::cerr << e.what();
        throw;
    }
}

std::tuple<int, bool, std::string> SQLiteCppWrapper::GetRuleScanningOther(const HandCharacteristic& hand, int lastBidId)
{
    try
    {
        // Bind parameters
        queryScanningOther->reset();

        queryScanningOther->bind(1, lastBidId);

        queryScanningOther->bind(2, hand.QueensSuit[0]);
        queryScanningOther->bind(3, hand.QueensSuit[1]);
        queryScanningOther->bind(4, hand.QueensSuit[2]);
        queryScanningOther->bind(5, hand.QueensSuit[3]);
        queryScanningOther->bind(6, hand.Shortages);

        if (!queryScanningOther->executeStep())
            throw std::runtime_error("No row found in scanning table.");

        int bidId = queryScanningOther->getColumn(0);
        bool endFase = queryScanningOther->getColumn(1).getInt();
        auto id = queryScanningOther->getColumn(2).getInt();
        auto str = queryScanningOther->getColumn(3).getString();

        DBOUT("Scanning other. Rule Id:" << id << '\n');

        return std::make_tuple(bidId, endFase, str);
    }
    catch (const std::exception& e)
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

        querySignOffs->bind(2, hand.Hcp);
        querySignOffs->bind(3, hand.Queens);

        if (!querySignOffs->executeStep())
            throw std::runtime_error("No row found in SignOff table.");

        int bidId = querySignOffs->getColumn(0);
        bool zoom = querySignOffs->getColumn(1).getInt();
        auto id = querySignOffs->getColumn(2).getInt();
        auto str = querySignOffs->getColumn(3).getString();

        DBOUT("SignOff. Rule Id:" << id << '\n');

        return std::make_tuple(bidId, zoom, str);
    }
    catch (const std::exception& e)
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
    queryScanningControls = std::make_unique<SQLite::Statement>(*db, scanningControlsSql.data());
    queryScanningOther = std::make_unique<SQLite::Statement>(*db, scanningOtherSql.data());
    querySignOffs = std::make_unique<SQLite::Statement>(*db, signOffsSql.data());
}