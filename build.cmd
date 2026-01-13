@echo off
REM MGAO Build Script (Windows CMD)
REM Prerequisites: .NET 6.0 SDK, Outlook installed

echo Building MGAO Solution...

echo Restoring NuGet packages...
dotnet restore src\MGAO.sln
if %ERRORLEVEL% NEQ 0 goto :error

echo Building solution...
dotnet build src\MGAO.sln --configuration Release --no-restore
if %ERRORLEVEL% NEQ 0 goto :error

echo.
echo Build successful!
echo Output: src\MGAO.UI\bin\Release\net6.0-windows\
goto :end

:error
echo Build failed!
exit /b 1

:end
