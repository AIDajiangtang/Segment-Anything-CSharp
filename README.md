简体中文 | [English](README_EN.md)

# segment anything（SAM）
[[`Paper`](https://ai.facebook.com/research/publications/segment-anything/)] [[`源码`](https://github.com/facebookresearch/segment-anything/)]  


# Ours：segment anything（SAM） for C# Inference  
虽然官方提供了预训练模型，和推理代码，但预训练模型只有pytorch格式的，且推理代码只提供了基于Pytorch框架的Python代码。  

本项目包含两部分：  
1.将官方发布的预训练模型，拆分成编码器和解码器，并分别保存为ONNX格式。  
2.使用C#语言加载模型，进行推理，并用WPF进行交互和显示。  

运行程序时需要将decoder-quant.onnx和encoder-quant.onnx放到exe路径下，.Net Framework4.7.2  
效果演示：   
<img width="500" src="https://user-images.githubusercontent.com/18625471/256461679-0a357c01-3a7d-41cd-9a83-411fca9a8787.jpg">   
<img width="500" src="https://user-images.githubusercontent.com/18625471/256462253-302bc6fb-f18e-4abc-ae69-5eacc3968a34.jpg">  



由于Github不支持上传超过25M的文件，所以ONNX模型文件不能上传，如有需要请关注下面微信公众号，后台回复【SAM】  

关注微信公众号：**人工智能大讲堂**    
<img width="180" src="https://user-images.githubusercontent.com/18625471/228743333-77abe467-2385-476d-86a2-e232c6482291.jpg">  
