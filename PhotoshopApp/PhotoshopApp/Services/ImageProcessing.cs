using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;

namespace PhotoshopApp.Services
{
	public class ImageProcessing
	{
		public static void InvertImage(Bitmap b)
		{
			BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
				ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

			int stride = bmData.Stride;
			IntPtr Scan0 = bmData.Scan0;

			byte[] invertLUT = new byte[256];
			for (int i = 0; i < 256; i++)
			{
				invertLUT[i] = (byte)(255 - i);
			}

			unsafe
			{
				byte* pBase = (byte*)Scan0.ToPointer();

				int nWidth = b.Width;
				int nHeight = b.Height;
				Parallel.For(0, nHeight, y =>
				{
					byte* p = pBase + y * stride;
					for (int x = 0; x < nWidth; ++x)
					{
						p[0] = invertLUT[p[0]];
						p[1] = invertLUT[p[1]];
						p[2] = invertLUT[p[2]];
						p += 3;
					}
				});
			}

			b.UnlockBits(bmData);
		}

		public static void Grayscale(Bitmap b)
		{
			BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
				ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

			int stride = bmData.Stride;
			IntPtr Scan0 = bmData.Scan0;

			byte[] redLUT = new byte[256];
			byte[] greenLUT = new byte[256];
			byte[] blueLUT = new byte[256];
			for (int i = 0; i < 256; i++)
			{
				redLUT[i] = (byte)((299 * i) / 1000);
				greenLUT[i] = (byte)((587 * i) / 1000);
				blueLUT[i] = (byte)((114 * i) / 1000);
			}

			unsafe
			{
				byte* pBase = (byte*)Scan0;

				int nHeight = b.Height;
				int nWidth = b.Width;

				Parallel.For(0, nHeight, y =>
				{
					byte* p = pBase + y * stride;

					for (int x = 0; x < nWidth; ++x)
					{
						int gray = (redLUT[p[2]] + greenLUT[p[1]] + blueLUT[p[0]]);
						p[0] = p[1] = p[2] = (byte)gray;
						p += 3;
					}
				});
			}

			b.UnlockBits(bmData);
		}

		public static void Gamma(Bitmap b, double red, double blue, double green)
		{
			BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
				ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

			int stride = bmData.Stride;
			IntPtr Scan0 = bmData.Scan0;

			byte[] redGamma = new byte[256];
			byte[] greenGamma = new byte[256];
			byte[] blueGamma = new byte[256];
			for (int i = 0; i < 256; i++)
			{
				redGamma[i] = (byte)Math.Min(255, (int)(
					(255.0 * Math.Pow(i / 255.0, 1.0 / red)) + 0.5));
				greenGamma[i] = (byte)Math.Min(255, (int)(
					(255.0 * Math.Pow(i / 255.0, 1.0 / green)) + 0.5));
				blueGamma[i] = (byte)Math.Min(255, (int)(
					(255.0 * Math.Pow(i / 255.0, 1.0 / blue)) + 0.5));
			}
			int width = b.Width;
			int height = b.Height;
			unsafe
			{

				byte* pBase = (byte*)Scan0;
				Parallel.For(0, height, y =>
				{
					byte* row = pBase + (y * stride);

					for (int x = 0; x < width; x++)
					{
						row[0] = blueGamma[row[0]];
						row[1] = greenGamma[row[1]];
						row[2] = redGamma[row[2]];

						row += 3;
					}
				});
			}

			b.UnlockBits(bmData);
		}

		public static void Conv3x3(Bitmap b, ConvMatrix m)
		{
			if (m.Factor == 0)
				return;

			Bitmap bSrc = (Bitmap)b.Clone();
			BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
				ImageLockMode.ReadWrite,
				System.Drawing.Imaging.PixelFormat.Format24bppRgb);

			BitmapData bmSrc = bSrc.LockBits(new Rectangle(0, 0, b.Width, b.Height),
				ImageLockMode.ReadWrite,
				System.Drawing.Imaging.PixelFormat.Format24bppRgb);

			int stride = bmData.Stride;
			int stride2 = stride * 2;

			IntPtr Scan0 = bmData.Scan0;
			IntPtr SrcScan0 = bmSrc.Scan0;

			unsafe
			{
				byte* pDestBase = (byte*)Scan0;
				byte* pSrcBase = (byte*)SrcScan0;

				int nOffset = stride - b.Width * 3;
				int nWidth = b.Width - 2;
				int nHeight = b.Height - 2;

				Parallel.For(0, nHeight, y =>
				{
					byte* pDest = pDestBase + (y + 1) * stride + 3;
					byte* pSrcRow0 = pSrcBase + y * stride;
					byte* pSrcRow1 = pSrcBase + (y + 1) * stride;
					byte* pSrcRow2 = pSrcBase + (y + 2) * stride;

					for (int x = 0; x < nWidth; ++x)
					{
						int r = (pSrcRow0[2] * m.TopLeft + pSrcRow0[5] * m.TopMid + pSrcRow0[8] * m.TopRight +
								 pSrcRow1[2] * m.MidLeft + pSrcRow1[5] * m.Pixel + pSrcRow1[8] * m.MidRight +
								 pSrcRow2[2] * m.BottomLeft + pSrcRow2[5] * m.BottomMid + pSrcRow2[8] * m.BottomRight) / m.Factor + m.Offset;

						int g = (pSrcRow0[1] * m.TopLeft + pSrcRow0[4] * m.TopMid + pSrcRow0[7] * m.TopRight +
								pSrcRow1[1] * m.MidLeft + pSrcRow1[4] * m.Pixel + pSrcRow1[7] * m.MidRight +
								pSrcRow2[1] * m.BottomLeft + pSrcRow2[4] * m.BottomMid + pSrcRow2[7] * m.BottomRight) / m.Factor + m.Offset;

						int b = (pSrcRow0[0] * m.TopLeft + pSrcRow0[3] * m.TopMid + pSrcRow0[6] * m.TopRight +
								pSrcRow1[0] * m.MidLeft + pSrcRow1[3] * m.Pixel + pSrcRow1[6] * m.MidRight +
								pSrcRow2[0] * m.BottomLeft + pSrcRow2[3] * m.BottomMid + pSrcRow2[6] * m.BottomRight) / m.Factor + m.Offset;

						pDest[2] = (byte)Math.Max(0, Math.Min(255, r));
						pDest[1] = (byte)Math.Max(0, Math.Min(255, g));
						pDest[0] = (byte)Math.Max(0, Math.Min(255, b));

						pDest += 3;
						pSrcRow0 += 3;
						pSrcRow1 += 3;
						pSrcRow2 += 3;
					}
				});
			}

			b.UnlockBits(bmData);
			bSrc.UnlockBits(bmSrc);
		}


		public static void GaussianBlur(Bitmap b)
		{
			ConvMatrix m = new ConvMatrix();
			m.TopLeft = 1; m.TopMid = 2; m.TopRight = 3;
			m.MidLeft = 2; m.Pixel = 4; m.MidRight = 2;
			m.BottomLeft = 1; m.BottomMid = 2; m.BottomRight = 1;
			m.Factor = 16;
			Conv3x3(b, m);
		}

		public static void BoxFilter(Bitmap b)
		{
			ConvMatrix m = new ConvMatrix();
			m.SetAll(1);
			m.Factor = 9;

			Conv3x3(b, m);
		}

		//public static void SobelEdgeDetector(Bitmap b)
		//{
		//	Bitmap bX = (Bitmap)b.Clone();
		//	Bitmap bY = (Bitmap)b.Clone();

		//	ConvMatrix mx = new ConvMatrix();
		//	mx.TopLeft = -1; mx.TopMid = 0; mx.TopRight = 1;
		//	mx.MidLeft = -2; mx.Pixel = 0; mx.MidRight = 2;
		//	mx.BottomLeft = -1; mx.BottomMid = 0; mx.BottomRight = 1;
		//	mx.Factor = 1;
		//	mx.Offset = 0;

		//	ConvMatrix my = new ConvMatrix();
		//	my.TopLeft = -1; my.TopMid = -2; my.TopRight = -1;
		//	my.MidLeft = 0; my.Pixel = 0; my.MidRight = 0;
		//	my.BottomLeft = 1; my.BottomMid = 2; my.BottomRight = 1;
		//	my.Factor = 1;
		//	my.Offset = 0;

		//	Conv3x3(bX, mx);
		//	Conv3x3(bY, my);

		//	BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
		//		ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
		//	BitmapData bmX = bX.LockBits(new Rectangle(0, 0, b.Width, b.Height),
		//		ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
		//	BitmapData bmY = bY.LockBits(new Rectangle(0, 0, b.Width, b.Height),
		//		ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

		//	int stride = bmData.Stride;
		//	int offset = stride - b.Width * 3;

		//	unsafe
		//	{
		//		byte* p = (byte*)bmData.Scan0;
		//		byte* px = (byte*)bmX.Scan0;
		//		byte* py = (byte*)bmY.Scan0;

		//		for (int y = 0; y < b.Height; y++)
		//		{
		//			for (int x = 0; x < b.Width; x++)
		//			{
		//				for (int c = 0; c < 3; c++)
		//				{
		//					int gx = px[c];
		//					int gy = py[c];
		//					int g = (int)Math.Sqrt(gx * gx + gy * gy);
		//					if (g > 255) g = 255;
		//					p[c] = (byte)g;
		//				}

		//				p += 3;
		//				px += 3;
		//				py += 3;
		//			}

		//			p += offset;
		//			px += offset;
		//			py += offset;
		//		}
		//	}

		//	b.UnlockBits(bmData);
		//	bX.UnlockBits(bmX);
		//	bY.UnlockBits(bmY);
		//}

		public static void SobelEdgeDetector(Bitmap b)
		{
			int width = b.Width;
			int height = b.Height;

			BitmapData bmData = b.LockBits(
				new Rectangle(0, 0, width, height),
				ImageLockMode.ReadWrite,
				System.Drawing.Imaging.PixelFormat.Format24bppRgb);

			int stride = bmData.Stride;
			int totalBytes = stride * height;

			byte[] original = new byte[totalBytes];
			System.Runtime.InteropServices.Marshal.Copy(bmData.Scan0, original, 0, totalBytes);

			int max = 2040;
			byte[] LUT = new byte[max + 1];
			for (int i = 0; i <= max; i++)
			{
				LUT[i] = (byte)(i > 255 ? 255 : i);
			}

			unsafe
			{
				byte* dstBase = (byte*)bmData.Scan0;

				GCHandle handle = GCHandle.Alloc(original, GCHandleType.Pinned);
				try
				{
					IntPtr srcPtr = handle.AddrOfPinnedObject();



					Parallel.For(1, height - 1, y =>
					{
						byte* srcBase = (byte*)srcPtr;
						byte* dstRow = dstBase + y * stride;
						byte* srcPrev = srcBase + (y - 1) * stride;
						byte* srcCur = srcBase + y * stride;
						byte* srcNext = srcBase + (y + 1) * stride;

						for (int x = 1; x < width - 1; x++)
						{
							int baseIdx = x * 3;
							int leftIdx = baseIdx - 3;
							int rightIdx = baseIdx + 3;

							byte* topLeft = srcPrev + leftIdx;
							byte* topCenter = srcPrev + baseIdx;
							byte* topRight = srcPrev + rightIdx;

							byte* midLeft = srcCur + leftIdx;
							byte* midRight = srcCur + rightIdx;

							byte* botLeft = srcNext + leftIdx;
							byte* botCenter = srcNext + baseIdx;
							byte* botRight = srcNext + rightIdx;

							int tlB = topLeft[0]; int tlG = topLeft[1]; int tlR = topLeft[2];
							int tcB = topCenter[0]; int tcG = topCenter[1]; int tcR = topCenter[2];
							int trB = topRight[0]; int trG = topRight[1]; int trR = topRight[2];

							int mlB = midLeft[0]; int mlG = midLeft[1]; int mlR = midLeft[2];
							int mrB = midRight[0]; int mrG = midRight[1]; int mrR = midRight[2];

							int blB = botLeft[0]; int blG = botLeft[1]; int blR = botLeft[2];
							int bcB = botCenter[0]; int bcG = botCenter[1]; int bcR = botCenter[2];
							int brB = botRight[0]; int brG = botRight[1]; int brR = botRight[2];

							int gxB = (-tlB - 2 * mlB - blB) + (trB + 2 * mrB + brB);
							int gxG = (-tlG - 2 * mlG - blG) + (trG + 2 * mrG + brG);
							int gxR = (-tlR - 2 * mlR - blR) + (trR + 2 * mrR + brR);

							int gyB = (-tlB - 2 * tcB - trB) + (blB + 2 * bcB + brB);
							int gyG = (-tlG - 2 * tcG - trG) + (blG + 2 * bcG + brG);
							int gyR = (-tlR - 2 * tcR - trR) + (blR + 2 * bcR + brR);

							int absGxB = gxB < 0 ? -gxB : gxB;
							int absGyB = gyB < 0 ? -gyB : gyB;
							byte valB = LUT[absGxB + absGyB];

							int absGxG = gxG < 0 ? -gxG : gxG;
							int absGyG = gyG < 0 ? -gyG : gyG;
							byte valG = LUT[absGxG + absGyG];

							int absGxR = gxR < 0 ? -gxR : gxR;
							int absGyR = gyR < 0 ? -gyR : gyR;
							byte valR = LUT[absGxR + absGyR];

							byte* dstPixel = dstRow + baseIdx;
							dstPixel[0] = valB;
							dstPixel[1] = valG;
							dstPixel[2] = valR;
						}

						dstRow[0] = dstRow[1] = dstRow[2] = 0;
						int rb = (width - 1) * 3;
						dstRow[rb + 0] = dstRow[rb + 1] = dstRow[rb + 2] = 0;
					});
				}
				finally
				{
					handle.Free();
				}
			}

			b.UnlockBits(bmData);
		}


		public static void LaplaceEdgeDetector(Bitmap b)
		{
			ConvMatrix m = new ConvMatrix();

			m.TopLeft = 0; m.TopMid = -1; m.TopRight = 0;
			m.MidLeft = -1; m.Pixel = 4; m.MidRight = -1;
			m.BottomLeft = 0; m.BottomMid = -1; m.BottomRight = 0;

			m.Factor = 1;
			m.Offset = 0;
			Conv3x3(b, m);
		}

		public static void LogTransform(Bitmap b)
		{
			int width = b.Width;
			int height = b.Height;

			BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
		ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

			int stride = bmData.Stride;
			IntPtr Scan0 = bmData.Scan0;

			double c = 255.0 / Math.Log(256.0);

			byte[] logLUT = new byte[256];
			for (int i = 0; i < 256; i++)
			{
				logLUT[i] = (byte)(c * Math.Log(1 + i));
			}

			unsafe
			{
				byte* p = (byte*)Scan0;

				Parallel.For(0, height, y =>
				{
					byte* row = p + y * stride;

					for (int x = 0; x < width; ++x)
					{
						row[0] = logLUT[row[0]];
						row[1] = logLUT[row[1]];
						row[2] = logLUT[row[2]];

						row += 3;
					}
				});
			}

			b.UnlockBits(bmData);
		}

		public static int[] Histogram(Bitmap b)
		{
			int[] hist = new int[256];

			int width = b.Width;
			int height = b.Height;

			byte[] redLUT = new byte[256];
			byte[] greenLUT = new byte[256];
			byte[] blueLUT = new byte[256];
			for (int i = 0; i < 256; i++)
			{
				redLUT[i] = (byte)(.299 * i);
				greenLUT[i] = (byte)(.587 * i);
				blueLUT[i] = (byte)(.114 * i);
			}

			BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
		ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

			int stride = bmData.Stride;
			IntPtr Scan0 = bmData.Scan0;

			unsafe
			{
				byte* pBase = (byte*)Scan0;
				int[][] localHists = new int[Environment.ProcessorCount][];
				for (int i = 0; i < localHists.Length; i++)
				{
					localHists[i] = new int[256];
				}

				Parallel.For(0, height, y =>
				{
					int threadId = System.Threading.Thread.CurrentThread.ManagedThreadId % localHists.Length;
					int[] localHist = localHists[threadId];

					byte* row = pBase + y * stride;

					for (int x = 0; x < width; x++)
					{
						int intensity = (int)(redLUT[row[2]] + greenLUT[row[1]] + blueLUT[row[0]]);
						localHist[intensity]++;
						row += 3;
					}

				});

				for (int t = 0; t < localHists.Length; t++)
				{
					for (int i = 0; i < 256; i++)
					{
						hist[i] += localHists[t][i];
					}
				}

			}

			b.UnlockBits(bmData);
			return hist;
		}

		public static void HistogramEqualization(Bitmap b)
		{
			int[] hist = Histogram(b);
			int totalPixels = b.Width * b.Height;

			int[] cdf = new int[256];
			cdf[0] = hist[0];
			for (int i = 1; i < 256; i++)
				cdf[i] = cdf[i - 1] + hist[i];

			byte[] lut = new byte[256];
			float scale = 255f / (totalPixels - cdf[0]);
			for (int i = 0; i < 256; i++)
			{
				lut[i] = (byte)Math.Round((cdf[i] - cdf[0]) * scale);
				if (lut[i] > 255) lut[i] = 255;
			}

			BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
				ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

			int stride = bmData.Stride;
			IntPtr Scan0 = bmData.Scan0;

			int width = b.Width;
			int height = b.Height;

			unsafe
			{
				byte* pBase = (byte*)Scan0;


				Parallel.For(0, height, y =>
				{
					byte* row = pBase + y * stride;

					for (int x = 0; x < width; x++)
					{
						row[0] = lut[row[0]];
						row[1] = lut[row[1]];
						row[2] = lut[row[2]];

						row += 3;
					}
				});
			}

			b.UnlockBits(bmData);
		}

		//public static void HarrisCornerDetector(Bitmap b)
		//{
		//	const float k = 0.04f;
		//	const float threshold = 1000000f;
		//	const int windowRadius = 1;

		//	int width = b.Width;
		//	int height = b.Height;

		//	BitmapData bmData = b.LockBits(
		//		new Rectangle(0, 0, width, height),
		//		ImageLockMode.ReadWrite,
		//		System.Drawing.Imaging.PixelFormat.Format24bppRgb);

		//	int stride = bmData.Stride;
		//	IntPtr scan0 = bmData.Scan0;

		//	unsafe
		//	{
		//		byte* p = (byte*)scan0;

		//		float[,] Ix2 = new float[height, width];
		//		float[,] Iy2 = new float[height, width];
		//		float[,] IxIy = new float[height, width];

		//		for (int y = 1; y < height - 1; y++)
		//		{
		//			for (int x = 1; x < width - 1; x++)
		//			{
		//				byte* pixel = p + y * stride + x * 3;

		//				int gx = pixel[3] - pixel[-3];
		//				int gy = pixel[stride] - pixel[-stride];

		//				Ix2[y, x] = gx * gx;
		//				Iy2[y, x] = gy * gy;
		//				IxIy[y, x] = gx * gy;
		//			}
		//		}

		//		float[,] R = new float[height, width];

		//		for (int y = 1; y < height - 1; y++)
		//		{
		//			for (int x = 1; x < width - 1; x++)
		//			{
		//				float sumIx2 = 0, sumIy2 = 0, sumIxIy = 0;
		//				for (int wy = -1; wy <= 1; wy++)
		//				{
		//					for (int wx = -1; wx <= 1; wx++)
		//					{
		//						sumIx2 += Ix2[y + wy, x + wx];
		//						sumIy2 += Iy2[y + wy, x + wx];
		//						sumIxIy += IxIy[y + wy, x + wx];
		//					}
		//				}

		//				float det = sumIx2 * sumIy2 - sumIxIy * sumIxIy;
		//				float trace = sumIx2 + sumIy2;
		//				R[y, x] = det - k * trace * trace;
		//			}
		//		}

		//		for (int y = windowRadius; y < height - windowRadius; y++)
		//		{
		//			for (int x = windowRadius; x < width - windowRadius; x++)
		//			{
		//				if (R[y, x] < threshold) continue;

		//				bool isMax = true;
		//				for (int wy = -windowRadius; wy <= windowRadius && isMax; wy++)
		//				{
		//					for (int wx = -windowRadius; wx <= windowRadius; wx++)
		//					{
		//						if (R[y + wy, x + wx] > R[y, x])
		//						{
		//							isMax = false;
		//							break;
		//						}
		//					}
		//				}

		//				if (isMax)
		//				{
		//					byte* pixel = p + y * stride + x * 3;
		//					pixel[0] = 0;
		//					pixel[1] = 0;
		//					pixel[2] = 255;
		//				}
		//			}
		//		}
		//	}

		//	b.UnlockBits(bmData);
		//}

		//big iamge 3700ms
		//1080 190ms
		//new 750ms
		public static void HarrisCornerDetector(Bitmap b)
		{
			const float k = 0.06f;
			const float threshold = 1000000f;

			int width = b.Width;
			int height = b.Height;

			byte[] gray = new byte[width * height];
			BitmapData bmData = b.LockBits(
				new Rectangle(0, 0, width, height),
				ImageLockMode.ReadWrite,
				System.Drawing.Imaging.PixelFormat.Format24bppRgb);

			int stride = bmData.Stride;
			IntPtr scan0 = bmData.Scan0;

			unsafe
			{
				byte* p = (byte*)scan0;

				for (int y = 0; y < height; y++)
				{
					byte* row = p + y * stride;
					for (int x = 0; x < width; x++)
					{
						gray[y * width + x] = (byte)(0.299 * row[2] + 0.587 * row[1] + 0.114 * row[0]);
						row += 3;
					}
				}

				float[] Ix2 = new float[width * height];
				float[] Iy2 = new float[width * height];
				float[] IxIy = new float[width * height];

				Parallel.For(1, height - 1, y =>
				{
					for (int x = 1; x < width - 1; x++)
					{
						int idx = y * width + x;
						int gx = gray[idx + 1] - gray[idx - 1];
						int gy = gray[idx + width] - gray[idx - width];
						Ix2[idx] = gx * gx;
						Iy2[idx] = gy * gy;
						IxIy[idx] = gx * gy;

					}
				});

				float[] R = new float[width * height];

				Parallel.For(1, height - 1, y =>
				{
					for (int x = 1; x < width - 1; x++)
					{
						int idx = y * width + x;
						float sumIx2 = 0, sumIy2 = 0, sumIxIy = 0;
						for (int wy = -1; wy <= 1; wy++)
						{
							int rowOffset = (y + wy) * width;
							for (int wx = -1; wx <= 1; wx++)
							{
								int i = rowOffset + (x + wx);
								sumIx2 += Ix2[i];
								sumIy2 += Iy2[i];
								sumIxIy += IxIy[i];
							}

						}

						float det = sumIx2 * sumIy2 - sumIxIy * sumIxIy;
						float trace = sumIx2 + sumIy2;
						R[idx] = det - k * trace * trace;
					}
				});

				for (int y = 1; y < height; y++)
				{
					byte* row = p + y * stride;
					for (int x = 0; x < width; x++)
					{
						int idx = y * width + x;
						if (R[idx] > threshold)
						{
							row[x * 3 + 0] = 0;
							row[x * 3 + 1] = 0;
							row[x * 3 + 2] = 255;
						}
					}
				}

			}

			b.UnlockBits(bmData);
		}

	}
}
