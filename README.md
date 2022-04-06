# RhyLiveSDK_Unity
This SDK is for unity developers, contains basic functions like motion data receiving, smoothing.
## File Contents
[Not Necessary] Demo Scene + Demo Dependences  
[Core Files] SDK Scripts + SDK Dependences
## SDK Core Script Description
### Human Updater Daemon
Receive Motion Data From Capture Device and apply data to Updaters.
### Face Updater
Apply received data to 52 ARKit blendshapes.
### Pose Updater
Apply received Rotation data to unity HumanBodyBones, apply received Position data to unity HumanBodyBones.Hips 


**PS: For details, please see comments in code.**

**SDK use Unity 2020.3.18**

***
# RhyLiveSDK_Unity
此SDK为unity开发者制作，包含RhyLive驱动虚拟形象一切基础代码，包括：动作数据的接收，平滑等。
## SDK内容
[不必要的] Demo 场景 + Demo 依赖插件及模型  
[核心文件] SDK 脚本 + SDK 依赖库
## SDK 核心脚本描述
### Human Updater Daemon
从动捕端接收动作数据并且将数据同步给Updater脚本
### Face Updater
将接收到的面捕数据赋给52个ARKit的blendshapes
### Pose Updater
将接收到的骨骼旋转数据赋给unity Humanbodybones的每一块骨骼（不含lastBone）,同时接收hips的position数据（注，其实并没有接收position数据，但是预留了position数据位置）


**PS: 代码细节描述请看注释.**

**SDK使用Unity 2020.3.18制作**
