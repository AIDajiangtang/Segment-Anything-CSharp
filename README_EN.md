English | [简体中文](README.md)
segment anything (SAM) for C# Inference

Official website: https://segment-anything.com/

Official project source code: https://github.com/facebookresearch/segment-anything

Although the official pre-training model and inference code are provided, the pre-training model is only in pytorch format, and the inference code only provides Python code based on the Pytorch framework.

This project consists of two parts:

1. Split the officially released pre-training model into encoder and decoder, and save them in ONNX format.

2. Use C# language to load the model, perform inference, and use WPF for interaction and display.

Effect demo video:

https://weixin.qq.com/sph/A1KT5X

https://weixin.qq.com/sph/AJXH8U

Since Github does not support uploading files exceeding 25M, ONNX model files cannot be uploaded. If necessary, please pay attention to the following WeChat public account, and the background will reply [SAM]

Pay attention to the WeChat public account: 人工智能大讲堂
