using Microsoft.Win32;
using PhotoshopApp.Services;
using System.Diagnostics;
using System.Drawing;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PhotoshopApp
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			FilterComboBox.Items.Add("Invert");
			FilterComboBox.Items.Add("Grayscale");
			FilterComboBox.Items.Add("Gamma");
			FilterComboBox.Items.Add("BoxFilter");
			FilterComboBox.Items.Add("GaussianBlur");
			FilterComboBox.Items.Add("SobelEdgeDetector");
			FilterComboBox.Items.Add("LaplaceEdgeDetector");
			FilterComboBox.Items.Add("LogTransform");
			FilterComboBox.Items.Add("Histogram");
			FilterComboBox.Items.Add("HistogramEqualization");
			FilterComboBox.Items.Add("HarrisCornerDetector");


			FilterComboBox.SelectedIndex = 0;
		}

		private BitmapImage LoadUserImage()
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Title = "Select an Image";
			openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*";

			if (openFileDialog.ShowDialog() == true)
			{
				// Load the selected image
				BitmapImage bitmap = new BitmapImage();
				bitmap.BeginInit();
				bitmap.UriSource = new Uri(openFileDialog.FileName, UriKind.Absolute);
				bitmap.CacheOption = BitmapCacheOption.OnLoad;
				bitmap.EndInit();
				bitmap.Freeze();

				return bitmap;
			}

			return null;
		}

		private void Button_LoadImage_Click(object sender, RoutedEventArgs e)
		{
			BitmapImage bitmap = LoadUserImage();
			if (bitmap != null)
			{
				MyImageControl.Source = bitmap;
				loadedImage = ConvertToBitmap(bitmap);

				originalImage = (Bitmap)loadedImage.Clone();
			}
		}
		private void ResetButton_Click(object sender, RoutedEventArgs e)
		{
			if (originalImage == null)
				return;

			// Clone the original to loadedImage so filters can be applied again
			loadedImage = (Bitmap)originalImage.Clone();

			// Refresh the Image control
			MyImageControl.Source = ConvertToBitmapImage(loadedImage);
		}


		private static Bitmap ConvertToBitmap(BitmapImage bitmapImage)
		{
			using (MemoryStream outStream = new MemoryStream())
			{
				BitmapEncoder enc = new BmpBitmapEncoder();
				enc.Frames.Add(BitmapFrame.Create(bitmapImage));
				enc.Save(outStream);
				Bitmap bmp = new Bitmap(outStream);
				return new Bitmap(bmp);
			}
		}

		private BitmapImage ConvertToBitmapImage(Bitmap bitmap)
		{
			using (MemoryStream memory = new MemoryStream())
			{
				bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
				memory.Position = 0;

				BitmapImage bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.StreamSource = memory;
				bitmapImage.EndInit();
				bitmapImage.Freeze();
				return bitmapImage;
			}
		}

		private Bitmap originalImage;
		private Bitmap loadedImage;
		private void ApplyFilterButton_Click(object sender, RoutedEventArgs e)
		{
			if (loadedImage == null)
			{
				MessageBox.Show("Load an image first!");
				return;
			}

			string selectedFilter = FilterComboBox.SelectedItem.ToString();

			var sw = new Stopwatch();

			switch (selectedFilter)
			{
				case "Invert":
					sw = Stopwatch.StartNew();
					ImageProcessing.InvertImage(loadedImage);
					break;
				case "Grayscale":
					sw = Stopwatch.StartNew();
					ImageProcessing.Grayscale(loadedImage);
					break;
				case "Gamma":
					GammaInputWindow gammaWindow = new GammaInputWindow();
					gammaWindow.Owner = this;
					if (gammaWindow.ShowDialog() == true)
					{
						sw = Stopwatch.StartNew();
						ImageProcessing.Gamma(loadedImage,
							gammaWindow.RedGamma,
							gammaWindow.BlueGamma,
							gammaWindow.GreenGamma);
					}
					break;
				case "BoxFilter":
					sw = Stopwatch.StartNew();
					ImageProcessing.BoxFilter(loadedImage);
					break;
				case "GaussianBlur":
					sw = Stopwatch.StartNew();
					ImageProcessing.GaussianBlur(loadedImage);
					break;
				case "SobelEdgeDetector":
					sw = Stopwatch.StartNew();
					ImageProcessing.SobelEdgeDetector(loadedImage);
					break;
				case "LaplaceEdgeDetector":
					sw = Stopwatch.StartNew();
					ImageProcessing.LaplaceEdgeDetector(loadedImage);
					break;
				case "LogTransform":
					sw = Stopwatch.StartNew();
					ImageProcessing.LogTransform(loadedImage);
					break;
				case "Histogram":
					int[] hist = ImageProcessing.Histogram(loadedImage);
					Histogram histWindow = new Histogram(hist);
					histWindow.Show();
					break;
				case "HistogramEqualization":
					sw = Stopwatch.StartNew();
					ImageProcessing.HistogramEqualization(loadedImage);
					break;
				case "HarrisCornerDetector":
					sw = Stopwatch.StartNew();
					ImageProcessing.HarrisCornerDetector(loadedImage);
					break;

			}

			sw.Stop();
			MyImageControl.Source = ConvertToBitmapImage(loadedImage);
			MessageBox.Show($"Filter applied in {sw.ElapsedMilliseconds} ms");

		}

		private void SaveImageButton_Click(object sender, RoutedEventArgs e)
		{
			if (loadedImage == null)
			{
				MessageBox.Show("No image to save!");
				return;
			}

			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.Title = "Save Image";
			saveFileDialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg;*.jpeg|Bitmap Image|*.bmp";

			if (saveFileDialog.ShowDialog() == true)
			{
				System.Drawing.Imaging.ImageFormat format = System.Drawing.Imaging.ImageFormat.Png;

				string ext = System.IO.Path.GetExtension(saveFileDialog.FileName).ToLower();
				switch (ext)
				{
					case ".jpg":
					case ".jpeg":
						format = System.Drawing.Imaging.ImageFormat.Jpeg;
						break;
					case ".bmp":
						format = System.Drawing.Imaging.ImageFormat.Bmp;
						break;
				}

				loadedImage.Save(saveFileDialog.FileName, format);
				MessageBox.Show("Image saved successfully!");
			}
		}


	}
}
