#include "pch.h"
#include "../Engine/Rule.h"

TEST(EngineTest, TestHandConversion)
{
  HandCharacteristic handCharacteristic {"A,KJ54,AQJ76,853"};
  EXPECT_EQ(handCharacteristic.Spades, 1);
  EXPECT_EQ(handCharacteristic.Hearts, 4);
  EXPECT_EQ(handCharacteristic.Diamonds, 5);
  EXPECT_EQ(handCharacteristic.Clubs, 3);
  EXPECT_EQ(handCharacteristic.distribution, "5431");
  EXPECT_EQ(handCharacteristic.Controls, 5);
  EXPECT_EQ(handCharacteristic.isBalanced, false);
}

TEST(EngineTest, TestIsBalanced)
{
  EXPECT_EQ(HandCharacteristic{"A,KJ54,AQJ76,853"}.isBalanced, false);
  EXPECT_EQ(HandCharacteristic{"A432,A43,A432,A3"}.isBalanced, true);
  EXPECT_EQ(HandCharacteristic{"A432,A432,A43,A3"}.isBalanced, true);
  EXPECT_EQ(HandCharacteristic{",,AKQJT98,AKQJT9"}.isBalanced, false);
}

TEST(EngineTest, TestIsReverse)
{
  EXPECT_EQ(HandCharacteristic{"A,KJ54,AQJ76,853"}.isReverse, true);
  EXPECT_EQ(HandCharacteristic{"A,AQJ76,KJ54,853"}.isReverse, false);
  EXPECT_EQ(HandCharacteristic{"A,AQJ76,KJ6543,3"}.isReverse, false);
  EXPECT_EQ(HandCharacteristic{"A432,A432,432,32"}.isReverse, false);  
}

TEST(EngineTest, TestShortage)
{
  // Two suiters
  EXPECT_EQ(HandCharacteristic{"A,KJ54,AQJ76,853"}.shortage, Shortage::HighTwo);
  EXPECT_EQ(HandCharacteristic{"AQJ76,KJ54,A,853"}.shortage, Shortage::HighTwo);
  EXPECT_EQ(HandCharacteristic{"853,A,KJ54,AQJ76"}.shortage, Shortage::LowTwo);
  EXPECT_EQ(HandCharacteristic{"85,A2,KJ54,AQJ76"}.shortage, Shortage::EqualTwo);

  // one suiters
  EXPECT_EQ(HandCharacteristic{"853,A3,KJ4,AQJ76"}.shortage, Shortage::MiddleOne);
  EXPECT_EQ(HandCharacteristic{"853,KJ4,AQJ76,A2"}.shortage, Shortage::LowOne);
  EXPECT_EQ(HandCharacteristic{"AQJ76,A2,KJ4,853"}.shortage, Shortage::HighOne);

  // 6332 7222
  EXPECT_EQ(HandCharacteristic{"AQJ765,KJ4,A2,85"}.shortage, Shortage::EqualHighOne);
  EXPECT_EQ(HandCharacteristic{"AQJ765,A2,KJ4,85"}.shortage, Shortage::EqualMiddleOne);
  EXPECT_EQ(HandCharacteristic{"AQJ765,A2,85,KJ4"}.shortage, Shortage::EqualLowOne);
  EXPECT_EQ(HandCharacteristic{"AQJ7654,A2,KJ,85"}.shortage, Shortage::EqualOne);

  // Three suiter
  EXPECT_EQ(HandCharacteristic{"AQJ5,A542,KJ64,5"}.shortage, Shortage::Unknown);
}

TEST(EngineTest, TestIsThreeSuiter)
{
  EXPECT_EQ(HandCharacteristic{"A,KJ54,AQJ7,8653"}.isThreeSuiter, true);
  EXPECT_EQ(HandCharacteristic{"AQJ6,KJ54,A,8653"}.isThreeSuiter, true);

  EXPECT_EQ(HandCharacteristic{"A,KJ54,AQJ76,853"}.isThreeSuiter, false);
}

TEST(EngineTest, TestHcp)
{
   EXPECT_EQ(HandCharacteristic{"A,KJ54,AQJ7,8653"}.Hcp, 15);
   EXPECT_EQ(HandCharacteristic{"AKQ,AKQ,AKQ,AKQJ"}.Hcp, 37);
   EXPECT_EQ(HandCharacteristic{"32,432,5432,5432"}.Hcp, 0);
   EXPECT_EQ(HandCharacteristic{"A2,432,K432,5432"}.Hcp, 7);  
}

TEST(EngineTest, TestControls)
{
    EXPECT_EQ(HandCharacteristic{"A,KJ54,AQJ7,8653"}.ControlsSuit, std::vector<int>({1, 2, 0, 2}));
    EXPECT_EQ(HandCharacteristic{"A,AKJ4,AQJ76,853"}.ControlsSuit, std::vector<int>({2, 3, 0, 2}));
    EXPECT_EQ(HandCharacteristic{"432,432,432,5432"}.ControlsSuit, std::vector<int>({0, 0, 0, 0}));
    EXPECT_EQ(HandCharacteristic{"AKQ,AKQ,AKQ,AKQJ"}.ControlsSuit, std::vector<int>({3, 3, 3, 3}));
}

TEST(EngineTest, TestQueens)
{
    EXPECT_EQ(HandCharacteristic{"A,KJ54,AQJ7,8653"}.QueensSuit, std::vector<bool>({false, true, false, false}));
    EXPECT_EQ(HandCharacteristic{"A,AKJ4,AQJ76,Q53"}.QueensSuit, std::vector<bool>({true, false, true, false}));
    EXPECT_EQ(HandCharacteristic{"432,432,432,5432"}.QueensSuit, std::vector<bool>({false, false, false, false}));
    EXPECT_EQ(HandCharacteristic{"AKQ,AKQ,AKQ,AKQJ"}.QueensSuit, std::vector<bool>({true, true, true, true}));
}