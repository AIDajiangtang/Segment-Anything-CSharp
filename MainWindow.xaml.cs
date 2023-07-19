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
using System.Windows.Media;
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
        private string mImagePath = string.Empty;
        List<Promotion> mPromotionList = new List<Promotion>();
        float[] mImg;
        InferenceSession mEncoder;
        InferenceSession mDecoder;
        float[] mImgEmbedding;
        private RectAnnotation mCurRectAnno;
        private Point _startPoint;
        int mOrgwid;
        int mOrghei;

        bool mReady = false;

        Dispatcher UI;

        // 构造函数
        public MainWindow()
        {
            InitializeComponent();
            this.mImage.Width = 0.7f * this.Width;
            this.mImage.Height = this.Height;

            this.mMask.Width = 0.7f * this.Width;
            this.mMask.Height = this.Height;

            this.UI = Dispatcher.CurrentDispatcher;

        }

        /// <summary>
        /// 加载图像
        /// </summary>
        void LoadImage(string imgpath)
        {
            BitmapImage bitmap = new BitmapImage(new Uri(imgpath));
            this.mOrgwid = (int)bitmap.Width;
            this.mOrghei = (int)bitmap.Height;
            this.mImage.Source = bitmap;//显示图像
        }

        /// <summary>
        /// 加载模型
        /// </summary>
        void LoadOnnxModel()
        {
            string exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            
            string encode_model_path = exePath+ @"\encoder-quant.onnx";
            this.mEncoder = new InferenceSession(encode_model_path);

            string decode_model_path = exePath + @"\decoder-quant.onnx";
            this.mDecoder = new InferenceSession(decode_model_path);
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
        void Decode(List<Promotion> promotions)
        {
            if (this.mReady == false)
                return;

            var embedding_tensor = new DenseTensor<float>(this.mImgEmbedding, new[] { 1, 256, 64, 64 });

            var bpmos = promotions.FindAll(e => e.mType == PromotionType.Box);
            var pproms = promotions.FindAll(e => e.mType == PromotionType.Point);
            int boxCount = promotions.FindAll(e => e.mType == PromotionType.Box).Count();
            int pointCount = promotions.FindAll(e => e.mType == PromotionType.Point).Count();
            float[] promotion = new float[2 * (boxCount * 2 + pointCount)];
            float[] label = new float[boxCount * 2 + pointCount];
            for (int i = 0; i < boxCount; i++)
            {
                var input = bpmos[i].GetInput();
                for (int j = 0; j < input.Count(); j++)
                {
                    promotion[4 * i + j] = input[j];
                }
                var la = bpmos[i].GetLable();
                for (int j = 0; j < la.Count(); j++)
                {
                    label[2 * i+j] = la[j];
                }
            }
            for (int i = 0; i < pointCount; i++)
            {
                var p = pproms[i].GetInput();
                for (int j = 0; j < p.Count(); j++)
                {
                    promotion[boxCount * 4 + 2 * i + j] = p[j];
                }
                var la = pproms[i].GetLable();
                for (int j = 0; j < la.Count(); j++)
                {
                    label[boxCount * 2 + i+j] = la[j];
                }             
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
            System.Drawing.Bitmap bitmapout = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            
            ////加载原始图像
            //System.Drawing.Image originalImage = System.Drawing.Image.FromFile(this.mImagePath);

            ////创建新的Bitmap对象，并设置大小
            //System.Drawing.Bitmap resizedImage = new System.Drawing.Bitmap(width, height);

            ////创建Graphics对象，并设置插值模式
            //System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(resizedImage);
            //g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            ////将原始图像绘制到新的Bitmap对象中
            //g.DrawImage(originalImage, new System.Drawing.Rectangle(0, 0, width, height), new System.Drawing.Rectangle(0, 0, originalImage.Width, originalImage.Height), System.Drawing.GraphicsUnit.Pixel);
            //// Set the pixel values in the bitmap
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int ind = y * width + x;
                    if (outputmask[ind] > 0)
                    {
                        bitmapout.SetPixel(x, y, System.Drawing.Color.FromArgb(0, 255, 0, 0));
                    }
                    else
                    {
                        bitmapout.SetPixel(x, y, System.Drawing.Color.FromArgb(255, 255, 255, 255));
                    }
                }
            }
         

            UI.Invoke(new Action(delegate
            {
                //this.mMaskBitmap = new BitmapImage();

                //using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
                //{
                //    bitmapout.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                //    stream.Seek(0, System.IO.SeekOrigin.Begin);
                //    this.mMaskBitmap.BeginInit();
                //    this.mMaskBitmap.CacheOption = BitmapCacheOption.OnLoad;
                //    this.mMaskBitmap.StreamSource = stream;
                //    this.mMaskBitmap.EndInit();
                //}

                // BitmapImage bitmapImage = new BitmapImage();
                // 将Bitmap转换为BitmapImage

                WriteableBitmap bp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
                

                // 设置像素数据，将所有像素的透明度设置为半透明
                byte[] pixelData = new byte[width * height * 4];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int ind = y * width + x;
                        if (outputmask[ind] > 0)
                        {
                            pixelData[4*ind] = 0;  // Blue
                            pixelData[4 * ind + 1] = 0;  // Green
                            pixelData[4 * ind + 2] = 255;  // Red
                            pixelData[4 * ind + 3] = 255;  // Alpha
                        }
                        else
                        {
                            pixelData[4 * ind] = 0;  // Blue
                            pixelData[4 * ind + 1] = 0;  // Green
                            pixelData[4 * ind + 2] = 0;  // Red
                            pixelData[4 * ind + 3] = 0;  // Alpha
                        }
                    }
                }


                bp.WritePixels(new Int32Rect(0, 0, width, height), pixelData, width * 4, 0);

                // 创建一个BitmapImage对象，将WriteableBitmap作为源
                this.mMask.Source = bp;
            }));
        }
        Point TranslateOrgImgPoint(Point clickPoint)
        {
            double imageWidth = this.mImage.ActualWidth;
            double imageHeight = this.mImage.ActualHeight;
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

            todel.ForEach(e => { this.ImgCanvas.Children.Remove(e); });
        }
        // 鼠标左键按下事件处理程序
        private void image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {  
            // 如果当前没有选中的标注，创建一个点标注
            this.mImage.CaptureMouse();
            if (RPoint.IsChecked == true)
            {          
                Point clickPoint = e.GetPosition(this.mImage);
                Point orgImgPoint = this.TranslateOrgImgPoint(clickPoint);
                OpType type = this.RAdd.IsChecked == true ? OpType.ADD : OpType.REMOVE;
                SolidColorBrush brush = type == OpType.ADD ? Brushes.Red : Brushes.Black;

                PointAnnotation annotation = new PointAnnotation(brush);
                Point canvasP = e.GetPosition(this.ImgCanvas);
                annotation.Position = canvasP;
                this.ImgCanvas.Children.Add(annotation);

                Promotion promt = new PointPromotion(type);
                (promt as PointPromotion).X = (int)orgImgPoint.X;
                (promt as PointPromotion).Y = (int)orgImgPoint.Y;
             
              
                Transforms ts = new Transforms(1024);
                PointPromotion ptn = ts.ApplyCoords((promt as PointPromotion), this.mOrgwid, this.mOrghei);
                this.mPromotionList.Add(ptn);
                Thread thread = new Thread(() =>
                {
                    this.Decode(this.mPromotionList);
                });
                thread.Start();
            }
            else if (RBox.IsChecked == true)
            {
                _startPoint = e.GetPosition(this.ImgCanvas);
                this.mCurRectAnno = new RectAnnotation
                {
                    Width = 0,
                    Height = 0,
                    StartPosition = _startPoint
                };
                this.Reset();
                this.ImgCanvas.Children.Add(this.mCurRectAnno);

                Point clickPoint = e.GetPosition(this.mImage);
                Point orgImgPoint = this.TranslateOrgImgPoint(clickPoint);
                this.mCurRectAnno.LeftUP = orgImgPoint;
            }

        }

        // 鼠标移动事件处理程序
        private void image_MouseMove(object sender, MouseEventArgs e)
        {
            // 如果当前有选中的标注，处理拖动和调整大小操作
            if (e.LeftButton == MouseButtonState.Pressed && this.mCurRectAnno != null)
            {
                var currentPoint = e.GetPosition(this.ImgCanvas);
                var width = Math.Abs(currentPoint.X - _startPoint.X);
                var height = Math.Abs(currentPoint.Y - _startPoint.Y);
                this.mCurRectAnno.Width = width;
                this.mCurRectAnno.Height = height;
                Canvas.SetLeft(this.mCurRectAnno, Math.Min(_startPoint.X, currentPoint.X));
                Canvas.SetTop(this.mCurRectAnno, Math.Min(_startPoint.Y, currentPoint.Y));
            }
        }
        private void image_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.mImage.ReleaseMouseCapture();
            if (this.mCurRectAnno == null)
                return;
        
            Point clickPoint = e.GetPosition(this.mImage);
            Point orgImgPoint = this.TranslateOrgImgPoint(clickPoint);
            this.mCurRectAnno.RightBottom = orgImgPoint;

            BoxPromotion promt = new BoxPromotion();
            (promt as BoxPromotion).mLeftUp.X = (int)this.mCurRectAnno.LeftUP.X;
            (promt as BoxPromotion).mLeftUp.Y = (int)this.mCurRectAnno.LeftUP.Y;

            (promt as BoxPromotion).mRightBottom.X = (int)this.mCurRectAnno.RightBottom.X;
            (promt as BoxPromotion).mRightBottom.Y = (int)this.mCurRectAnno.RightBottom.Y;

            Transforms ts = new Transforms(1024);
            var pb = ts.ApplyBox(promt, this.mOrgwid, this.mOrghei);
            this.mPromotionList.Add(pb);
            Thread thread = new Thread(() =>
            {              
                this.Decode(this.mPromotionList);
            });
            thread.Start();
            this.mCurRectAnno = null;
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

        void Reset()
        {
            this.ClearAnation();
            this.mPromotionList.Clear();
            this.mMask.Source = null;
        }
        /// <summary>
        /// 复位
        /// </summary>
        private void BReset_Click(object sender, RoutedEventArgs e)
        {
            this.Reset();
        }
    }

}
