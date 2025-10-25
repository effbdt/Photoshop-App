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

namespace PhotoshopApp.Services
{
	/// <summary>
	/// Interaction logic for GammaInputWindow.xaml
	/// </summary>
	public partial class GammaInputWindow : Window
	{
		public double RedGamma { get; private set; }
		public double GreenGamma { get; private set; }
		public double BlueGamma { get; private set; }

		public GammaInputWindow()
		{
			InitializeComponent();
		}

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			RedGamma = RedSlider.Value;
			GreenGamma = GreenSlider.Value;
			BlueGamma = BlueSlider.Value;

			this.DialogResult = true;
			this.Close();
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
			this.Close();
		}

	}


}
