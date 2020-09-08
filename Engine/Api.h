#pragma once

enum class Fase
{
    UnKnown,
    Shape,
    Controls,
    Scanning,
    End
};

extern "C" __declspec(dllexport) int GetBidFromRule(Fase fase, const char* hand, int lastBidId, Fase* newFase);
extern "C" __declspec(dllexport) int GetBidFromRuleEx(Fase fase, const char* hand, int lastBidId, Fase * newFase, char* description);
extern "C" __declspec(dllexport) int Setup(const char* database);

extern "C" __declspec(dllexport) void GetBid(int bidId, int& rank, int& suit);
