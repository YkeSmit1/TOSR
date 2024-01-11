#pragma once

enum class Phase
{
    UnKnown,
    Shape,
    Controls,
    ScanningControls,
    ScanningOther,
    End,
    Pull3NTNoAsk,
    Pull3NTOneAskMin,
    Pull3NTOneAskMax,
    Pull3NTTwoAsks,
    Pull4DiamondsNoAsk,
    Pull4DiamondsOneAskMin,
    Pull4DiamondsOneAskMax,
    BidGame,
};

extern "C" __declspec(dllexport) int GetBidFromRule(Phase phase, Phase previousPhase, const char* hand, int lastBidId, Phase* newPhase, int* zoomOffset);
extern "C" __declspec(dllexport) int GetBidFromRuleEx(Phase phase, Phase previousPhase, const char* hand, int lastBidId, Phase * newPhase, char* description);
extern "C" __declspec(dllexport) int Setup(const char* database);

extern "C" __declspec(dllexport) void GetBid(int bidId, int& rank, int& suit);
