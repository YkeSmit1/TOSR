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
template<typename chartype>
	static std::vector<std::basic_string<chartype>> Split(const std::basic_string<chartype>& str, const chartype& delimeter);
};

template<typename chartype>
std::vector<std::basic_string<chartype>> Utils::Split(const std::basic_string<chartype>& str, const chartype& delimeter)
{
	std::vector<std::basic_string<chartype>> subStrings;
	std::basic_stringstream<chartype> stringStream(str);
	std::basic_string<chartype> item;
	while (getline(stringStream, item, delimeter))
	{
		subStrings.push_back(move(item));
	}

	while (subStrings.size() < 4)
		subStrings.emplace_back("");

	return subStrings;

}
