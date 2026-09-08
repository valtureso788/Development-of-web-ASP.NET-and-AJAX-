@echo off
chcp 65001 >nul
title QuestPortal - Веб-приложение заявок (ASP.NET + AJAX)

echo ========================================================
echo    QuestPortal: Веб-приложение заявок и анкет
echo    ASP.NET Core 8.0 + AJAX
echo ========================================================
echo.

:: Поиск dotnet
set "DOTNET_CMD=dotnet"
where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    if exist "%USERPROFILE%\.dotnet\dotnet.exe" (
        set "DOTNET_CMD=%USERPROFILE%\.dotnet\dotnet.exe"
    ) else if exist "C:\Program Files\dotnet\dotnet.exe" (
        set "DOTNET_CMD=C:\Program Files\dotnet\dotnet.exe"
    ) else (
        echo [ОШИБКА] .NET SDK не найден!
        echo Пожалуйста, установите .NET 8 SDK.
        pause
        exit /b 1
    )
)

echo [1/2] Запуск сервера ASP.NET Core...
echo [2/2] Открытие браузера по адресу http://localhost:5000...
echo.

start "" "http://localhost:5000"

"%DOTNET_CMD%" run --urls "http://localhost:5000"

pause
