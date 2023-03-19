@echo off
chcp 65001 > nul

setlocal enabledelayedexpansion

set "start_datetime=20230115000000"
set "end_datetime=20230318235959"

set "excluded_folders=C:\Windows;C:\Program Files;C:\Program Files (x86)"
set "excluded_drives=A:"

echo "正在扫描，请稍候..."
echo "开始时间：%start_time%"
echo "结束时间：%end_time%"
echo "免扫描目录：%excluded_folders%"
echo "免扫描盘符：%excluded_drives%"

set result=pass
echo "1"
for /f "tokens=1-6 delims=/: " %%a in ("%start_time%") do (
    set /a "yyyy=%%f,mm=%%c,dd=%%d,hh=%%e,nn=%%f,ss=%%h"
    setlocal enabledelayedexpansion
    set "start_datetime=!yyyy!%%mm!%%dd!%%hh!%%nn!%%ss!"
    endlocal
)



for /f "tokens=1-6 delims=/: " %%a in ("%end_time%") do (
    set /a "yyyy=%%f,mm=%%c,dd=%%d,hh=%%e,nn=%%f,ss=%%h"
    setlocal enabledelayedexpansion
    set "end_datetime=!yyyy!%%mm!%%dd!%%hh!%%nn!%%ss!"
    endlocal
)


(for /f "skip=1 tokens=1,2" %%a in ('wmic logicaldisk get deviceid^,drivetype') do (	
	pause
	if "%%b"=="3" (
		echo "当前盘符 %%a"
		
		echo "%%d"
		pause
		for %%c in (%excluded_drives%) do (
			if /i "%%a"=="%%c" (
				echo "跳过盘符 %%a"
				
			)
		)
		
		for /f "usebackq delims=" %%d in (`dir /s /b /a-d "%%a"\`) do (
			
		
		)
		
	)
)) > result.txt

pause
echo "扫描完成。"
echo "状态：%result%"
echo "结果已保存到 result.txt 文件"
