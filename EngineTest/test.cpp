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
  EXPECT_EQ(HandCharacteristic{"AQJ765,A2,KJ4,85"}.shortage, Shortage::EqualOne);
  // Three suiter
  EXPECT_EQ(HandCharacteristic{"AQJ5,A542,KJ64,5"}.shortage, Shortage::Unknown);
}

TEST(EngineTest, TestIsThreeSuiter)
{
  EXPECT_EQ(HandCharacteristic{"A,KJ54,AQJ7,8653"}.isThreeSuiter, true);
  EXPECT_EQ(HandCharacteristic{"AQJ6,KJ54,A,8653"}.isThreeSuiter, true);

  EXPECT_EQ(HandCharacteristic{"A,KJ54,AQJ76,853"}.isThreeSuiter, false);
}