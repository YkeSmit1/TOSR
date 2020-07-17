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
	std::tuple<int, bool, std::string> GetRule(const HandCharacteristic& handCharacteristic, const Fase& fase, int lastBidId) final override;
};

