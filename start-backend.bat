@echo off
title EnglishGame Backend - localhost:5089
color 0A
echo =========================================
echo   EnglishGame Backend Server
echo   URL: http://localhost:5089
echo =========================================
echo.
echo Dang khoi dong backend...
echo Khong dong cua so nay khi dang lam viec!
echo.
cd /d "D:\EnglishGame\EnglishGame"
dotnet run --launch-profile http
echo.
echo Backend da dung. Nhan phim bat ky de thoat...
pause
