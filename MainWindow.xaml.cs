using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Linq;

namespace SAMViewer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        // 图像文件路径
        private string mImagePath = string.Empty;
        SAM mSam = SAM.Instance();
        CLIP mCLIP = CLIP.Instance();
        List<Promotion> mPromotionList = new List<Promotion>();
        float[] mImgEmbedding;
        private RectAnnotation mCurRectAnno;
        private Point _startPoint;
        int mOrgwid;
        int mOrghei;
        //undo and redo
        private Stack<Promotion> mUndoStack = new Stack<Promotion>();
        private Stack<Promotion> mRedoStack = new Stack<Promotion>();
        Dispatcher UI;
        SAMAutoMask mAutoMask;
        MaskData mAutoMaskData;
        Operation mCurOp;
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
        // 鼠标左键按下事件处理程序
        private void image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {  
            // 如果当前没有选中的标注，创建一个点标注
            this.mImage.CaptureMouse();
            if (this.mCurOp == Operation.Point)
            {

                OpType type = this.RAdd.IsChecked == true ? OpType.ADD : OpType.REMOVE;
                SolidColorBrush brush = type == OpType.ADD ? Brushes.Red : Brushes.Black;

                PointAnnotation annotation = new PointAnnotation(brush);
                Point canvasP = e.GetPosition(this.ImgCanvas);
                annotation.Position = canvasP;
                this.ImgCanvas.Children.Add(annotation);

               
                Promotion promt = new PointPromotion(type);
                Point clickPoint = e.GetPosition(this.mImage);
                Point orgImgPoint = this.Window2Image(clickPoint);
                (promt as PointPromotion).X = (int)orgImgPoint.X;
                (promt as PointPromotion).Y = (int)orgImgPoint.Y;
             
              
                Transforms ts = new Transforms(1024);
                PointPromotion ptn = ts.ApplyCoords((promt as PointPromotion), this.mOrgwid, this.mOrghei);
                ptn.mAnation = annotation;
                this.mUndoStack.Push(ptn);
                this.mPromotionList.Add(ptn);
                Thread thread = new Thread(() =>
                {
                    MaskData md = this.mSam.Decode(this.mPromotionList, this.mImgEmbedding, this.mOrgwid, this.mOrghei);
                    this.ShowMask(md.mMask.ToArray(), Color.FromArgb((byte)100, (byte)255, (byte)0, (byte)0));
                });
                thread.Start();
            }
            else if (this.mCurOp == Operation.Box)
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
                Point orgImgPoint = this.Window2Image(clickPoint);
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
            Point orgImgPoint = this.Window2Image(clickPoint);
            this.mCurRectAnno.RightBottom = orgImgPoint;

            BoxPromotion promt = new BoxPromotion();
            (promt as BoxPromotion).mLeftUp.X = (int)this.mCurRectAnno.LeftUP.X;
            (promt as BoxPromotion).mLeftUp.Y = (int)this.mCurRectAnno.LeftUP.Y;

            (promt as BoxPromotion).mRightBottom.X = (int)this.mCurRectAnno.RightBottom.X;
            (promt as BoxPromotion).mRightBottom.Y = (int)this.mCurRectAnno.RightBottom.Y;

            Transforms ts = new Transforms(1024);
            var pb = ts.ApplyBox(promt, this.mOrgwid, this.mOrghei);
            pb.mAnation = this.mCurRectAnno;
            this.mUndoStack.Push(pb);
            this.mPromotionList.Add(pb);
            Thread thread = new Thread(() =>
            {
                MaskData md = this.mSam.Decode(this.mPromotionList, this.mImgEmbedding,this.mOrgwid,this.mOrghei);
                this.ShowMask(md.mMask.ToArray(), Color.FromArgb((byte)100, (byte)255, (byte)0, (byte)0));
            });
            thread.Start();
            this.mCurRectAnno = null;
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
                    this.mSam.LoadONNXModel();//加载Segment Anything模型

                    UI.Invoke(new Action(delegate
                    {
                        this.ShowStatus("ONNX Model Loaded");
                    }));
                    // 读取图像
                    OpenCvSharp.Mat image = OpenCvSharp.Cv2.ImRead(this.mImagePath, OpenCvSharp.ImreadModes.Color);
                    this.mImgEmbedding = this.mSam.Encode(image, this.mOrgwid, this.mOrghei);//Image Embedding

                    this.mAutoMask = new SAMAutoMask();
                    this.mAutoMask.mImgEmbedding = this.mImgEmbedding;
                    this.mAutoMask.mSAM = this.mSam;
                    image.Dispose();
                    UI.Invoke(new Action(delegate
                    {
                        this.ShowStatus("Image Embedding Cal Finished");
                    }));
                });
                thread.Start();

            }
        }
        private void BReLoad_Click(object sender, RoutedEventArgs e)
        {
            this.Reset();
            this.LoadImgGrid.Visibility = Visibility.Visible;
            this.ImgCanvas.Visibility = Visibility.Hidden;
        }

        double CalculateCosineSimilarity(List<float> vector1, List<float> vector2)
        {
            double dotProduct = DotProduct(vector1, vector2);
            double magnitude1 = Magnitude(vector1);
            double magnitude2 = Magnitude(vector2);

            if (magnitude1 == 0 || magnitude2 == 0)
                return 0;

            return dotProduct / (magnitude1 * magnitude2);
        }

        double DotProduct(List<float> vector1, List<float> vector2)
        {          
            return vector1.Zip(vector2, (a, b) => a * b).Sum();
        }

        static double Magnitude(List<float> vector)
        {
            return Math.Sqrt(vector.Select(x => x * x).Sum());
        }
        /// <summary>
        /// 撤销
        /// </summary>
        private void BUndo_Click(object sender, RoutedEventArgs e)
        {
            if (this.mUndoStack.Count > 0)
            {
                Promotion p = this.mUndoStack.Pop();
                this.mRedoStack.Push(p);
                this.RemoveAnation(p);
                this.mPromotionList.Clear();
                this.mPromotionList.AddRange(this.mUndoStack.ToArray());
                
                Thread thread = new Thread(() =>
                {
                    MaskData md = this.mSam.Decode(this.mPromotionList, this.mImgEmbedding, this.mOrgwid, this.mOrghei);
                    this.ShowMask(md.mMask.ToArray(), Color.FromArgb((byte)100, (byte)255, (byte)0, (byte)0));
                });
                thread.Start();
            }
            else
            {
                MessageBox.Show("No Undo Promot");
            }
        }
        /// <summary>
        /// 重做
        /// </summary>
        private void BRedo_Click(object sender, RoutedEventArgs e)
        {
            if (this.mRedoStack.Count > 0)
            {
                Promotion pt = this.mRedoStack.Pop();
                this.mUndoStack.Push(pt);
                this.AddAnation(pt);
                this.mPromotionList.Clear();
                this.mPromotionList.AddRange(this.mUndoStack.ToArray());
                Thread thread = new Thread(() =>
                {
                    MaskData md = this.mSam.Decode(this.mPromotionList, this.mImgEmbedding, this.mOrgwid, this.mOrghei);
                    this.ShowMask(md.mMask.ToArray(), Color.FromArgb((byte)100, (byte)255, (byte)0, (byte)0));
                });
                thread.Start();
            }
            else
            {
                MessageBox.Show("No Redo Promot");
            }
        }
        /// <summary>
        /// 复位
        /// </summary>
        private void BReset_Click(object sender, RoutedEventArgs e)
        {
            this.Reset();
        }
        /// <summary>
        /// 显示分割结果
        /// </summary>
        void ShowMask(float[] mask, Color color)
        {

            UI.Invoke(new Action(delegate
            {
                WriteableBitmap bp = new WriteableBitmap(this.mOrgwid, this.mOrghei, 96, 96, PixelFormats.Pbgra32, null);
                // 设置像素数据，将所有像素的透明度设置为半透明
                byte[] pixelData = new byte[this.mOrgwid * this.mOrghei * 4];
                Array.Clear(pixelData, 0, pixelData.Length);
                for (int y = 0; y < this.mOrghei; y++)
                {
                    for (int x = 0; x < this.mOrgwid; x++)
                    {
                        int ind = y * this.mOrgwid + x;
                        if (mask[ind] > this.mSam.mask_threshold)
                        {
                            pixelData[4 * ind] = color.B;  // Blue
                            pixelData[4 * ind + 1] = color.G;  // Green
                            pixelData[4 * ind + 2] = color.R;  // Red
                            pixelData[4 * ind + 3] = 100;  // Alpha
                        }
                    }
                }

                bp.WritePixels(new Int32Rect(0, 0, this.mOrgwid, this.mOrghei), pixelData, this.mOrgwid * 4, 0);
                // 创建一个BitmapImage对象，将WriteableBitmap作为源
                this.mMask.Source = bp;
            }));
        }

        /// <summary>
        /// 显示分割结果
        /// </summary>
        void ShowMask(MaskData mask)
        {
            UI.Invoke(new Action(delegate
            {
                this.ShowStatus("Finish");
                this.ClearAnation();
                WriteableBitmap bp = new WriteableBitmap(this.mOrgwid, this.mOrghei, 96, 96, PixelFormats.Pbgra32, null);
                // 设置像素数据，将所有像素的透明度设置为半透明
                byte[] pixelData = new byte[this.mOrgwid * this.mOrghei * 4];
                Array.Clear(pixelData, 0, pixelData.Length);
                for (int i =0;i< mask.mShape[1];i++)
                {
                    Random random = new Random();
                    Color randomColor = Color.FromArgb((byte)100, (byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
                    for (int y = 0; y < this.mOrghei; y++)
                    {
                        for (int x = 0; x < this.mOrgwid; x++)
                        {
                            //int ind = i* this.mOrghei* this.mOrgwid+y * this.mOrgwid + x;
                            //int indpixel = y * this.mOrgwid + x;
                            //if (mask.mMask[ind] > this.mSam.mask_threshold)

                            int indpixel = y * this.mOrgwid + x;
                            if (mask.mfinalMask[i][indpixel] > this.mSam.mask_threshold)
                            {
                                pixelData[4 * indpixel] = randomColor.B;  // Blue
                                pixelData[4 * indpixel + 1] = randomColor.G;  // Green
                                pixelData[4 * indpixel + 2] = randomColor.R;  // Red
                                pixelData[4 * indpixel + 3] = 100;  // Alpha
                            }
                        }
                    }
                    Point leftup = this.Image2Window(new Point(mask.mBox[4 * i], mask.mBox[4 * i+1]));
                    Point rightdown = this.Image2Window(new Point(mask.mBox[4 * i+2], mask.mBox[4 * i +3]));
                    RectAnnotation box = new RectAnnotation();
                    this.ImgCanvas.Children.Add(box);
                    box.Width = rightdown.X - leftup.X;
                    box.Height = rightdown.Y - leftup.Y;
                    Canvas.SetLeft(box, leftup.X);
                    Canvas.SetTop(box, leftup.Y);

                  
                }
              
                bp.WritePixels(new Int32Rect(0, 0, this.mOrgwid, this.mOrghei), pixelData, this.mOrgwid * 4, 0);
                // 创建一个BitmapImage对象，将WriteableBitmap作为源
                this.mMask.Source = bp;
            }));
        }
        /// <summary>
        /// 窗口坐标转图像坐标
        /// </summary>
        Point Window2Image(Point clickPoint)
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
        Point Image2Window(Point image)
        {
            double imageWidth = this.mImage.ActualWidth;
            double imageHeight = this.mImage.ActualHeight;
            double scaleX = imageWidth / this.mOrgwid;
            double scaleY = imageHeight / this.mOrghei;
            double offsetX = (imageWidth - scaleX * this.mOrgwid) / 2;
            double offsetY = (imageHeight - scaleY * this.mOrghei) / 2;

            double windowsX = image.X * scaleX + offsetX;
            double windowsY = image.Y * scaleY + offsetX;

            Point p = new Point();
            p.X = (int)windowsX;
            p.Y = (int)windowsY;

            return p;
        }
        /// <summary>
        /// 清空
        /// </summary>
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
        /// <summary>
        /// 删除
        /// </summary>
        void RemoveAnation(Promotion pt)
        {
            if (this.ImgCanvas.Children.Contains(pt.mAnation))
                this.ImgCanvas.Children.Remove(pt.mAnation);
        }
        /// <summary>
        /// 添加
        /// </summary>
        void AddAnation(Promotion pt)
        {
            if (!this.ImgCanvas.Children.Contains(pt.mAnation))
                this.ImgCanvas.Children.Add(pt.mAnation);

        }
        /// <summary>
        /// 显示状态信息
        /// </summary>
        void ShowStatus(string message)
        {
            this.StatusTxt.Text = message;
        }   
        void Reset()
        {
            this.ClearAnation();
            this.mPromotionList.Clear();
            this.mMask.Source = null;
        }

        private void mAddPoint_Click(object sender, RoutedEventArgs e)
        {
            this.mCurOp = Operation.Point;
        }

        private void mAddBox_Click(object sender, RoutedEventArgs e)
        {
            this.mCurOp = Operation.Box;
        }

        private void mAutoSeg_Click(object sender, RoutedEventArgs e)
        {
            this.mAutoMask.points_per_side = int.Parse(this.mPoints_per_side.Text);
            this.mAutoMask.pred_iou_thresh = float.Parse(this.mPred_iou_thresh.Text);
            this.mAutoMask.stability_score_thresh = float.Parse(this.mStability_score_thresh.Text);
            this.ShowStatus("Auto Segment......");
            Thread thread = new Thread(() =>
            {
                this.mCurOp = Operation.Everything;               
                this.mAutoMaskData = this.mAutoMask.Generate(this.mImagePath);
                this.ShowMask(this.mAutoMaskData);
            });
            thread.Start();        
        }
        MaskData MatchTextAndImage(string txt)
        {
            var txtEmbedding = this.mCLIP.TxtEncoder(txt);
            OpenCvSharp.Mat image = new OpenCvSharp.Mat(this.mImagePath, OpenCvSharp.ImreadModes.Color);
            int maxindex = 0;
            double max = 0.0;
            MaskData final = new MaskData();
            for (int i = 0; i < this.mAutoMaskData.mShape[1]; i++)
            {
                // Define the coordinates of the ROI
                int x = this.mAutoMaskData.mBox[4 * i];  // Top-left x coordinate
                int y = this.mAutoMaskData.mBox[4 * i + 1];// Top-left y coordinate
                int width = this.mAutoMaskData.mBox[4 * i + 2] - this.mAutoMaskData.mBox[4 * i];  // Width of the ROI
                int height = this.mAutoMaskData.mBox[4 * i + 3] - this.mAutoMaskData.mBox[4 * i + 1];  // Height of the ROI

                // Create a Rect object for the ROI
                OpenCvSharp.Rect roiRect = new OpenCvSharp.Rect(x, y, width, height);
                // Extract the ROI from the image
                OpenCvSharp.Mat roi = new OpenCvSharp.Mat(image, roiRect);
                int neww = 0;
                int newh = 0;
                float scale = 224 * 1.0f / Math.Max(image.Rows, image.Cols);
                float newht = image.Rows * scale;
                float newwt = image.Cols * scale;

                neww = (int)(newwt + 0.5);
                newh = (int)(newht + 0.5);

                OpenCvSharp.Mat resizedImage = new OpenCvSharp.Mat();
                OpenCvSharp.Cv2.Resize(roi, resizedImage, new OpenCvSharp.Size(neww, newh));
                // 创建大的Mat
                OpenCvSharp.Mat largeMat = new OpenCvSharp.Mat(new OpenCvSharp.Size(224, 224), OpenCvSharp.MatType.CV_8UC3, OpenCvSharp.Scalar.Black);

                // 计算小的Mat放置的位置
                int xoffset = (largeMat.Width - resizedImage.Width) / 2;
                int yoffset = (largeMat.Height - resizedImage.Height) / 2;

                // 将小的Mat放置到大的Mat的中心位置
                resizedImage.CopyTo(largeMat[new OpenCvSharp.Rect(xoffset, yoffset, resizedImage.Width, resizedImage.Height)]);

                //将图像转换为浮点型
                OpenCvSharp.Mat floatImage = new OpenCvSharp.Mat();
                largeMat.ConvertTo(floatImage, OpenCvSharp.MatType.CV_32FC3);
                // 计算均值和标准差
                OpenCvSharp.Scalar mean = new OpenCvSharp.Scalar(0.48145466, 0.4578275, 0.40821073);
                OpenCvSharp.Scalar std = new OpenCvSharp.Scalar(0.26862954, 0.26130258, 0.27577711);
                // 归一化
                OpenCvSharp.Cv2.Normalize(floatImage, floatImage, 0, 255, OpenCvSharp.NormTypes.MinMax);
                OpenCvSharp.Cv2.Subtract(floatImage, mean, floatImage);
                OpenCvSharp.Cv2.Divide(floatImage, std, floatImage);

                float[] transformedImg = new float[3 * 224 * 224];
                for (int ii = 0; ii < 224; ii++)
                {
                    for (int j = 0; j < 224; j++)
                    {
                        int index = j * 224 + ii;
                        transformedImg[index] = floatImage.At<OpenCvSharp.Vec3f>(j, ii)[0];
                        transformedImg[224 * 224 + index] = floatImage.At<OpenCvSharp.Vec3f>(j, ii)[1];
                        transformedImg[2 * 224 * 224 + index] = floatImage.At<OpenCvSharp.Vec3f>(j, ii)[2];
                    }
                }

                var imgEmbedding = this.mCLIP.ImgEncoder(transformedImg);
                double maxs = CalculateCosineSimilarity(txtEmbedding.ToList(), imgEmbedding.ToList());
                if (maxs > max)
                {
                    maxindex = i;
                    max = maxs;
                }

                roi.Dispose();
                resizedImage.Dispose();
                largeMat.Dispose();
                floatImage.Dispose();
            }

            this.mAutoMaskData.mShape.CopyTo(final.mShape,0);
            final.mShape[1] = 1;
            final.mBox.AddRange(this.mAutoMaskData.mBox.GetRange(maxindex * 4, 4));
            final.mIoU.AddRange(this.mAutoMaskData.mIoU.GetRange(maxindex, 1));
            final.mStalibility.AddRange(this.mAutoMaskData.mStalibility.GetRange(maxindex, 1));
            //.GetRange(maxindex * final.mShape[2] * final.mShape[3], final.mShape[2] * final.mShape[3])
            final.mfinalMask.Add(this.mAutoMaskData.mfinalMask[maxindex]);



            image.Dispose();


            return final;
        }
        private void mText_Click(object sender, RoutedEventArgs e)
        {
            this.mCurOp = Operation.Text;
            this.ShowStatus("Image And Text Matching......");
            string txt = this.mTextinput.Text;
            Thread thread = new Thread(() =>
            {
                MaskData matches = this.MatchTextAndImage(txt);
                this.ShowMask(matches);
            });
            thread.Start();
        }

        private void Expanded(object sender, RoutedEventArgs e)
        {
            if (this.mPointexp == null || this.mBoxexp == null || this.mEverythingExp == null || this.mTextExp == null)
                return;

            Expander exp = sender as Expander;
            if (exp.IsExpanded == true)
            {
                this.mPointexp.IsExpanded = this.mPointexp == exp;
                this.mBoxexp.IsExpanded = this.mBoxexp == exp;
                this.mEverythingExp.IsExpanded = this.mEverythingExp == exp;
                this.mTextExp.IsExpanded = this.mTextExp == exp;                                                   
            }

        }
    }

    enum Operation
    {
        Point,
        Box,
        Everything,
        Text
    }

}