@echo off
setlocal

if /I "%~1"=="--version" (
    echo uvx 0.11.13
    exit /b 0
)

set "PROJECT_ROOT=%~dp0..\.."
set "SERVER_EXE=%PROJECT_ROOT%\.mcp-server\Scripts\mcp-for-unity.exe"

if not exist "%SERVER_EXE%" (
    echo MCP for Unity local server not found: "%SERVER_EXE%" 1>&2
    exit /b 1
)

set "ARGS=%*"
set "FORWARD_ARGS=%ARGS:*mcp-for-unity =%"
if "%FORWARD_ARGS%"=="%ARGS%" set "FORWARD_ARGS=%ARGS%"

"%SERVER_EXE%" %FORWARD_ARGS%
exit /b %ERRORLEVEL%
