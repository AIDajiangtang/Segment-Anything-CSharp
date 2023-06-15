using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAMViewer
{
    public enum PromotionType
    {
        Point,
        Box
    }

    public abstract class PromotionBase
    {
        public abstract float[] GetDenseTensor();
        public PromotionType mType;
    }
    /// <summary>
    /// 提示点
    /// </summary>
    public class PointPromotion: PromotionBase
    {
        public PointPromotion()
        {
            this.mType = PromotionType.Point;
        }
        public int X { get; set; }
        public int Y { get; set; }
        public override float[] GetDenseTensor()
        {
            float[] pts = new float[2] { X ,Y};
            return pts;
        }
    }
    /// <summary>
    /// 提示框
    /// </summary>
    class BoxPromotion : PromotionBase
    {
        public BoxPromotion()
        {
            this.mLeftUp = new PointPromotion();
            this.mRightBottom = new PointPromotion();
            this.mType = PromotionType.Box;
        }
        public override float[] GetDenseTensor()
        {
            float[] pts = new float[4] { this.mLeftUp.X, this.mLeftUp.Y, this.mRightBottom.X, this.mRightBottom.Y };
            return pts;
        }
        public PointPromotion mLeftUp { get; set; }//左上角点
        public PointPromotion mRightBottom { get; set; }//左上角点

    }
    /// <summary>
    /// 提示蒙版
    /// </summary>
    class MaskPromotion
    {
        public MaskPromotion(int wid,int hei)
        {
            this.mWidth = wid;
            this.mHeight = hei;
            this.mMask = new float[this.mWidth,this.mHeight];
        }

        float[,] mMask { get; set; }//蒙版
        public int mWidth { get; set; }//长度
        public int mHeight { get; set; }//高度
    }
    /// <summary>
    /// 提示文本
    /// </summary>
    class TextPromotion
    {
        public string mText { get; set; }
    }
}
