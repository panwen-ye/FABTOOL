@echo off
chcp 65001 > nul
setlocal enabledelayedexpansion

set "drive=%1"
set "wait_time=%2"

echo %date% %time% 开始扫描盘符 %drive%
timeout /t %wait_time% /nobreak > nul
echo %date% %time% 扫描盘符 %drive% 完成

set "output=%drive:~0,1%_temp.txt"
echo %drive% %wait_time% > %output%
