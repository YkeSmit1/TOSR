#pragma once

#include <string>
#include "ISQLiteWrapper.h"

class Rule;
struct HandCharacteristic;

class SQLiteOrmWrapper : public ISQLiteWrapper
{
public:
	void GetBid(int bidId, int& rank, int& suit) final override;
	SQLiteOrmWrapper();
	void TestUser();
	std::tuple<int, bool> GetRule(const HandCharacteristic& handCharacteristic, const Fase& fase, int lastBidId) final override;
};

