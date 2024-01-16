#pragma once
#include <vector>
#include <string>
#include <sstream>

#define DBOUT( s )            \
{                             \
   std::ostringstream os_;    \
   os_ << s;                   \
   OutputDebugStringA( os_.str().c_str() );  \
}

class Utils
{
public:
    template<typename CharType>
    static std::vector<std::basic_string<CharType>> Split(const std::basic_string<CharType>& str, const CharType& delimiter);
};

template<typename CharType>
std::vector<std::basic_string<CharType>> Utils::Split(const std::basic_string<CharType>& str, const CharType& delimiter)
{
    std::vector<std::basic_string<CharType>> subStrings;
    std::basic_stringstream<CharType> stringStream(str);
    std::basic_string<CharType> item;
    while (getline(stringStream, item, delimiter))
    {
        subStrings.push_back(move(item));
    }

    while (subStrings.size() < 4)
        subStrings.emplace_back("");

    return subStrings;

}
