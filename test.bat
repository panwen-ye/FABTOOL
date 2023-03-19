@echo off
chcp 65001 > nul

setlocal enabledelayedexpansion

set "start_datetime=20230315000000"
set "end_datetime=20230318235959"

set "excluded_folders=C:\Windows;C:\Program Files;C:\Program Files (x86)"
set "excluded_drives=C: D:"

echo "正在扫描，请稍候..."
echo "开始时间：%start_datetime%"
echo "结束时间：%end_datetime%"
echo "免扫描目录：%excluded_folders%"
echo "免扫描盘符：%excluded_drives%"

set "result=pass"
set "driver_dir=E:"


for /f "usebackq delims=" %%d in (`dir /s /b /a-d "%driver_dir%"\`) do (
	set "modified_datetime=%%~td%%~zd"
	set "modified_datetime=!modified_datetime:/=!"
	set "modified_datetime=!modified_datetime::=!"
	
	if !modified_datetime! geq !start_datetime! if !modified_datetime! leq !end_datetime! (
		echo %%~td %%~tt "%%d"
		echo %%~td %%~tt "%%d" >> result.txt
	)
)

pause


(for /f "skip=1 tokens=1,2" %%a in ('wmic logicaldisk get deviceid^,drivetype') do (	
	set "flag=Y"
	if "%%b"=="3" (
		echo %date% %time% 扫描盘符 %%a
		
		for %%c in (%excluded_drives%) do (
			if /i "%%a"=="%%c" (
				echo "跳过盘符 %%a"
				set "flag=N"
			)
		)
		
		if "!flag!"=="Y" (
			echo "%%a"
			for /f "usebackq delims=" %%d in (`dir /s /b /a-d "%%a"\`) do (
				set "modified_datetime=%%~td%%~zd"
				set "modified_datetime=!modified_datetime:/=!"
				set "modified_datetime=!modified_datetime::=!"
				
				if !modified_datetime! geq !start_datetime! if !modified_datetime! leq !end_datetime! (
					echo %%~td %%~tt "%%d"
					echo %%~td %%~tt "%%d" >> result.txt
				)
			)
		)		
		echo %date% %time% 扫描盘符 %%a 完成
	)
)) 

echo "扫描完成。"
echo "状态：%result%"
echo "结果已保存到 result.txt 文件"
