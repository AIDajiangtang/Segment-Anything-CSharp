using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SAMViewer
{
    // 点标注控件
    public class PointAnnotation : UserControl
    {
        private const int DefaultSize = 10;

        private readonly Shape _shape;
        private readonly TextBlock _textBlock;
        public SolidColorBrush m_Brush = Brushes.Red;
        public PointAnnotation(SolidColorBrush brush)
        {
            this.m_Brush = brush;
            _shape = new Ellipse
            {
                Width = DefaultSize,
                Height = DefaultSize,
                Fill = brush,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };

            _textBlock = new TextBlock
            {
                Text = "Point",
                Foreground = Brushes.Black,
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var grid = new Grid();
            grid.Children.Add(_shape);

            Content = grid;
        }
        public Point Position
        {
            get { return new Point(Canvas.GetLeft(this) + _shape.Width / 2, Canvas.GetTop(this) + _shape.Height / 2); }
            set
            {
                Canvas.SetLeft(this, value.X - _shape.Width / 2);
                Canvas.SetTop(this, value.Y - _shape.Height / 2);
            }
        }
        public string Text
        {
            get { return _textBlock.Text; }
            set { _textBlock.Text = value; }
        }

        //protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        //{
        //    base.OnMouseLeftButtonDown(e);
        //    CaptureMouse();
        //}

        //protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
        //{
        //    base.OnMouseLeftButtonUp(e);
        //    ReleaseMouseCapture();
        //}

        //protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        //{
        //    base.OnMouseMove(e);
        //    if (IsMouseCaptured)
        //    {
        //        var newPosition = e.GetPosition(Parent as UIElement);
        //        Canvas.SetLeft(this, newPosition.X - _shape.Width / 2);
        //        Canvas.SetTop(this, newPosition.Y - _shape.Height / 2);
        //    }
        //}
    }
    /// <summary>
    /// 四边形标注
    /// </summary>
    public class RectAnnotation : UserControl
    {
        private readonly Shape _shape;
        private readonly TextBlock _textBlock;

        public RectAnnotation()
        {
            _shape = new Rectangle
            {
                Width = 50,
                Height = 50,
                Fill = Brushes.Transparent,
                Stroke = Brushes.Blue,
                StrokeThickness = 1
            };

            _textBlock = new TextBlock
            {
                Text = "Rect",
                Foreground = Brushes.Blue,
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var grid = new Grid();
            grid.Children.Add(_shape);
            //grid.Children.Add(_textBlock);

            Content = grid;
        }

        public Point StartPosition
        {
            get { return new Point(Canvas.GetLeft(this) + _shape.Width / 2, Canvas.GetTop(this) + _shape.Height / 2); }
            set
            {
                Canvas.SetLeft(this, value.X - _shape.Width / 2);
                Canvas.SetTop(this, value.Y - _shape.Height / 2);
            }
        }

        public Point LeftUP { get; set; }
        public Point RightBottom { get; set; }

        public double Width
        {
            get { return _shape.Width; }
            set { _shape.Width = value; }
        }

        public double Height
        {
            get { return _shape.Height; }
            set { _shape.Height = value; }
        }

        public string Text
        {
            get { return _textBlock.Text; }
            set { _textBlock.Text = value; }
        }

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            //CaptureMouse();           
        }

        protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            //ReleaseMouseCapture();
        }

        //protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        //{
        //    base.OnMouseMove(e);
        //    if (IsMouseCaptured)
        //    {
        //        var newPosition = e.GetPosition(Parent as UIElement);
        //        Canvas.SetLeft(this, newPosition.X - _shape.Width / 2);
        //        Canvas.SetTop(this, newPosition.Y - _shape.Height / 2);
        //    }
        //}
    }
}
