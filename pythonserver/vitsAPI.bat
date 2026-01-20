@echo off
@chcp 65001 >nul 
cd C:\Users\Administrator\Downloads\GPT-SoVITS-v2pro-20250604 ::需要替换到哦VITS GPT根目录
.\runtime\python api.py -a 0.0.0.0 -d cpu -s "SoVITS_weights_v2/luotianyi_e16_s432.pth" -p 3712 -g "GPT_weights_v2/luotianyi-e50.ckpt" -dr "怎么还是这张图捏，能不能换一张呀.wav" -dt "怎么还是这张图捏，能不能换一张呀。" -dl zh
pause
