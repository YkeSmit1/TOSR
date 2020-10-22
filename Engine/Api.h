#pragma once

enum class Fase
{
    UnKnown,
    Shape,
    Controls,
    ScanningControls,
    ScanningOther,
    End,
    Pull3NTNoAsk,
    Pull3NTOneAsk,
    Pull3NTTwoAsks,
    Pull4DiamondsNoAsk,
    Pull4DiamondsOneAsk,
    BidGame,
};

extern "C" __declspec(dllexport) int GetBidFromRule(Fase fase, Fase previousFase, const char* hand, int lastBidId, Fase* newFase, int* zoomOffset);
extern "C" __declspec(dllexport) int GetBidFromRuleEx(Fase fase, Fase previousFase, const char* hand, int lastBidId, Fase * newFase, char* description);
extern "C" __declspec(dllexport) int Setup(const char* database);

extern "C" __declspec(dllexport) void GetBid(int bidId, int& rank, int& suit);
