using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SAMViewer
{
    /// <summary>
    /// Image And Text Encoder
    /// </summary>
    class CLIP
    {

        InferenceSession mTxtEncoder;
        InferenceSession mImgEncoder;
        public static CLIP theSingleton = null;
        int contextLength = 77;
        protected CLIP()
        {
            Thread thread = new Thread(() =>
            {
                this.LoadONNXModel();
            });
            thread.Start();       
        }
        public static CLIP Instance()
        {
            if (null == theSingleton)
            {
                theSingleton = new CLIP();
            }
            return theSingleton;
        }

        /// <summary>
        /// 加载CLIP模型
        /// </summary>
        void LoadONNXModel()
        {
            if (this.mTxtEncoder != null)
                this.mTxtEncoder.Dispose();

            if (this.mImgEncoder != null)
                this.mImgEncoder.Dispose();

            string exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string txtencoder = exePath + @"\textual.onnx";
            if (!File.Exists(txtencoder))
            {
                MessageBox.Show(txtencoder + " not exist!");
                return;
            }
            this.mTxtEncoder = new InferenceSession(txtencoder);

            string imgencoder = exePath + @"\visual.onnx";
            if (!File.Exists(imgencoder))
            {
                MessageBox.Show(imgencoder + " not exist!");
                return;
            }
            this.mImgEncoder = new InferenceSession(imgencoder);
        }


        /// <summary>
        /// CLIP对文本进行编码
        /// </summary>
        public List<float> TxtEncoder(string txt)
        {
            List<Int64> token = SimpleTokenizer.Instance().tolikenlize(txt);
            var txt_tensor = new DenseTensor<Int64>(token.ToArray(), new[] { 1,contextLength });
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input", txt_tensor)
            };

            var results = this.mTxtEncoder.Run(inputs);
            var result = results.First().AsTensor<float>().ToArray();

            return result.ToList();
        }
        /// <summary>
        /// CLIP对图像进行编码
        /// </summary>
        public List<float> ImgEncoder(float[] img)
        {
            var tensor = new DenseTensor<float>(img, new[] { 1, 3, 224, 224 });
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input", tensor)
            };

            var results = this.mImgEncoder.Run(inputs);
            var result = results.First().AsTensor<float>().ToArray();

            return result.ToList();
        }
    }
}
