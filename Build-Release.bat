@echo off
title Build Wan22ToolDesigner_WebView2 - Release
echo ============================================
echo 🧱 Building Wan22ToolDesigner_WebView2.sln in Release mode...
echo ============================================

REM --- Đường dẫn Solution ---
set SOLUTION=Wan22ToolDesigner_WebView2.sln

REM --- Bước 1: Clean ---
dotnet clean "%SOLUTION%" -c Release
if %errorlevel% neq 0 (
    echo ❌ Clean failed!
    pause
    exit /b %errorlevel%
)

REM --- Bước 2: Build ---
dotnet build "%SOLUTION%" -c Release
if %errorlevel% neq 0 (
    echo ❌ Build failed!
    pause
    exit /b %errorlevel%
)
echo.
echo ============================================
echo Build - Publish completed successfully!
echo Output folder: bin\Release\net6.0-windows
echo ============================================

pause
