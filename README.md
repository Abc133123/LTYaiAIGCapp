# LTYaiAIGCapp
这个是由unity打造的AIGC交互 使用洛天依的VITS模型和fbx 

本项目为开源 社区ltyai.cn  （没BBS申请,加QQ群）
局域网服务器部署教程https://blog.csdn.net/m0_67479857/article/details/157141190

项目介绍 Unity AIGC app 

1.模型交互部分
核心
Script文件夹下
main下
ChatMain.cs //主调用 记得改百炼API 不然默认没功能 <br>
ChatLLMService.cs //启用服务 <br>
ChatManager.cs  //信息交互<br>
ChatUIController.cs //控制器<br>
LLMset.cs //保存设置<br>

动画 音乐 <br>
RandomAnimationPlayer <br>
RandomMusicPlayer <br>

2.场景切换 和UI 和其他 <br>
script文件夹下 <br>
Level //(关卡)切换场景 （如果2019版本灰一片 添加脚本在开始刷新一次Directional Light 自己解决或者问人） <br>
gdui  //（滚动ui）移动文本视图  <br>
about  //(关于) 版权法律信息提示 <br>
openui //打开第一次提示 <br>
SkipSplash //跳过unity动画 <br>
3.模型其他文件 <br>
3d //模型文件夹 <br>
mp3 //随机歌曲和指定文件夹 <br>
tb //图标和UI <br>
textMesh //unity自己的UI套装 啥也不是 <br>
Scenes //场景文件夹 <br>
<br>
本人技术有限只是想看见一个洛天依形象的ASI诞生 <br>
现在只是 做个AIGC小app <br>
当然 人工智能发展从 开始的机器学习 再到深度学习和神经网络，2017 Transformer  到现在的LLM 连AGI都很遥远 <br>
