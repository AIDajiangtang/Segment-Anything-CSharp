using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SAMViewer
{
    /// <summary>
    /// 自动分割Everything
    /// </summary>
    class SAMAutoMask
    {
        public int points_per_side = 4;
        int points_per_batch = 64;
        public float pred_iou_thresh = 0.88f;
        public float stability_score_thresh = 0.95f;
        float stability_score_offset = 1.0f;
        float box_nms_thresh = 0.7f;
        int crop_n_layers = 0;
        float crop_nms_thresh = 0.7f;
        float crop_overlap_ratio = (float)512 / 1500;
        int crop_n_points_downscale_factor = 1;
        List<double[,]> point_grids = null;
        int min_mask_region_area = 0;
        string output_mode = "binary_mask";
        Mat mImage;
        public SAM mSAM;
        public float[] mImgEmbedding;

        public SAMAutoMask(int points_per_side = 4,
                            int points_per_batch = 64,
                            float pred_iou_thresh = 0.88f,
                            float stability_score_thresh = 0.95f,
                            float stability_score_offset = 1.0f,
                            float box_nms_thresh = 0.7f,
                            int crop_n_layers = 0,
                            float crop_nms_thresh = 0.7f,
                            float crop_overlap_ratio = (float)512 / 1500,
                            int crop_n_points_downscale_factor = 1,
                            List<double[,]> point_grids = null,
                            int min_mask_region_area = 0,
                            string output_mode = "binary_mask")
        {
            this.points_per_side = points_per_side;
            this.points_per_batch = points_per_batch;
            this.pred_iou_thresh = pred_iou_thresh;
            this.stability_score_thresh = stability_score_thresh;
            this.stability_score_offset = stability_score_offset;
            this.box_nms_thresh = box_nms_thresh;
            this.crop_n_layers = crop_n_layers;
            this.crop_nms_thresh = crop_nms_thresh;
            this.crop_overlap_ratio = crop_overlap_ratio;
            this.crop_n_points_downscale_factor = crop_n_points_downscale_factor;
            this.point_grids = point_grids;
            this.min_mask_region_area = min_mask_region_area;
            this.output_mode = output_mode;

            if ((points_per_side == 0) && (point_grids == null || point_grids.Count == 0))
            {
                MessageBox.Show("Exactly one of points_per_side or point_grid must be provided.");
                return;
            }

            if (points_per_side != 0)
            {
                this.point_grids = build_all_layer_point_grids(
                points_per_side,
                crop_n_layers,
                crop_n_points_downscale_factor);
            }
        }
        /// <summary>
        /// 创建网格
        /// </summary>
        /// <param name="n_per_side"></param>
        /// <param name="n_layers"></param>
        /// <param name="scale_per_layer"></param>
        /// <returns></returns>
        List<double[,]> build_all_layer_point_grids(int n_per_side, int n_layers, int scale_per_layer)
        {
            List<double[,]> points_by_layer = new List<double[,]>();
            for (int i = 0; i <= n_layers; i++)
            {
                int n_points = (int)(n_per_side / Math.Pow(scale_per_layer, i));
                points_by_layer.Add(BuildPointGrid(n_points));
            }
            return points_by_layer;
        }

        double[,] BuildPointGrid(int n_per_side)
        {
            double offset = 1.0 / (2 * n_per_side);
            double[] points_one_side = Enumerable.Range(0, n_per_side)
                .Select(i => offset + i * (1.0 - 2 * offset) / (n_per_side - 1))
                .ToArray();

            double[,] points = new double[n_per_side * n_per_side, 2];

            for (int i = 0; i < n_per_side; i++)
            {
                for (int j = 0; j < n_per_side; j++)
                {
                    int index = i * n_per_side + j;
                    points[index, 0] = points_one_side[i];
                    points[index, 1] = points_one_side[j];
                }
            }

            return points;
        }

     
        /// <summary>
        ///  Generates a list of crop boxes of different sizes. Each layer
        ///  has(2**i)**2 boxes for the ith layer.
        /// </summary>
        void generateCropBoxes(int orgWid, int orgHei, int n_layers, float overlap_ratio,
            ref List<List<int>> crop_boxes, ref List<int> layer_idxs)
        {
            int im_h = orgHei;
            int im_w = orgWid;

            int short_side = Math.Min(im_h, im_w);
            //Original image
            crop_boxes.Add(new List<int> { 0, 0, im_w, im_h });
            layer_idxs.Add(0);

            for (int i_layer = 0; i_layer < n_layers; i_layer++)
            {
                int n_crops_per_side = (int)Math.Pow(2, i_layer + 1);
                int overlap = (int)(overlap_ratio * short_side * (2 / n_crops_per_side));

                int crop_w = crop_len(im_w, n_crops_per_side, overlap);
                int crop_h = crop_len(im_h, n_crops_per_side, overlap);

                List<int> crop_box_x0 = new List<int>();
                List<int> crop_box_y0 = new List<int>();

                for (int i = 0; i < n_crops_per_side; i++)
                {
                    crop_box_x0.Add((crop_w - overlap) * i);
                    crop_box_y0.Add((crop_h - overlap) * i);
                }

                foreach (int x0 in crop_box_x0)
                {
                    foreach (int y0 in crop_box_y0)
                    {
                        List<int> box = new List<int>
                        {
                            x0,
                            y0,
                            Math.Min(x0 + crop_w, im_w),
                            Math.Min(y0 + crop_h, im_h)
                        };

                        crop_boxes.Add(box);
                        layer_idxs.Add(i_layer + 1);
                    }
                }

            }
        }
        int crop_len(int orig_len, int n_crops, int overlap)
        {
            return (int)(Math.Ceiling((double)(overlap * (n_crops - 1) + orig_len) / n_crops));
        }
        IEnumerable<List<object>> BatchIterator(int batch_size, params object[] args)
        {

            int n_batches = ((Array)args[0]).Length / batch_size + (((Array)args[0]).Length % batch_size != 0 ? 1 : 0);
            for (int b = 0; b < n_batches; b++)
            {
                List<object> batch = new List<object>();
                foreach (object arg in args)
                {
                    Array arr = (Array)arg;
                    int start = b * batch_size;
                    int end = (b + 1) * batch_size;
                    if (end > arr.Length)
                        end = arr.Length;

                    Array slice = Array.CreateInstance(arr.GetType().GetElementType(), end - start);
                    Array.Copy(arr, start, slice, 0, end - start); 
                    batch.Add(slice);
                }

                yield return batch;
            }
        }
        public MaskData Generate(string imgfile)
        {
            if (this.mImage != null)
                this.mImage.Dispose();

            this.mImage = Cv2.ImRead(imgfile, ImreadModes.Color);
            if (points_per_side != 0)
            {
                this.point_grids = build_all_layer_point_grids(
                points_per_side,
                crop_n_layers,
                crop_n_points_downscale_factor);
            }
            MaskData masks = this._generate_masks(this.mImage);
            return masks;
        }
        MaskData _generate_masks(Mat img)
        {
            MaskData masks = new MaskData();
            List<List<int>> crop_boxes = new List<List<int>>();
            List<int> layer_idxs = new List<int>();

            this.generateCropBoxes(img.Cols, img.Rows, this.crop_n_layers, this.crop_overlap_ratio, ref crop_boxes, ref layer_idxs);

            for (int i = 0; i < crop_boxes.Count; i++)
            {
                MaskData mask = this._process_crop(crop_boxes[i], layer_idxs[i]);
                masks.Cat(mask);
            }

            return masks;
        }

        MaskData _process_crop(List<int> crop_box, int crop_layer_idx)
        {
            MaskData masks = new MaskData();
            int x0 = crop_box[0], y0 = crop_box[1], x1 = crop_box[2], y1 = crop_box[3];
            // 定义ROI的矩形区域
            OpenCvSharp.Rect roiRect = new OpenCvSharp.Rect(x0, y0, x1, y1); // (x, y, width, height)
            // 提取ROI区域
            Mat roiImage = new Mat(this.mImage, roiRect);
            //图像编码
            //this.mImgEmbedding = this.mSAM.Encode(roiImage, roiImage.Cols, roiImage.Rows);

            double[,] gps = this.point_grids[crop_layer_idx];
            PointPromotion[] points_for_image = new PointPromotion[gps.GetLength(0)];
            Transforms ts = new Transforms(1024);
            for (int i = 0; i < gps.GetLength(0); i++)
            {
                Promotion promt = new PointPromotion(OpType.ADD);
                (promt as PointPromotion).X = (int)(gps[i, 0] * roiImage.Cols);
                (promt as PointPromotion).Y = (int)(gps[i, 1] * roiImage.Rows);
                PointPromotion ptn = ts.ApplyCoords((promt as PointPromotion), roiImage.Cols, roiImage.Rows);
                points_for_image[i] = ptn;
            }
            object[] args = { points_for_image };
            foreach (var v in BatchIterator(this.points_per_batch, args))
            {
                MaskData mask = this._process_batch(v.ToList(), roiImage.Cols, roiImage.Rows, crop_box, this.mImage.Cols, this.mImage.Rows);
                masks.Cat(mask);
            }
            roiImage.Dispose();
            return masks;
        }

        MaskData _process_batch(List<object> points,int cropimgwid,int cropimghei,List<int>cropbox,
            int orgimgwid, int orgimghei)
        {
            MaskData batch = new MaskData();
            List<float[]> masks = new List<float[]>();
            foreach (var v in points)
            {
                PointPromotion[] pts = v as PointPromotion[];
                for (int i=0;i<pts.Length;i++)
                {
                    MaskData md = this.mSAM.Decode(new List<Promotion>() { pts[i] }, this.mImgEmbedding, cropimgwid, cropimghei);
                    md.mStalibility = md.CalculateStabilityScore(this.mSAM.mask_threshold, this.stability_score_offset).ToList();
                    md.Filter(this.pred_iou_thresh, this.stability_score_thresh);
                    batch.Cat(md);
                }
                          
            }

            batch.mBox = batch.batched_mask_to_box().ToList();
            return batch;
        }

    }
}
