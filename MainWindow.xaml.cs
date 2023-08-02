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
            
        private RectAnnotation mCurRectAnno;
        private Point _startPoint;
        int mOrgwid;
        int mOrghei;
   
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
        /// 窗口坐标转图像坐标
        /// </summary>
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
                    float[] mask = this.mSam.Decode(this.mPromotionList, this.mOrgwid, this.mOrghei);
                    this.ShowMask(mask);
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
                float[] mask = this.mSam.Decode(this.mPromotionList,this.mOrgwid,this.mOrghei);
                this.ShowMask(mask);
            });
            thread.Start();
            this.mCurRectAnno = null;
        }
        /// <summary>
        /// 显示分割结果
        /// </summary>
        void ShowMask(float[] mask)
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
                        if (mask[ind] > 0)
                        {
                            pixelData[4 * ind] = 0;  // Blue
                            pixelData[4 * ind + 1] = 0;  // Green
                            pixelData[4 * ind + 2] = 255;  // Red
                            pixelData[4 * ind + 3] = 255;  // Alpha
                        }
                    }
                }

                bp.WritePixels(new Int32Rect(0, 0, this.mOrgwid, this.mOrghei), pixelData, this.mOrgwid * 4, 0);
                // 创建一个BitmapImage对象，将WriteableBitmap作为源
                this.mMask.Source = bp;
            }));
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
                    this.mSam.LoadONNXModel();//加载Segment Anything模型

                    UI.Invoke(new Action(delegate
                    {
                        this.ShowStatus("ONNX Model Loaded");
                    }));
                    this.mSam.Encode(this.mImagePath,this.mOrgwid,this.mOrghei);//Image Embedding

                    UI.Invoke(new Action(delegate
                    {
                        this.ShowStatus("Image Embedding Cal Finished");
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

        private void BReLoad_Click(object sender, RoutedEventArgs e)
        {
            this.Reset();
            this.LoadImgGrid.Visibility = Visibility.Visible;
            this.ImgCanvas.Visibility = Visibility.Hidden;
        }
    }

}