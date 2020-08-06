#include "pch.h"
#include "Rule.h"
#include "Utils.h"
#include <algorithm>
#include <cassert>
#include <iterator>

HandCharacteristic::HandCharacteristic(const std::string& hand)
{
    Initialize(hand);
}

void HandCharacteristic::Initialize(const std::string& hand)
{
    this->hand = hand;
    assert(hand.length() == 16);

    auto suits = Utils::Split<char>(hand, ',');
    assert(suits.size() == 4);
    std::unordered_map<int, size_t> suitLength = {
        {0, suits[0].length()}, 
        {1, suits[1].length()},
        {2, suits[2].length()}, 
        {3, suits[3].length()}};

    Spades = (int)suitLength.at(0);
    Hearts = (int)suitLength.at(1);
    Diamonds = (int)suitLength.at(2);
    Clubs = (int)suitLength.at(3);

    std::sort(suits.begin(), suits.end(), [] (const auto& l, const auto& r) {return l.length() > r.length();});
    distribution = std::to_string(suits[0].length()) + std::to_string(suits[1].length()) + 
        std::to_string(suits[2].length())  + std::to_string(suits[3].length());
    isBalanced = distribution == "4333" || distribution == "4432";
    isThreeSuiter = CalcuateIsThreeSuiter(suitLength);
    isReverse = !isBalanced && !isThreeSuiter && CalcuateIsReverse(suitLength);
    is65Reverse = !isBalanced && !isThreeSuiter && Calcuate65IsReverse(suitLength);
    shortage = CalculateShortage(suitLength);
    Controls = CalculateControls(hand);
    shortageString = ConvertShortage(shortage);
    Hcp = CalculateHcp(hand);
    ControlsSuit = CalculateControlsSuit(suits);
    QueensSuit = CalculateQueensSuit(suits);
}

bool HandCharacteristic::CalcuateIsReverse(const std::unordered_map<int, size_t>& suitLength)
{
    std::unordered_map<int, size_t> longSuits;
    std::copy_if(suitLength.begin(), suitLength.end(), std::inserter(longSuits, longSuits.begin()), [] (const auto &pair) {return pair.second > 3;});
    return longSuits.begin()->second == 4;
}

bool HandCharacteristic::Calcuate65IsReverse(const std::unordered_map<int, size_t>& suitLength)
{
    std::unordered_map<int, size_t> longSuits;
    std::copy_if(suitLength.begin(), suitLength.end(), std::inserter(longSuits, longSuits.begin()), [] (const auto &pair) {return pair.second > 3;});
    return longSuits.begin()->second == 5;
}

Shortage HandCharacteristic::CalculateShortage(const std::unordered_map<int, size_t>& suitLength)
{
    std::unordered_map<int, size_t> shortSuits;
    std::copy_if(suitLength.begin(), suitLength.end(), std::inserter(shortSuits, shortSuits.begin()), [] (const auto &pair) {return pair.second < 4;});
    auto minElement = std::min_element(shortSuits.begin(), shortSuits.end(), [] (const auto& l, const auto& r) {return l.second < r.second; });
    // One suiters
    if (shortSuits.size() == 3)
    {
        if (std::count_if(shortSuits.begin(), shortSuits.end(), [&minElement] (const auto& pair) {return pair.second == minElement->second; }) == 3)
        {
            return Shortage::EqualOne;
        }
        if (std::count_if(shortSuits.begin(), shortSuits.end(), [&minElement] (const auto& pair) {return pair.second == minElement->second; }) == 2)
        {
            auto maxElement = std::max_element(shortSuits.begin(), shortSuits.end(), [] (const auto& l, const auto& r) {return l.second < r.second; });
            if (maxElement == shortSuits.begin())
            {
                return Shortage::EqualHighOne;
            }
            if (std::next(maxElement) == shortSuits.end())
            {
                return Shortage::EqualLowOne;
            }
            return Shortage::EqualMiddleOne;
        }

        if (minElement == shortSuits.begin())
        {
            return Shortage::HighOne;
        }
        if (std::next(minElement) == shortSuits.end())
        {
            return Shortage::LowOne;
        }
        return Shortage::MiddleOne;
    }
    // Two suiters
    auto is55 = std::count_if(suitLength.begin(), suitLength.end(), [] (const auto& pair) {return pair.second >= 5;}) == 2;
    if (shortSuits.size() == 2)
    {
        if (std::count_if(shortSuits.begin(), shortSuits.end(), [&minElement] (const auto& pair) {return pair.second == minElement->second; }) > 1)
        {
            return is55 ? Shortage::Equal55Two : Shortage::EqualTwo;
        }
        if (minElement == shortSuits.begin())
        {
            return is55 ? Shortage::High55Two : Shortage::HighTwo;
        }
        return is55 ? Shortage::Low55Two : Shortage::LowTwo;
    }
    return Shortage::Unknown;
}

int HandCharacteristic::CalculateControls(const std::string& hand)
{
    return NumberOfCards(hand, 'A') * 2 + NumberOfCards(hand, 'K');
}

bool HandCharacteristic::CalcuateIsThreeSuiter(const std::unordered_map<int, size_t>& suitLength)
{
    return std::count_if(suitLength.begin(), suitLength.end(), [] (const auto &pair) {return pair.second > 3;}) == 3;
}

std::string HandCharacteristic::ConvertShortage(Shortage shortage)
{
    switch (shortage)
    {
    case Shortage::HighOne: return "H1";
    case Shortage::MiddleOne: return "M1";
    case Shortage::LowOne: return "L1";
    case Shortage::EqualHighOne: return "EH1";
    case Shortage::EqualMiddleOne: return "EM1";
    case Shortage::EqualLowOne: return "EL1";
    case Shortage::EqualOne: return "E1";
    case Shortage::HighTwo: return "H2";
    case Shortage::LowTwo: return "L2";
    case Shortage::EqualTwo: return "E2";
    case Shortage::High55Two: return "55H2";
    case Shortage::Low55Two: return "55L2";
    case Shortage::Equal55Two: return "55E2";
    default:
        return "";
    }
}

int HandCharacteristic::CalculateHcp(const std::string& hand)
{
    const auto aces = NumberOfCards(hand, 'A');
    const auto kings = NumberOfCards(hand, 'K');
    const auto queens = NumberOfCards(hand, 'Q');
    const auto jacks = NumberOfCards(hand, 'J');
    return aces * 4 + kings * 3 + queens * 2 + jacks;
}

int HandCharacteristic::NumberOfCards(const std::string& hand, char card)
{
    return (int)std::count_if(hand.begin(), hand.end(), [card](char c) {return c == card; });
}

std::vector<int> HandCharacteristic::CalculateControlsSuit(const std::vector<std::string>& suits)
{
    std::vector<int> controlsSuit;
    std::transform(suits.begin(), suits.end(), std::back_inserter(controlsSuit),
        [](const auto& suit) 
        {
            return NumberOfCards(suit, 'A') * 2 + NumberOfCards(suit, 'K');
        });

    return controlsSuit;
}

std::vector<bool> HandCharacteristic::CalculateQueensSuit(const std::vector<std::string>& suits)
{
    std::vector<bool> queensSuit;
    std::transform(suits.begin(), suits.end(), std::back_inserter(queensSuit), 
        [](const auto& suit){ return NumberOfCards(suit, 'Q') == 1; });
    return queensSuit;
}