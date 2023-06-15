using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SAMViewer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        // 图像文件路径
        private string mImagePath =string.Empty;
        // 标注列表
        private List<FrameworkElement> annotations = new List<FrameworkElement>();
        float[] mImg;
        InferenceSession mEncoder;
        InferenceSession mDecoder;
        float[] mImgEmbedding;
        BitmapImage mMask;

        int mOrgwid;
        int mOrghei;

        bool mReady = false;

        Dispatcher UI;

        // 构造函数
        public MainWindow()
        {
            InitializeComponent();
            this.image.Width =  0.7f*this.Width;
            this.image.Height = this.Height;

            UI = Dispatcher.CurrentDispatcher;
            mMask = new BitmapImage();

        }
        /// <summary>
        /// 加载图像
        /// </summary>
        void LoadImage(string imgpath)
        {
            BitmapImage bitmap = new BitmapImage(new Uri(imgpath));
            this.mOrgwid = (int)bitmap.Width;
            this.mOrghei = (int)bitmap.Height;
            this.image.Source = bitmap;//显示图像
        }
        /// <summary>
        /// 加载模型
        /// </summary>
        void LoadOnnxModel()
        {
            string encode_model_path = @"D:\SAM\encoder-quant.onnx";
            this.mEncoder = new InferenceSession(encode_model_path);

            string decode_model_path = @"D:\SAM\decoder-quant.onnx";
            this.mDecoder = new InferenceSession(decode_model_path);

            string path = System.IO.Directory.GetCurrentDirectory();
        }
        /// <summary>
        /// 对图像进行编码
        /// </summary>
        void ImageEncode()
        {
            Transforms tranform = new Transforms(1024);
            this.mImg = tranform.ApplyImage(mImagePath, this.mOrgwid, this.mOrghei);

            var tensor = new DenseTensor<float>(this.mImg, new[] { 1, 3, 1024, 1024 });
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("x", tensor)
            };

            var results = this.mEncoder.Run(inputs);
            this.mImgEmbedding = results.First().AsTensor<float>().ToArray();
        }
        /// <summary>
        /// 提示信息解码
        /// </summary>
        void Decode(List<PromotionBase> promotions)
        {
            if (this.mReady == false)
                return;
            
            var embedding_tensor = new DenseTensor<float>(this.mImgEmbedding, new[] { 1, 256, 64, 64 });

            var bpmos = promotions.FindAll(e => e.mType == PromotionType.Box);
            var pproms = promotions.FindAll(e => e.mType == PromotionType.Point);
            int boxCount = promotions.FindAll(e=>e.mType == PromotionType.Box).Count();
            int pointCount = promotions.FindAll(e => e.mType == PromotionType.Point).Count();
            float[] promotion = new float[2*(boxCount * 2 + pointCount)];
            float[] label = new float[boxCount * 2 + pointCount];
            for (int i = 0; i < boxCount; i++)
            {
                var p = bpmos[i].GetDenseTensor();
                for (int j =0;j< p.Count();j++)
                {
                    promotion[4 * i + j] = p[j];
                }

                label[2 * i] = 2;
                label[2 * i+1] = 3;
            }
            for (int i = 0; i < pointCount; i++)
            {
                var p = pproms[i].GetDenseTensor();
                for (int j = 0; j < p.Count(); j++)
                {
                    promotion[boxCount*4+2 * i + j] = p[j];
                }

                label[boxCount * 2+ i] =1;
            }
         
            var point_coords_tensor = new DenseTensor<float>(promotion, new[] { 1, boxCount * 2 + pointCount, 2 });
          
            var point_label_tensor = new DenseTensor<float>(label, new[] { 1, boxCount * 2 + pointCount });

            float[] mask = new float[256 * 256];
            for (int i = 0; i < mask.Count(); i++)
            {
                mask[i] = 0;
            }
            var mask_tensor = new DenseTensor<float>(mask, new[] { 1, 1, 256, 256 });

            float[] hasMaskValues = new float[1] { 0 };
            var hasMaskValues_tensor = new DenseTensor<float>(hasMaskValues, new[] { 1 });

            float[] orig_im_size_values = { (float)this.mOrghei, (float)this.mOrgwid };
            var orig_im_size_values_tensor = new DenseTensor<float>(orig_im_size_values, new[] { 2 });

            var decode_inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("image_embeddings", embedding_tensor),
                NamedOnnxValue.CreateFromTensor("point_coords", point_coords_tensor),
                NamedOnnxValue.CreateFromTensor("point_labels", point_label_tensor),
                NamedOnnxValue.CreateFromTensor("mask_input", mask_tensor),
                NamedOnnxValue.CreateFromTensor("has_mask_input", hasMaskValues_tensor),
                NamedOnnxValue.CreateFromTensor("orig_im_size", orig_im_size_values_tensor)
            };

            var mask1 = this.mDecoder.Run(decode_inputs);


            var outputmask = mask1.First().AsTensor<float>().ToArray();
            int width = mOrgwid;
            int height = mOrghei;

            //// Create a new bitmap with the desired size
            System.Drawing.Bitmap bitmapout = new System.Drawing.Bitmap(width, height);
            //加载原始图像
            System.Drawing.Image originalImage = System.Drawing.Image.FromFile(this.mImagePath);

            //创建新的Bitmap对象，并设置大小
            System.Drawing.Bitmap resizedImage = new System.Drawing.Bitmap(width, height);

            //创建Graphics对象，并设置插值模式
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(resizedImage);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            //将原始图像绘制到新的Bitmap对象中
            g.DrawImage(originalImage, new System.Drawing.Rectangle(0, 0, width, height), new System.Drawing.Rectangle(0, 0, originalImage.Width, originalImage.Height), System.Drawing.GraphicsUnit.Pixel);
            //// Set the pixel values in the bitmap
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int ind = y * width + x;
                    if (outputmask[ind] > 0)
                    {
                        System.Drawing.Color color = System.Drawing.Color.FromArgb(255, 255, 0, 0);
                        bitmapout.SetPixel(x, y, color);
                    }
                    else
                    {
                        bitmapout.SetPixel(x, y, resizedImage.GetPixel(x,y));
                    }
                }
            }
           // BitmapImage bitmapImage = new BitmapImage();
            // 将Bitmap转换为BitmapImage
           
            UI.Invoke(new Action(delegate
            {
                mMask = new BitmapImage();
                using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
                {
                    bitmapout.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                    stream.Seek(0, System.IO.SeekOrigin.Begin);
                    mMask.BeginInit();
                    mMask.CacheOption = BitmapCacheOption.OnLoad;
                    mMask.StreamSource = stream;
                    mMask.EndInit();
                }
                this.image.Source = mMask;
            }));
        }
        Point TranslateOrgImgPoint(Point clickPoint)
        {
            double imageWidth = image.ActualWidth;
            double imageHeight = image.ActualHeight;
            double scaleX = imageWidth / this.mOrgwid;
            double scaleY = imageHeight / this.mOrghei;
            double offsetX = (imageWidth - scaleX * this.mOrgwid) / 2;
            double offsetY = (imageHeight - scaleY * this.mOrghei) / 2;
            double imageX = (clickPoint.X - offsetX) / scaleX;
            double imageY = (clickPoint.Y - offsetY) / scaleY;
            Point p = new Point();
            p.X = (int)imageX;
            p.Y = (int)imageY;

            return p;
        }
        void ClearAnation()
        {
            List<UserControl> todel = new List<UserControl>();
            foreach (var v in this.ImgCanvas.Children)
            {
                if (v is PointAnnotation || v is RectAnnotation)
                    todel.Add(v as UserControl);
            }

            todel.ForEach(e=> { this.ImgCanvas.Children.Remove(e); });
        }
        // 鼠标左键按下事件处理程序
        private void image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 如果当前没有选中的标注，创建一个点标注
            this.image.CaptureMouse();
            if (RPoint.IsChecked == true)
            {
                Point canvasP = e.GetPosition(this.ImgCanvas);
                PointAnnotation annotation = new PointAnnotation();
                annotations.Add(annotation);
                this.ClearAnation();
                this.ImgCanvas.Children.Add(annotation);
                annotation.Position = canvasP;

                Point clickPoint = e.GetPosition(image);
                Point orgImgPoint = this.TranslateOrgImgPoint(clickPoint);

                PromotionBase promt = new PointPromotion();
                (promt as PointPromotion).X = (int)orgImgPoint.X;
                (promt as PointPromotion).Y = (int)orgImgPoint.Y;

                Transforms ts = new Transforms(1024);
                PointPromotion ptn = ts.ApplyCoords((promt as PointPromotion), this.mOrgwid, this.mOrghei);
                Thread thread = new Thread(() =>
                {
                    this.Decode(new List<PromotionBase>() { ptn });                  
                });
                thread.Start();
            }
            else if(RBox.IsChecked == true)
            {
                _startPoint = e.GetPosition(this.ImgCanvas);
                _currentAnnotation = new RectAnnotation
                {
                    Width = 0,
                    Height = 0,
                    Position = _startPoint
                };
                this.ClearAnation();
                this.ImgCanvas.Children.Add(_currentAnnotation);

                Point clickPoint = e.GetPosition(image);
                Point orgImgPoint = this.TranslateOrgImgPoint(clickPoint);
                _currentAnnotation.LeftUP = orgImgPoint;
            }     
        }

        // 鼠标移动事件处理程序
        private void image_MouseMove(object sender, MouseEventArgs e)
        {
            // 如果当前有选中的标注，处理拖动和调整大小操作
            if (e.LeftButton == MouseButtonState.Pressed && _currentAnnotation != null)
            {
                var currentPoint = e.GetPosition(this.ImgCanvas);
                var width = Math.Abs(currentPoint.X - _startPoint.X);
                var height = Math.Abs(currentPoint.Y - _startPoint.Y);
                _currentAnnotation.Width = width;
                _currentAnnotation.Height = height;
                Canvas.SetLeft(_currentAnnotation, Math.Min(_startPoint.X, currentPoint.X));
                Canvas.SetTop(_currentAnnotation, Math.Min(_startPoint.Y, currentPoint.Y));         
            }
        }
        private void image_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_currentAnnotation == null)
                return;
           
            this.image.ReleaseMouseCapture();
            Point clickPoint = e.GetPosition(image);
            Point orgImgPoint = this.TranslateOrgImgPoint(clickPoint);
            _currentAnnotation.RightBottom = orgImgPoint;

            BoxPromotion promt = new BoxPromotion();
            (promt as BoxPromotion).mLeftUp.X = (int)_currentAnnotation.LeftUP.X;
            (promt as BoxPromotion).mLeftUp.Y = (int)_currentAnnotation.LeftUP.Y;

            (promt as BoxPromotion).mRightBottom.X = (int)_currentAnnotation.RightBottom.X;
            (promt as BoxPromotion).mRightBottom.Y = (int)_currentAnnotation.RightBottom.Y;

            Transforms ts = new Transforms(1024);
            var pb = ts.ApplyBox(promt, this.mOrgwid, this.mOrghei);
           
            Thread thread = new Thread(() =>
            {
                this.Decode(new List<PromotionBase>() { pb });
               
            });
            thread.Start();
            _currentAnnotation = null;
        }
        /// <summary>
        /// 显示状态信息
        /// </summary>
        void ShowStatus(string message)
        {
            this.StatusTxt.Text = message;
        }
        /// <summary>
        /// 图像路径选择
        /// </summary>
        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension
            openFileDialog.DefaultExt = ".png";
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*";

            // Display OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = openFileDialog.ShowDialog();

            // Get the selected file name and display in a TextBox
            if (result == true)
            {
                this.ImgPathTxt.Text = openFileDialog.FileName;
                this.mImagePath = this.ImgPathTxt.Text;

                if (!File.Exists(this.mImagePath))
                    return;
            
                this.LoadImgGrid.Visibility = Visibility.Collapsed;
                this.ImgCanvas.Visibility = Visibility.Visible;
                this.LoadImage(this.mImagePath);
                this.ShowStatus("Image Loaded");

                Thread thread = new Thread(() =>
                {
                    this.LoadOnnxModel();//加载模型
                    UI.Invoke(new Action(delegate
                    {
                        this.ShowStatus("ONNX Model Loaded");
                    }));
                    this.ImageEncode();//Image Embedding

                    UI.Invoke(new Action(delegate
                    {
                        this.ShowStatus("Image Embedding Cal Finished");
                        this.mReady = true;
                    }));
                });
                thread.Start();
            }
        }    
        private RectAnnotation _currentAnnotation;
        private Point _startPoint;


    }

}
