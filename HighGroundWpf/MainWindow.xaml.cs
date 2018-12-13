using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Google.Maps;
using Google.Maps.StaticMaps;
using Google.Maps.Elevation;
using Google.Maps.Common;
using Google.Maps.Geocoding;
using Newtonsoft.Json;

namespace HighGroundWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        string location;
        string key = ;
        static string urlStart = @"https://api.open-elevation.com/api/v1/lookup?locations=";
        public int zoom { get; set; }
        public double radius { get; set; }
        public double startLat { get; set; }
        public double startLng { get; set; }
        public double endLat { get; set; }
        public double endLng { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            zoom = 7;
            radius = .01;
            endLat = 0;
            endLng = 0;
        }

        private void UpdateLocation()
        //Replacs the imagebox background with a map contining either a single point or a start and end point.
        {
            //Initialize Map settings
            location = locationText.Text;
            GoogleSigned.AssignAllServices(new GoogleSigned(key));
            var llResponse = new GeocodingRequest();
            llResponse.Address = new Location(location);
            var ll = new GeocodingService().GetResponse(llResponse).Results[0].Geometry.Location;
            startLat = ll.Latitude;
            startLng = ll.Longitude;

            if(endLat == 0 && endLng == 0)
            //Executes if there is only a start point.
            {
                var map = new StaticMapRequest();
                map.Center = new Location(location);
                map.Zoom = zoom;
                map.Markers.Add(location);
                var response = new StaticMapService().GetImage(map);
                int len = (int)Math.Round(Math.Sqrt(response.Length));
                ImageSourceConverter a = new ImageSourceConverter();
                //Replaces imagebox background with map.
                ImageBrush brush = new ImageBrush();
                brush.ImageSource = (ImageSource)a.ConvertFrom(response);
                picGrid.Background = brush;
            }
            else
            //Executes when there is a start and end point.
            {
                var map = new StaticMapRequest();
                double tempLat = (startLat + endLat) / 2;
                double tempLng = (startLng + endLng) / 2;
                map.Center = new Location(Convert.ToString(tempLat) + ", " + Convert.ToString(tempLng));
                map.Zoom = zoom;
                map.MapType = MapTypes.Terrain;
                Location[] beginEnd = {new Location(startLat.ToString() + ", " + startLng.ToString()),
                new Location(endLat.ToString() + ", " + endLng.ToString())};
                map.Path = new Google.Maps.Path(beginEnd);

                var response = new StaticMapService().GetImage(map);
                ImageSourceConverter a = new ImageSourceConverter();

                ImageBrush brush = new ImageBrush();
                brush.ImageSource = (ImageSource)a.ConvertFrom(response);
                picGrid.Background = brush;
            }
            

           
        }

        public static double getElevation(double lat, double lng)
        //Gets the elevation from the open elevation api
        {
            byte[] response;
            try
            {
                response = new System.Net.WebClient().DownloadData(urlStart + lat.ToString() + ',' + lng.ToString());
            }
            catch (Exception x)
            //Allows the program to continue even if the connection quits
            {
                return 0;
            }
            //Find the elevation result in the json response
            string jsonStr = Encoding.UTF8.GetString(response);
            var temp = JsonConvert.DeserializeObject<Dictionary<String, Object>>(jsonStr);
            return Convert.ToDouble(temp["results"].ToString().Split(',')[1].Split(':')[1]);
        }

        private ElevationResult[] GetElevation(string place)
        //Used for the steepest ascent algorithm because it offers higher precision than open elevation api.
        {
            //This determines the gradient by sampling a circle around the current location.
            int numSamples = 100;
            double interval = 2 * Math.PI / numSamples;
            GoogleSigned.AssignAllServices(new GoogleSigned(key));
            var elev = new ElevationRequest();
            var llResponse = new GeocodingRequest();
            llResponse.Address = new Location(place);
            var ll = new GeocodingService().GetResponse(llResponse).Results[0].Geometry.Location;
            LatLng[] latLongList = new LatLng[numSamples + 1]; 
            for(int i = 0; i < numSamples; i++)
            {
                double angle = interval * i;
                double lat = ll.Latitude + radius * Math.Sin(angle);
                double lon = ll.Longitude + radius * Math.Sin(angle);
                latLongList[i] = new LatLng(lat, lon);
            }
            latLongList[numSamples] = ll;
            elev.AddLocations(latLongList);
            var elevations = new ElevationService().GetResponse(elev).Results;
            //Returns all numSamples elevations.
            return elevations;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        //Activates when the search button is pressed
        {
            UpdateLocation();
            var responses = GetElevation(location);
            int len = responses.Length;
            decimal elev = responses[len - 1].Elevation;

            //Can pick which algorithm to run here.
            SimulatedAnnealing();
            //SteepestAscent();
            double newElev = getElevation(endLat, endLng);
            double diff = newElev - Convert.ToDouble(elev);
            // Update elevation label
            elevationLabel.Content = "You Climbed " + Convert.ToString(diff) + " ft";
            Window1 newWindow = new Window1(startLat, startLng, endLat, endLng);
            newWindow.Show();
        }

        private void SteepestAscent()
        //Performs the steepest ascent algorithm on the starting point.
        {
            double tolerance = .0000000001;
            double dif = 9999999;
            List<double> gammas = new List<double>();
            List<ElevationResult> positions = new List<ElevationResult>();
            var responses = GetElevation(location);
            int numPoints = responses.Length;
            positions.Add(responses[numPoints - 1]);
            gammas.Add(1);
            decimal elev = responses[numPoints - 1].Elevation;
            int j = 1;
            while (dif > tolerance)
            {
                // Calculate greatest increase in elevation
                ElevationResult maxResult = new ElevationResult();
                maxResult = responses[0];
                decimal max = maxResult.Elevation - elev;
                for (int i = 1; i < numPoints - 1; i++)
                {
                    decimal tempDif = responses[i].Elevation - elev;
                    if (tempDif > max)
                    {
                        maxResult = responses[i];
                        max = tempDif;
                    }
                }
                //Advance in direction of greatest ascent
                double difLat = maxResult.Location.Latitude - positions[j - 1].Location.Latitude;
                double difLong = maxResult.Location.Longitude - positions[j - 1].Location.Longitude;
                decimal difElev = maxResult.Elevation - positions[j - 1].Elevation;
                double newLat = positions[j - 1].Location.Latitude + gammas[j - 1] * difLat;
                double newLong = positions[j - 1].Location.Longitude + gammas[j - 1] * difLong;
                string next = Convert.ToString(newLat) + ", " + Convert.ToString(newLong);
                responses = GetElevation(next);
                positions.Add(responses[numPoints - 1]);
                decimal newElev = responses[numPoints - 1].Elevation;
                double dr = Math.Sqrt(difLat * difLat + difLong * difLong);
                double dE = Convert.ToDouble(newElev - positions[j - 1].Elevation) /radius;
                double gamma = 1 * dr * dE / (dE * dE);
                gammas.Add(gamma);
                j = j + 1;
                dif = Math.Abs(dE);
            }
            endLat = positions[j - 1].Location.Latitude;
            endLng = positions[j - 1].Location.Longitude;
            UpdateLocation();
        }

        private void SimulatedAnnealing()
        //Performs the Simulated annealing algroithm on the starting point
        {
            int stepsPerTemp = 100;
            int numTemps = 100;
            double alpha = 0.9;
            double temp = 1.0;
            double radius = 0.05;
            double elev = getElevation(startLat, startLng);
            double[] elevations = new double[numTemps * stepsPerTemp];
            double[] lats = new double[numTemps * stepsPerTemp];
            double[] lngs = new double[numTemps * stepsPerTemp];
            elevations[0] = elev;
            lats[0] = startLat;
            lngs[0] = startLng;
            for(int i = 0; i < numTemps; i++)
            {
                for(int j = 0; j < stepsPerTemp; j++)
                {
                    if (i == 0 && j == 0) { } //Skips first step because the first position is the starting point.
                    else
                    {
                        int index = i * stepsPerTemp + j;
                        Random rand = new Random();
                        double randRad = rand.NextDouble() * radius;
                        double randAngle = rand.NextDouble() * 6.28318530718;
                        double oldLat = lats[index - 1];
                        double oldLng = lngs[index - 1];
                        double oldElev = elevations[index - 1];
                        double newLat = oldLat + Math.Cos(randAngle) * randRad;
                        double newLng = oldLng + Math.Sin(randAngle) * randRad;
                        double newElev = getElevation(newLat, newLng);
                        if(newElev > oldElev)
                        {
                            //Accept this always.
                            lats[index] = newLat;
                            lngs[index] = newLng;
                            elevations[index] = newElev;
                        }
                        else
                        {
                            //Accept this SOMETIMES
                            double prob = Math.Exp(-(oldElev - newElev) / temp);
                            if(rand.NextDouble() < prob)
                            {
                                //Accepted condition
                                lats[index] = newLat;
                                lngs[index] = newLng;
                                elevations[index] = newElev;
                            }
                            else
                            {
                                //New step not accepted
                                lats[index] = oldLat;
                                lngs[index] = oldLng;
                                elevations[index] = oldElev;
                            }
                        }
                    }
                }
                //Decrease the Temperature
                temp = temp * alpha;
            }
            double maxelev, maxlat, maxlng;
            maxelev = -99999999;
            maxlat = 0;
            maxlng = 0;
            Random rand1 = new Random();
            //Write the latitude, longitude and elevation to a csv file.
            using (var writer = new StreamWriter(@"C:\Users\topha\OneDrive\Documents\Visual Studio 2017\CHE 204\HighGroundWpf\HighGroundWpf\latlngelev" + rand1.Next().ToString() + ".csv"))
            {
                for (int i = 0; i < stepsPerTemp * numTemps; i++)
                {
                    writer.WriteLine(lats[i] + ", " + lngs[i] + ", " + elevations[i]);
                    if(maxelev < elevations[i])
                    {
                        maxelev = elevations[i];
                        maxlat = lats[i];
                        maxlng = lngs[i];
                    }
                }
            }
            // Draw map based on Maximum spot with route between
            startLat = lats[0];
            startLng = lngs[0];
            endLat = maxlat;
            endLng = maxlng;
            UpdateLocation();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        //Allows the window size to be changed.
        {
            var s = sender as Window;
            double width = s.Width;
            double height = s.Height;
            width = Math.Max(width, 200);
            height = Math.Max(height, 200);
            this.Width = width;
            this.Height = height;
            picGrid.Width = width - 20;
            picGrid.Height = height - 110;
            
            locationText.Width = width - searchButton.Width - 30;

        }

        private void zoomInButton_Click(object sender, RoutedEventArgs e)
        //When + button is pressed, zooms in.
        {
            zoom = zoom == 22 ? 22 : zoom + 1; //22 is the maximum zoom allowed.
            UpdateLocation();
        }

        private void zoomOutButton_Click(object sender, RoutedEventArgs e)
        //When - button is pressed, zoom out.
        {
            zoom = zoom == 1 ? 1 : zoom - 1; //1 is the smallest zoom allowed.
            UpdateLocation();
        }

        private void locationText_KeyDown(object sender, KeyEventArgs e)
        //When user types enter, triggers search button press.
        {
            if (e.Key == Key.Enter)
            {
                Button_Click(sender, e);
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        //Makes window draggable.
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        //Makes x button close the window.
        {
            this.Close();
        }
    }
}
