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

REM --- Bước 3: Publish ra bản chạy thực tế (self-contained) ---
echo.
echo 🚀 Publishing self-contained Windows build...
dotnet publish "%SOLUTION%" -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
if %errorlevel% neq 0 (
    echo ❌ Publish failed!
    pause
    exit /b %errorlevel%
)

echo.
echo ============================================
echo ✅ Build & Publish completed successfully!
echo Output folder:
echo bin\Release\net8.0-windows\win-x64\publish\
echo ============================================

pause
