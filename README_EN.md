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
1. Download the source code to the local<br />
2.Visual Studio opens the .sln project solution<br />
3. Install the Nuget package<br />
   3.1 In Visual Studio, right-click on the project and select "Manage NuGet Packages".<br />
   3.2 In the "NuGet Package Manager" window, select the "Browse" tab.<br />
   3.3 Search for Microsoft.ML.OnnxRuntime, select version 1.15.1, and click Install<br />
   3.4 Search for OpenCvSharp4, select version 4.8.0, and click Install<br />
   3.5 Search for OpenCvSharp4.runtime.win, select version 4.8.0, and click Install<br />
   3.6 the target platform of SAMViewer set x64<br />
  4. Put decoder-quant.onnx , encoder-quant.onnx ,visual.onnx and textual.onnx in the exe path<br />
  5. Run the program<br />

Effect demo:

<img width="500" src="https://user-images.githubusercontent.com/18625471/256461679-0a357c01-3a7d-41cd-9a83-411fca9a8787.jpg">   
<img width="500" src="https://user-images.githubusercontent.com/18625471/256462253-302bc6fb-f18e-4abc-ae69-5eacc3968a34.jpg">  

model file is upload to release, or  pay attention to the following WeChat public account, and the background will reply [SAM]get model file download link

Pay attention to the WeChat public account: 人工智能大讲堂
