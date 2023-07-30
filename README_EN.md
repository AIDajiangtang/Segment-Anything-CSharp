English | [简体中文](README.md)

# SAMTool
 ## SAM for CSharp ONNX Inference</h2>  
[[`Paper`](https://ai.facebook.com/research/publications/segment-anything/)] [[`Source Code`](https://github.com/facebookresearch/segment-anything/)]  


Based on C# language, ONNX format Segment Anything reasoning program.
Although the official pre-training model and inference code are provided, the pre-training model is only in pytorch format, and the inference code only provides Python code based on the Pytorch framework.
This project consists of two parts:
1. Split the officially released pre-training model into encoder and decoder, and save them in ONNX format.
2. Use C# language to load the model, perform inference, and use WPF for interaction and display.

## Source code compilation</h2>
  1. Download the source code to the local
  2. Install the Nuget package
  In Visual Studio, right-click on the project and select "Manage NuGet Packages".
  In the NuGet Package Manager window, select the Browse tab.
  Search for Microsoft.ML.OnnxRuntime, select version 1.15.1, and click Install
  Search for MathNet.Numerics, select version 5.0.0, and click Install
  MathNet.Numerics is used to calculate the mean and variance of images. It requires .Net Framework 4.6.1 or above, and it can also be implemented by itself, thereby removing this dependency.
  3. Put decoder-quant.onnx and encoder-quant.onnx in the exe path
  4. Run the program

Effect demo:

<img width="500" src="https://user-images.githubusercontent.com/18625471/256461679-0a357c01-3a7d-41cd-9a83-411fca9a8787.jpg">   
<img width="500" src="https://user-images.githubusercontent.com/18625471/256462253-302bc6fb-f18e-4abc-ae69-5eacc3968a34.jpg">  

Since Github does not support uploading files exceeding 25M, ONNX model files cannot be uploaded. If necessary, please pay attention to the following WeChat public account, and the background will reply [SAM]

Pay attention to the WeChat public account: 人工智能大讲堂
