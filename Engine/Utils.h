#pragma once
#include <vector>
#include <string>
#include <sstream>

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
	if (str.empty())
	{
		return subStrings;
	}

	std::basic_stringstream<chartype> stringStream(str);
	std::basic_string<chartype> item;
	while (getline(stringStream, item, delimeter))
	{
		subStrings.push_back(move(item));
	}

	return subStrings;

}
