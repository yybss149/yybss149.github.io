@echo off
chcp 65001 >nul
cd /d "%~dp0"
start "知屿资源库管理器" /B python resource_manager.py
timeout /t 1 /nobreak >nul
start "" http://127.0.0.1:8001
echo.
echo 资源库管理器正在运行。关闭此窗口即可停止它。
pause >nul
