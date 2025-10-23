@echo off
title 🧹 Clean bin & obj folders (Wan22ToolDesigner_WebView2)
echo ============================================
echo 🧹 Removing bin và obj...
echo ============================================

REM --- Xoá thư mục bin ---
for /d /r %%d in (bin) do (
    if exist "%%d" (
        echo Deleting %%d
        rmdir /s /q "%%d"
    )
)

REM --- Xoá thư mục obj ---
for /d /r %%d in (obj) do (
    if exist "%%d" (
        echo Deleting %%d
        rmdir /s /q "%%d"
    )
)

echo.
echo ============================================
echo OKKKKKKK!
echo ============================================

pause
