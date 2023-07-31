简体中文 | [English](README_EN.md)

# segment anything（SAM）
[[`Paper`](https://ai.facebook.com/research/publications/segment-anything/)] [[`源码`](https://github.com/facebookresearch/segment-anything/)]  


 ## SAM for CSharp ONNX Inference</h2>  
基于C#语言，ONNX格式Segment Anything推理程序。  
虽然官方提供了预训练模型，和推理代码，但预训练模型只有pytorch格式的，且推理代码只提供了基于Pytorch框架的Python代码。  
本项目包含两部分：  
1.将官方发布的预训练模型，拆分成编码器和解码器，并分别保存为ONNX格式。  
2.使用C#语言加载模型，进行推理，并用WPF进行交互和显示。  

 ## 源码编译</h2>  
 1.下载源码到本地  
 2.Visual Studio打开.sln项目解决方案
 3.安装Nuget包  
 
  3.1在Visual Studio中，鼠标右键单击项目并选择“管理NuGet程序包”。  
  3.2在“NuGet包管理器”窗口中，选择“浏览”选项卡。  
  3.3搜索Microsoft.ML.OnnxRuntime，选择1.15.1版本，点击安装  
  3.4搜索MathNet.Numerics，选择5.0.0版本，点击安装  
  3.5MathNet.Numerics用于计算图像均值和方差，要求.Net Framework4.6.1以上版本，也可以自己实现，进而去掉这个依赖  
 4.将decoder-quant.onnx和encoder-quant.onnx放到exe路径下  
 5.运行程序

 效果演示：   
<img width="500" src="https://user-images.githubusercontent.com/18625471/256461679-0a357c01-3a7d-41cd-9a83-411fca9a8787.jpg">   
<img width="500" src="https://user-images.githubusercontent.com/18625471/256462253-302bc6fb-f18e-4abc-ae69-5eacc3968a34.jpg">  

由于Github不支持上传超过25M的文件，所以ONNX模型文件不能上传，如有需要请关注下面微信公众号，后台回复【SAM】  

关注微信公众号：**人工智能大讲堂**    
<img width="180" src="https://user-images.githubusercontent.com/18625471/228743333-77abe467-2385-476d-86a2-e232c6482291.jpg">  
