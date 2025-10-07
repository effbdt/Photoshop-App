using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace PhotoshopApp.Services
{
	public class ImageProcessing
	{
		public static void InvertImage(Bitmap b)
		{
			BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
				ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

			int stride = bmData.Stride;
			System.IntPtr Scan0 = bmData.Scan0;
			unsafe
			{
				byte* p = (byte*)(void*)Scan0;
				int nOffset = stride - b.Width * 3;
				int nWidth = b.Width * 3;
				int nHeight = b.Height;
				for (int y = 0; y < nHeight; ++y)
				{
					for (int x = 0; x < nWidth; ++x)
					{
						p[0] = (byte)(255 - p[0]);
						++p;
					}
					p += nOffset;
				}
			}

			b.UnlockBits(bmData);
		}

		public static void Grayscale(Bitmap b)
		{
			BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
				ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

			int stride = bmData.Stride;
			System.IntPtr Scan0 = bmData.Scan0;

			unsafe
			{
				byte* p = (byte*)(void*)Scan0;

				int nOffset = stride - b.Width * 3;

				byte red, green, blue;

				for (int y = 0; y < b.Height; ++y)
				{
					for (int x = 0; x < b.Width; ++x)
					{
						blue = p[0];
						green = p[1];
						red = p[2];

						p[0] = p[1] = p[2] = (byte)
							(.299 * red + .587 * green + .114 * blue);

						p += 3;
					}
					p += nOffset;
				}
			}

			b.UnlockBits(bmData);
		}

		public static void Brightness(Bitmap b, int value)
		{
			BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
				ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

			int stride = bmData.Stride;
			System.IntPtr Scan0 = bmData.Scan0;

			unsafe
			{
				byte* p = (byte*)(void*)Scan0;

				int nOffset = stride - b.Width * 3;
				int nWidth = b.Width * 3;
				int nHeight = b.Height;

				for (int y = 0; y < b.Height; ++y)
				{
					for (int x = 0; x < nWidth; ++x)
					{
						int temp = (int)(p[0] + value);
						if (temp < 0) temp = 0;
						if (temp > 255) temp = 255;

						p[0] = (byte)temp;
						++p;
					}
					p += nOffset;
				}
			}

			b.UnlockBits(bmData);
		}

		public static void Contrast(Bitmap b, int nContrast)
		{
			if (nContrast < -100) nContrast = -100;
			if (nContrast > 100) nContrast = 100;

			BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
				ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

			int stride = bmData.Stride;
			System.IntPtr Scan0 = bmData.Scan0;

			double contrast = (100.0 + nContrast) / 100.0;
			contrast *= contrast;

			byte[] contrastLut = new byte[256];

			for (int i = 0; i < 256; i++)
			{
				double pixel = i / 255.0;
				pixel -= 0.5;
				pixel *= contrast;
				pixel += 0.5;
				pixel *= 255;

				if (pixel < 0) pixel = 0;
				if (pixel > 255) pixel = 255;

				contrastLut[i] = (byte)pixel;
			}

			unsafe
			{
				byte* p = (byte*)(void*)Scan0;
				int nOffset = stride - b.Width * 3;

				for (int y = 0; y < b.Height; ++y)
				{
					for (int x = 0; x < b.Width; ++x)
					{
						p[0] = contrastLut[p[0]];
						p[1] = contrastLut[p[1]];
						p[2] = contrastLut[p[2]];
						p += 3;
					}
					p += nOffset;
				}
			}

			b.UnlockBits(bmData);
		}

		public static void Gamma(Bitmap b, double red, double blue, double green)
		{
			BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
				ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

			int stride = bmData.Stride;
			System.IntPtr Scan0 = bmData.Scan0;

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

			unsafe
			{
				byte* p = (byte*)(void*)Scan0;

				int nOffset = stride - b.Width * 3;
				int nWidth = b.Width * 3;
				int nHeight = b.Height;

				for (int y = 0; y < b.Height; ++y)
				{
					for (int x = 0; x < nWidth; ++x)
					{
						p[0] = blueGamma[p[0]];
						p[1] = greenGamma[p[1]];
						p[2] = redGamma[p[2]];

						p += 3;
					}
					p += nOffset;
				}
			}

			b.UnlockBits(bmData);
		}

		public static void Conv3x3(Bitmap b, ConvMatrix m)
		{
			if (0 == m.Factor)
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

			System.IntPtr Scan0 = bmData.Scan0;
			System.IntPtr SrcScan0 = bmSrc.Scan0;

			unsafe
			{
				byte* p = (byte*)(void*)Scan0;
				byte* pSrc = (byte*)(void*)SrcScan0;
				int nOffset = stride - b.Width * 3;
				int nWidth = b.Width - 2;
				int nHeight = b.Height - 2;

				int nPixel;

				for (int y = 0; y < nHeight; ++y)
				{
					for (int x = 0; x < nWidth; ++x)
					{
						nPixel = ((((pSrc[2] * m.TopLeft) +
							(pSrc[5] * m.TopMid) +
							(pSrc[8] * m.TopRight) +
							(pSrc[2 + stride] * m.MidLeft) +
							(pSrc[5 + stride] * m.Pixel) +
							(pSrc[8 + stride] * m.MidRight) +
							(pSrc[2 + stride2] * m.BottomLeft) +
							(pSrc[5 + stride2] * m.BottomMid) +
							(pSrc[8 + stride2] * m.BottomRight))
							/ m.Factor) + m.Offset);

						if (nPixel < 0) nPixel = 0;
						if (nPixel > 255) nPixel = 255;
						p[5 + stride] = (byte)nPixel;

						nPixel = ((((pSrc[1] * m.TopLeft) +
							(pSrc[4] * m.TopMid) +
							(pSrc[7] * m.TopRight) +
							(pSrc[1 + stride] * m.MidLeft) +
							(pSrc[4 + stride] * m.Pixel) +
							(pSrc[7 + stride] * m.MidRight) +
							(pSrc[1 + stride2] * m.BottomLeft) +
							(pSrc[4 + stride2] * m.BottomMid) +
							(pSrc[7 + stride2] * m.BottomRight))
							/ m.Factor) + m.Offset);

						if (nPixel < 0) nPixel = 0;
						if (nPixel > 255) nPixel = 255;
						p[4 + stride] = (byte)nPixel;

						nPixel = ((((pSrc[0] * m.TopLeft) +
									   (pSrc[3] * m.TopMid) +
									   (pSrc[6] * m.TopRight) +
									   (pSrc[0 + stride] * m.MidLeft) +
									   (pSrc[3 + stride] * m.Pixel) +
									   (pSrc[6 + stride] * m.MidRight) +
									   (pSrc[0 + stride2] * m.BottomLeft) +
									   (pSrc[3 + stride2] * m.BottomMid) +
									   (pSrc[6 + stride2] * m.BottomRight))
							/ m.Factor) + m.Offset);

						if (nPixel < 0) nPixel = 0;
						if (nPixel > 255) nPixel = 255;
						p[3 + stride] = (byte)nPixel;

						p += 3;
						pSrc += 3;
					}

					p += nOffset;
					pSrc += nOffset;
				}
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

		public static void SobelEdgeDetector(Bitmap b)
		{
			Bitmap bX = (Bitmap)b.Clone();
			Bitmap bY = (Bitmap)b.Clone();

			// Sobel X
			ConvMatrix mx = new ConvMatrix();
			mx.TopLeft = -1; mx.TopMid = 0; mx.TopRight = 1;
			mx.MidLeft = -2; mx.Pixel = 0; mx.MidRight = 2;
			mx.BottomLeft = -1; mx.BottomMid = 0; mx.BottomRight = 1;
			mx.Factor = 1;
			mx.Offset = 0;

			// Sobel Y
			ConvMatrix my = new ConvMatrix();
			my.TopLeft = -1; my.TopMid = -2; my.TopRight = -1;
			my.MidLeft = 0; my.Pixel = 0; my.MidRight = 0;
			my.BottomLeft = 1; my.BottomMid = 2; my.BottomRight = 1;
			my.Factor = 1;
			my.Offset = 0;

			// Apply the convolutions
			Conv3x3(bX, mx);
			Conv3x3(bY, my);

			// Combine results
			BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
				ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			BitmapData bmX = bX.LockBits(new Rectangle(0, 0, b.Width, b.Height),
				ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			BitmapData bmY = bY.LockBits(new Rectangle(0, 0, b.Width, b.Height),
				ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

			int stride = bmData.Stride;
			int offset = stride - b.Width * 3;

			unsafe
			{
				byte* p = (byte*)bmData.Scan0;
				byte* px = (byte*)bmX.Scan0;
				byte* py = (byte*)bmY.Scan0;

				for (int y = 0; y < b.Height; y++)
				{
					for (int x = 0; x < b.Width; x++)
					{
						for (int c = 0; c < 3; c++)
						{
							int gx = px[c];
							int gy = py[c];
							int g = (int)Math.Sqrt(gx * gx + gy * gy);
							if (g > 255) g = 255;
							p[c] = (byte)g;
						}

						p += 3;
						px += 3;
						py += 3;
					}

					p += offset;
					px += offset;
					py += offset;
				}
			}

			b.UnlockBits(bmData);
			bX.UnlockBits(bmX);
			bY.UnlockBits(bmY);
		}

		public static void LaplaceEdgeDetector(Bitmap b)
		{
			ConvMatrix m = new ConvMatrix();

			m.TopLeft = 0; m.TopMid = -1; m.TopRight = 0;
			m.MidLeft = -1; m.Pixel = 4; m.MidRight = -1;
			m.BottomLeft = 0; m.BottomMid = -1; m.BottomRight = 0;

			m.Factor = 1;
			m.Offset = 128;
			Conv3x3(b, m);
		}

		public static void LogTransform(Bitmap b)
		{
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
				byte* p = (byte*)(void*)Scan0;
				int nOffset = stride - b.Width * 3;

				for (int y = 0; y < b.Height; ++y)
				{
					for (int x = 0; x < b.Width; ++x)
					{
						p[0] = logLUT[p[0]];
						p[1] = logLUT[p[1]];
						p[2] = logLUT[p[2]];

						p += 3;
					}
					p += nOffset;
				}
			}

			b.UnlockBits(bmData);
		}

	}
}
