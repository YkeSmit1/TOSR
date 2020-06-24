#pragma once
#include <string>
#include <map>
#include <array>
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
	
	int Spades;
	int Hearts;
	int Diamonds;
	int Clubs;

	std::string distribution;
	int Controls;
	bool isBalanced;
	bool isReverse;
	bool is65Reverse;
	bool isThreeSuiter;
	Shortage shortage;
	std::string shortageString;

	std::vector<int> ControlsSuit;
	std::vector<bool> QueensSuit;

	int Hcp;

	static bool CalcuateIsReverse(const std::map<int, size_t>& suitLength);
	static bool Calcuate65IsReverse(const std::map<int, size_t>& suitLength);
	static Shortage CalculateShortage(const std::map<int, size_t>& suitLength);
	static int CalculateControls(const std::string& hand);
	static bool CalcuateIsThreeSuiter(const std::map<int, size_t>& suitLength);
	static std::string ConvertShortage(Shortage shortage);
	static int CalculateHcp(const std::string& hand);
	std::vector<int> CalculateControlsSuit(const std::vector<std::string>& suits);
	std::vector<bool> CalculateQueensSuit(const std::vector<std::string>& suits);
	void Initialize(const std::string& hand);
	explicit HandCharacteristic(const std::string& hand);
	HandCharacteristic() = default;
};


class Rule
{
public :
	int id;
	int bidId;
	int faseId;
	int nextFaseId;

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
