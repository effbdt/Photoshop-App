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

		public byte[] GammaLookUpTable(double color)
		{
			byte[] lut = new byte[256];
			for (int i = 0; i < 256; i++)
			{
				lut[i] = (byte)Math.Min(255, (int)(
					(255.0 * Math.Pow(i / 255.0, 1.0 / color)) + 0.5));
			}
			return lut;
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
	}
}
