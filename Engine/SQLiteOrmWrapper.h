#pragma once
#include <iosfwd>
#include <tuple>
#include <vector>
#include <string>
#include "ISQLiteWrapper.h"

class Rule;
struct HandCharacteristic;

class SQLiteOrmWrapper : public ISQLiteWrapper
{
public:
	void GetBid(int bidId, int& rank, int& suit)
 override;
	SQLiteOrmWrapper();
	void TestUser();
	std::vector<std::tuple<int, int>> GetRules(const HandCharacteristic& handCharacteristic, int faseId, int lastBidId) override;
};

