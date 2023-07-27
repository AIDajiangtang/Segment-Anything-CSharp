English | [简体中文](README.md)

# SAMTool
segment anything (SAM) for C# Inference
[[`Paper`](https://ai.facebook.com/research/publications/segment-anything/)] [[`Source Code`](https://github.com/facebookresearch/segment-anything/)]  


Although the official pre-training model and inference code are provided, the pre-training model is only in pytorch format, and the inference code only provides Python code based on the Pytorch framework.

This project consists of two parts:

1. Split the officially released pre-training model into encoder and decoder, and save them in ONNX format.

2. Use C# language to load the model, perform inference, and use WPF for interaction and display.

put decoder-quant.onnx adn encoder-quant.onnx to the same path with exe,.Net Framework4.7.2

Effect demo:

<img width="500" src="https://user-images.githubusercontent.com/18625471/256461679-0a357c01-3a7d-41cd-9a83-411fca9a8787.jpg">   
<img width="500" src="https://user-images.githubusercontent.com/18625471/256462253-302bc6fb-f18e-4abc-ae69-5eacc3968a34.jpg">  

Since Github does not support uploading files exceeding 25M, ONNX model files cannot be uploaded. If necessary, please pay attention to the following WeChat public account, and the background will reply [SAM]

Pay attention to the WeChat public account: 人工智能大讲堂
