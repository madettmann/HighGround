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

namespace HighGroundWpf
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public double startLat { get; set; }
        public double startLng { get; set; }
        public double endLat { get; set; }
        public double endLng { get; set; }
        public Window1(double lat1, double lng1, double lat2, double lng2)
        //Window1 constructor
        {
            startLat = lat1;
            startLng = lng1;
            endLat = lat2;
            endLng = lng2;
            InitializeComponent();
        }

        private void Directions_Button_Click(object sender, RoutedEventArgs e)
        //Opens directions in default browser.
        {
            string baseUrl = @"https://www.google.com/maps/dir/?api=1&origin=";
            string locations = startLat.ToString() + ',' + startLng.ToString() + @"&destination=" +
                endLat.ToString() + ',' + endLng.ToString();
            System.Diagnostics.Process.Start(baseUrl + locations);
            this.Close();

        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        //Makes this window draggable
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        //Makes x button close the window.
        {
            this.Close();
        }
    }
}
