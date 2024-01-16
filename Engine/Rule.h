#pragma once
#include <string>
#include <unordered_map>
#include <vector>

enum class Shortage
{
	Unknown,
	HighOne,
	MiddleOne,
	LowOne,
	EqualHighOne,
	EqualMiddleOne,
	EqualLowOne,
	EqualOne,
	HighTwo,
	LowTwo,
	EqualTwo,
	High55Two,
	Low55Two,
	Equal55Two
};

enum class Player { West, North, East, South };

struct HandCharacteristic
{
	std::string hand {};
	
	int spades = 0;
	int hearts = 0;
	int diamonds = 0;
	int clubs = 0;

	std::string distribution;
	int controls = 0;
	bool isBalanced = false;
	bool isReverse = false;
	bool is65Reverse = false;
	bool isThreeSuiter = false;
	Shortage shortage = Shortage::Unknown;
	std::string shortageString;

	std::vector<int> controlsSuit;
	std::vector<bool> queensSuit;

	int hcp = 0;
	int queens = 0;
	int shortages = 0;

	static bool CalculateIsReverse(const std::unordered_map<int, size_t>& suitLength);
	static bool Calculate65IsReverse(const std::unordered_map<int, size_t>& suitLength);
	static Shortage CalculateShortage(const std::unordered_map<int, size_t>& suitLength);
	static int CalculateControls(const std::string& hand);
	static bool CalculateIsThreeSuiter(const std::unordered_map<int, size_t>& suitLength);
	static std::string ConvertShortage(Shortage shortage);
	static int CalculateHcp(const std::string& hand);
	static int NumberOfCards(const std::string& hand, char card);
	std::vector<int> CalculateControlsSuit(const std::vector<std::string>& suits) const;
	std::vector<bool> CalculateQueensSuit(const std::vector<std::string>& suits) const;
	void Initialize(const std::string& hand);
	explicit HandCharacteristic(const std::string& hand);
	HandCharacteristic() = default;
};


class Rule
{
public :
	int id;
	int bidId;
	int phaseId;
	int nextPhaseId;

	int minSpades;
	int maxSpades;
	int minHearts;
	int maxHearts;
	int minDiamonds;
	int maxDiamonds;
	int minClubs;
	int maxClubs;

	std::string distribution;
	int minControls;
	int maxControls;

	bool isBalanced;
};
