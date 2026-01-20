@echo off
chcp 65001 >nul
call conda activate ltyai
cd /d "%~dp0"
C:\ProgramData\miniconda3\envs\ltyai\python.exe llmapi.py
pause