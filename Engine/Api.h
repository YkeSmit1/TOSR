#pragma once

extern "C" __declspec(dllexport) int GetBidFromRule(int faseId, const char* hand, int lastBidId);
extern "C" __declspec(dllexport) void GetBid(int bidId, int& rank, int& suit);
