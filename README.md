# TOSR
Program to test and learn the TOSR bidding system

The goal of the program will be two-fold:
1) Teaching new players how to become familiar with the TOSR system.
2) Get statistical analyses about the system. For example, how often will the relayer be the declarer.

TODO
- Create a rules database for the Tarzan system
- Implement more statistics. 
- Implement zooming
- Implenet CI/CD. Create integration test

BUGS
- 2Nd step in scanning is off by one.

How to build
- Clone or download the sources
- Use vcpkg to install SQLiteCpp and Sqlite-orm. Use "vcpkg integrate install" to use it in VS
- Make sure .Net Core 3.1 is installed
- Install nuget packages newtonsoft and xunit.

How to run
- Download cards.dll and copy it to the outputdirectory
- Copy Tosr.db3 to the outputdirectory
