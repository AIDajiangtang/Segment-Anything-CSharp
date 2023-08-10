using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAMViewer
{
    /// <summary>
    /// A structure for storing masks and their related data in batched format.
    /// Implements basic filtering and concatenation.
    /// </summary>
    class MaskData
    {
        public int[] mShape;
        public List<float> mMask;
        public List<float> mIoU;
        public List<float> mStalibility;
        public List<int> mBox;

        public List<List<float>> mfinalMask;
        public MaskData()
        {
            this.mShape = new int[4];
            this.mMask = new List<float>();
            this.mIoU = new List<float>();
            this.mStalibility = new List<float>();
            this.mBox = new List<int>();

            this.mfinalMask = new List<List<float>>();
        }

        public void Filter(float pred_iou_thresh, float stability_score_thresh)
        {
            List<float> m = new List<float>();
            List<float> i = new List<float>();
            List<float> s = new List<float>();
            int batch = 0;
            for (int j = 0; j < this.mShape[1]; j++)
            {
                if (this.mIoU[j] >  pred_iou_thresh && this.mStalibility[j]> stability_score_thresh)
                {
                    this.mfinalMask.Add(this.mMask.GetRange(j * this.mShape[2] * this.mShape[3], this.mShape[2] * this.mShape[3]));
                    //m.AddRange(this.mMask.GetRange(j * this.mShape[2] * this.mShape[3], this.mShape[2] * this.mShape[3]));
                    i.Add(this.mIoU[j]);
                    s.Add(this.mStalibility[j]);
                    batch++;
                }              
            }
            this.mShape[1] = batch;
            this.mStalibility.Clear();
            this.mMask.Clear();
            this.mIoU.Clear();
            //this.mMask.AddRange(m);
            this.mIoU.AddRange(i);
            this.mStalibility.AddRange(s);
        }
       

        public float[] CalculateStabilityScore(float maskThreshold, float thresholdOffset)
        {
            int batchSize = this.mShape[1];
            int width = this.mShape[3];
            int height = this.mShape[2];

            float[] intersections = new float[batchSize];
            float[] unions = new float[batchSize];

            for (int i = 0; i < batchSize; i++)
            {
                float intersectionSum = 0;
                float unionSum = 0;

                for (int j = 0; j < width; j++)
                {
                    for (int k = 0; k < height; k++)
                    {
                        int index = i * width * height + k * width + j;
                        if (this.mMask[index] > maskThreshold + thresholdOffset)
                        {
                            intersectionSum++;
                        }
                        if (this.mMask[index] > maskThreshold - thresholdOffset)
                        {
                            unionSum++;
                        }
                    }
                }

                intersections[i] = intersectionSum;
                unions[i] = unionSum;
            }

            float[] stabilityScores = new float[batchSize];
            for (int i = 0; i < batchSize; i++)
            {
                stabilityScores[i] = intersections[i] / unions[i];
            }

            return stabilityScores;
        }

        public void Cat(MaskData md)
        {
            this.mShape[0] = md.mShape[0];
            this.mShape[1] += md.mShape[1];
            this.mShape[2] = md.mShape[2];
            this.mShape[3] = md.mShape[3];
            this.mBox.AddRange(md.mBox);
            this.mMask.AddRange(md.mMask);
            this.mStalibility.AddRange(md.mStalibility);
            this.mIoU.AddRange(md.mIoU);

            this.mfinalMask.AddRange(md.mfinalMask);
        }

        public int[] batched_mask_to_box()
        {
            int C = this.mShape[1];
            int width = this.mShape[3];
            int height = this.mShape[2];

            int[] boxes = new int[C*4];

            for (int c = 0; c < C; c++)
            {
                bool emptyMask = true;
                int top = height;
                int bottom = 0;
                int left = width;
                int right = 0;

                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        //int index = c * width * height + j * width + i;
                        //if (this.mMask[index] > 0)
                        int index =j * width + i;
                        if (this.mfinalMask[c][index] > 0)
                        {
                            emptyMask = false;
                            top = Math.Min(top, j);
                            bottom = Math.Max(bottom, j);
                            left = Math.Min(left, i);
                            right = Math.Max(right, i);
                        }
                    }
                }

                if (emptyMask)
                {
                    boxes[c*4]=0;
                    boxes[c * 4+1] = 0;
                    boxes[c * 4+2] = 0;
                    boxes[c * 4+3] = 0;
                }
                else
                {
                    boxes[c * 4] = left;
                    boxes[c * 4 + 1] = top;
                    boxes[c * 4 + 2] = right;
                    boxes[c * 4 + 3] = bottom;
                }
            }

            return boxes;
        }
    }
}
