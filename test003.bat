@echo off
chcp 65001 > nul

setlocal enabledelayedexpansion

set "wait_time=3"

set "excluded_drives=C:"

echo "正在获取本地盘符..."
for /f "skip=1 tokens=1,2" %%a in ('wmic logicaldisk get deviceid^,drivetype') do (
    if "%%b"=="3" (
        set "flag=Y"
        for %%c in (%excluded_drives%) do (
            if /i "%%a"=="%%c" (
                echo "跳过盘符 %%a"
                set "flag=N"
            )
        )
        if "!flag!"=="Y" (
            echo "启用新进程扫描盘符 %%a"
            start /b cmd /c "script2.bat %%a !wait_time!"
        )
    )
)

:check_process
set "process_count=0"
for /f "skip=3 tokens=1" %%a in ('tasklist /fi "imagename eq cmd.exe" /fo csv') do (
    set /a "process_count+=1"
)
if %process_count% GTR 1 (
    timeout /t 1 /nobreak > nul
    goto check_process
)

echo "所有进程已经执行完成"
echo "开始读取脚本2的输出结果并写入 result.txt"


set "dir_name=%COMPUTERNAME%_!date:~5,2!!date:~8,2!!time:~0,2!!time:~3,2!!time:~6,2!"
echo "!dir_name!"
md "!dir_name!"

(for %%f in (*.txt) do (
    echo "盘符：%%~nf"
    type "%%~f"
    echo.
)) > "!dir_name!\result.txt"

echo "写入 result.txt 完成"
echo "删除临时文件..."
del *_temp.txt
pause