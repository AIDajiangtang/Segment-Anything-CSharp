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
  3.4搜索OpenCvSharp4,选择4.8.0版本，点击安装  
  3.5搜索OpenCvSharp4.runtime.win,选择4.8.0版本，点击安装  
  3.6SAMViewer目标平台设置x64
 4.将decoder-quant.onnx,encoder-quant.onnx,textual.onnx,visual.onnx放到exe路径下  
 5.运行程序

 效果演示：   
 点Promot：展开Point栏，点击AddPoint后，鼠标点击左侧图像选择点。Add Mask表示正向点，Remove Mask表示负向点。  
<img width="500" src="https://user-images.githubusercontent.com/18625471/259664489-59f7a9ee-6652-4191-ab07-513b51962f90.png">   
Box Promot: 展开Box栏，点击AddBox后，鼠标点击左侧图像选择起始点，易懂鼠标改变Box大小。  
<img width="500" src="https://user-images.githubusercontent.com/18625471/259664712-963e2da5-5d82-4b7f-9d44-99ef7ec516f7.png">  
AutoSeg:自动分割，展everythin栏，设置阈值，根据points_per_side值在图像上均匀选择候选点，每个点都作为promot，然后根据阈值对结果后处理。  
<img width="500" src="https://user-images.githubusercontent.com/18625471/259666809-f0da6bba-6f77-4715-a034-a848351d53d8.png">  
Text Promot:展开text栏，先自动分割，然后借助CLIP计算自动分割的crop图像和text的相似度。  
<img width="500" src="https://user-images.githubusercontent.com/18625471/259664515-502d4694-9b3c-4492-8a51-977ba6b372af.png">  

模型文件在Release中，或者关注下面微信公众号，后台回复【SAM】获取模型下载链接  

关注微信公众号：**人工智能大讲堂**    
<img width="180" src="https://user-images.githubusercontent.com/18625471/228743333-77abe467-2385-476d-86a2-e232c6482291.jpg">  


