#pragma once

enum class Fase
{
    Shape,
    Controls,
    Scanning
};

extern "C" __declspec(dllexport) int GetBidFromRule(Fase fase, const char* hand, int lastBidId, Fase* newFase);
extern "C" __declspec(dllexport) void GetBid(int bidId, int& rank, int& suit);
