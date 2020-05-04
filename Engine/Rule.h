#pragma once
#include <string>
#include <map>

enum class Shortage { Unknown, HighOne, MiddleOne, LowOne, EqualOne, HighTwo, LowTwo, EqualTwo};

enum class Player { West, North, East, South };

struct HandCharacteristic
{
	int Spades;
	int Hearts;
	int Diamonds;
	int Clubs;

	std::string distribution;
	int Controls;
	bool isBalanced;
	bool isReverse;
	Shortage shortage;

	static bool CalcuateIsReverse(const std::map<int, size_t>& suitLength, bool isBalanced);
	static Shortage CalculateShortage(const std::map<int, size_t>& suitLength);
	static int CalculateControls(const std::string& hand);
	explicit HandCharacteristic(const std::string& hand);
	
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
