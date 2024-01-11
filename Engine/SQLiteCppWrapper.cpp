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
std::tuple<int, Phase, std::string, int> SQLiteCppWrapper::GetRule(const HandCharacteristic& hand, const Phase& phase, Phase previousPhase, int lastBidId)
{
    switch (phase)
    {
        case Phase::Shape:
        {
            auto [bidId, endphase, str, zoom] = GetRuleShape(hand, lastBidId);
            if (zoom)
            {
                auto [bidIdCtrls, endPhaseCtrls, strCtrl] = GetRuleControls(hand, 0);
                return std::make_tuple(bidId + bidIdCtrls - 1, GetNextPhase(endPhaseCtrls, NextPhase(phase)), str + "\n" + strCtrl, bidIdCtrls);
            }
            return std::make_tuple(bidId, GetNextPhase(endphase, phase), str, 0);
        }
        case Phase::Controls:
        {
            auto [bidId, endPhase, str] = GetRuleControls(hand, lastBidId);
            return std::make_tuple(bidId, GetNextPhase(endPhase, phase), str, 0);
        }
        case Phase::ScanningControls: 
        {
            auto [bidId, endPhase, str, zoom] = GetRuleScanningControls(hand, lastBidId);
            if (zoom)
            {
                auto [bidIdCtrls, endPhaseCtrls, strCtrl] = GetRuleScanningOther(hand, 0);
                return std::make_tuple(bidId + bidIdCtrls - 1, GetNextPhase(endPhaseCtrls, NextPhase(phase)), str + "\n" + strCtrl, bidIdCtrls);
            }

            return std::make_tuple(bidId, GetNextPhase(endPhase, phase), str, 0);
        }
        case Phase::ScanningOther:
        {
            auto [bidId, endPhase, str] = GetRuleScanningOther(hand, lastBidId);
            return std::make_tuple(bidId, GetNextPhase(endPhase, phase), str, 0);
        }
        case Phase::Pull3NTNoAsk:
        case Phase::Pull3NTOneAskMin:
        case Phase::Pull3NTOneAskMax:
        case Phase::Pull3NTTwoAsks:
        case Phase::Pull4DiamondsNoAsk:
        case Phase::Pull4DiamondsOneAskMin:
        case Phase::Pull4DiamondsOneAskMax:
        {
            auto [bidId, zoom, str] = GetRuleSignOff(hand, phase);
            if (zoom)
            {
                switch (previousPhase)
                {
                case Phase::Controls:
                {
                    auto [bidIdCtrls, endPhaseCtrls, strCtrl] = GetRuleControls(hand, lastBidId);
                    return std::make_tuple(bidId + bidIdCtrls - 1, GetNextPhase(endPhaseCtrls, previousPhase), str + "\n" + strCtrl, 0);
                }
                case Phase::ScanningControls:
                {
                    auto [bidIdCtrlsScanning, endPhaseCtrlsScanning, strCtrlScanning, zoom] = GetRuleScanningControls(hand, lastBidId);
                    if (zoom)
                    {
                        auto [bidIdScanningOther, endPhaseScanningOther, strScanningOther] = GetRuleScanningOther(hand, 0);
                        return std::make_tuple(bidId + (bidIdCtrlsScanning - 1) + (bidIdScanningOther - 1), 
                            GetNextPhase(endPhaseScanningOther, NextPhase(previousPhase)), str + "\n" + strCtrlScanning + "\n" + strScanningOther, bidIdScanningOther);
                    }
                    return std::make_tuple(bidId + bidIdCtrlsScanning - 1, GetNextPhase(endPhaseCtrlsScanning, previousPhase), str + "\n" + strCtrlScanning, 0);
                }
                case Phase::ScanningOther:
                {
                    auto [bidIdScanningOther, endPhaseScanningOther, strScanningOther] = GetRuleScanningOther(hand, lastBidId);
                    return std::make_tuple(bidId + bidIdScanningOther - 1, GetNextPhase(endPhaseScanningOther, previousPhase), str + "\n" + strScanningOther, 0);
                }

                default:
                    throw std::invalid_argument(std::to_string((int)previousPhase));
                }
            }
            auto nextPhase = bidId == 0 ? Phase::BidGame : previousPhase;
            return std::make_tuple(bidId, nextPhase, str, 0);
        }

        default:
            throw std::invalid_argument(std::to_string((int)phase));
    }
}

Phase SQLiteCppWrapper::GetNextPhase(bool endphase, Phase phase) noexcept
{
    return endphase ? NextPhase(phase) : phase;
}

Phase SQLiteCppWrapper::NextPhase(Phase phase) noexcept
{
    return (Phase)((int)phase + 1);
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
        bool endPhase = queryShape->getColumn(1).getInt();
        bool zoom = queryShape->getColumn(2).getInt();
        auto id = queryShape->getColumn(3).getInt();
        auto str = queryShape->getColumn(4).getString();

        DBOUT("Shape. Rule Id:" << id << '\n');

        return std::make_tuple(bidId, endPhase, str, zoom);
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
        bool endPhase = queryControls->getColumn(1).getInt();
        auto id = queryControls->getColumn(2).getInt();
        auto str = queryControls->getColumn(3).getString();

        DBOUT("Controls. Rule Id:" << id << '\n');

        return std::make_tuple(bidId, endPhase, str);
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
        bool endPhase = queryScanningControls->getColumn(1).getInt();
        bool zoom = queryScanningControls->getColumn(2).getInt();
        auto id = queryScanningControls->getColumn(3).getInt();
        auto str = queryScanningControls->getColumn(4).getString();

        DBOUT("Scanning controls. Rule Id:" << id << '\n');

        return std::make_tuple(bidId, endPhase, str, zoom);
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
        bool endPhase = queryScanningOther->getColumn(1).getInt();
        auto id = queryScanningOther->getColumn(2).getInt();
        auto str = queryScanningOther->getColumn(3).getString();

        DBOUT("Scanning other. Rule Id:" << id << '\n');

        return std::make_tuple(bidId, endPhase, str);
    }
    catch (const std::exception& e)
    {
        std::cerr << e.what();
        throw;
    }
}

std::tuple<int, bool, std::string> SQLiteCppWrapper::GetRuleSignOff(const HandCharacteristic& hand, Phase phase)
{
    try
    {
        // Bind parameters
        querySignOffs->reset();

        querySignOffs->bind(1, (int)phase);

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