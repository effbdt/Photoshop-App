using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PhotoshopApp
{
	/// <summary>
	/// Interaction logic for Histogram.xaml
	/// </summary>
	public partial class Histogram : Window
	{
		private int[] histogram;

		public Histogram(int[] histogram)
		{
			InitializeComponent();
			this.histogram = histogram;

			this.Loaded += Histogram_Loaded;
			HistogramCanvas.SizeChanged += Histogram_SizeChanged;
		}

		private void Histogram_Loaded(object sender, RoutedEventArgs e)
		{
			DrawHistogram();
		}

		private void Histogram_SizeChanged(object sender, RoutedEventArgs e)
		{
			DrawHistogram();
		}

		private void DrawHistogram()
		{
			if (histogram == null) return;
			HistogramCanvas.Children.Clear();

			double width = HistogramCanvas.ActualWidth;
			double height = HistogramCanvas.ActualHeight;

			if (width == 0 || height == 0) return;

			int max = histogram.Max();

			double barWidth = width / histogram.Length;

			for (int i = 0; i < histogram.Length; i++)
			{
				double barHeight = ((double)histogram[i] / max) * height;
				Rectangle rect = new Rectangle
				{
					Width = barWidth,
					Height = barHeight,
					Fill = Brushes.Black
				};
				Canvas.SetLeft(rect, i * barWidth);
				Canvas.SetTop(rect, height - barHeight);
				HistogramCanvas.Children.Add(rect);

			}
		}
	}
}
