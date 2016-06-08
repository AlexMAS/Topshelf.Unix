call "%VS140COMNTOOLS%VsDevCmd.bat"
nuget.exe restore "..\Topshelf.Unix.sln"
msbuild "..\Topshelf.Unix.sln" /t:Clean /p:Configuration=Release 
msbuild "..\Topshelf.Unix.sln" /p:Configuration=Release 
nuget.exe pack "..\Topshelf.Unix\Topshelf.Unix.nuspec" -OutputDirectory "..\Topshelf.Unix\bin\Release" -symbols
