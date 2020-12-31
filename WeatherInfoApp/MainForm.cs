using System;
using System.Windows.Forms;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;

namespace WeatherInfoApp
{
    public partial class MainForm : Form
    {
        // Specify API_ID here (you need an account on https://openweathermap.org/)
        private const string API_ID = "";
        private OpenWeather.WeatherData CurrentCityWeather;
        private List<OpenWeather.WeatherData> History = new List<OpenWeather.WeatherData>();
        private WebRequest request = null;


        public MainForm()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (InternetConnection.IsConnectedToInternet() == false)
                showInternetConnectionErrorMessage();
            else
            {
                requestWeather();
            }
        }

        private async void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (InternetConnection.IsConnectedToInternet() == false)
                    showInternetConnectionErrorMessage();
                else
                {
                    requestWeather();
                }
            }
        }

        private void showInternetConnectionErrorMessage()
        {
            MessageBox.Show("Internet connection is not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private async void requestWeather()
        {
            WebResponse response;
            string city = textBox1.Text;
            request = WebRequest.Create($"http://api.openweathermap.org/data/2.5/weather?q={city}&units=metric&APPID={API_ID}");
            textBox1.Text = null;
            request.Method = "POST";
            request.ContentType = "application/x-www-urlencoded";
            string cityWeatherJson;
            try
            {
                response = await request.GetResponseAsync();
                using (Stream s = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(s))
                    {
                        cityWeatherJson = await reader.ReadToEndAsync();
                    }
                }
                response.Close();
            }
            catch
            {
                MessageBox.Show("City with such name wasn't found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ShowWeather(JsonConvert.DeserializeObject<OpenWeather.WeatherData>(cityWeatherJson));
        }

        private void ShowWeather(OpenWeather.WeatherData cityWeather)
        {
            CurrentCityWeather = cityWeather;
            string name = cityWeather.name + ", " + cityWeather.country;
            string time = UnixTimeStampToDateTime(cityWeather.dt).ToString("MM.dd.yy H:mm:ss");

            if (History.Count == 11)
            {
                History.RemoveAt(0);
            }


            History.Add(cityWeather);
            listBox1.Items.Clear();
            OpenWeather.WeatherData[] temp = new OpenWeather.WeatherData[History.Count];
            History.CopyTo(temp);
            for (int i = History.Count - 1; i >= 0; i--)
            {
                var weather = temp[i];
                listBox1.Items.Add(weather.name + ", " + weather.country + " " + UnixTimeStampToDateTime(weather.dt).ToString("MM.dd.yy H:mm:ss ") + weather.temp.ToString("0.#") + "°C");
            }


            label1.Visible = true;
            label10.Visible = true;
            groupBox2.Visible = true;

            label1.Text = name;
            label4.Text = cityWeather.temp.ToString("0.#") + "°C";
            label5.Text = "Humidity: " + cityWeather.humidity.ToString() + " %";
            label6.Text = "Pressure: " + cityWeather.pressure.ToString("0.") + " mm";
            label10.Text = "Time: " + time;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                CheckFileExists = true,
                Filter = "json files (*.json)|*.json"
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if (openFileDialog.FileName.Trim() != string.Empty)
                    {
                        using (StreamReader sr = new StreamReader(openFileDialog.FileName))
                        {
                            string json = sr.ReadToEnd();
                            if (String.IsNullOrWhiteSpace(json))
                            {
                                throw new FileLoadException();
                            }
                            ShowWeather(JsonConvert.DeserializeObject<OpenWeather.WeatherData>(json));
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + ex.StackTrace);
                }
            }
        }

        private void textBox1_MouseHover(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(textBox1, "Enter a city here");
        }

        private void infoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("The Weather checking application." + System.Environment.NewLine +
                            "To start the usage of the application type the city name and click Request Weather." + System.Environment.NewLine + System.Environment.NewLine +
                            "In the case of any connection error or too long response time, you may cancel the request by the Cancel Request button." + System.Environment.NewLine + System.Environment.NewLine +
                            "The current weather state of the requested city will appear at the top right corner of the screen." + System.Environment.NewLine + 
                            "You may see the history of your searches at the top left panel." + System.Environment.NewLine + System.Environment.NewLine +
                            "EXAMPLE 1, type Kyev and hit the Request Weather button;" + System.Environment.NewLine + System.Environment.NewLine +
                            "EXAMPLE 2, type Lviv and hit Ctrl + S to save the result. Then hit Ctrl + O to open saved data." + System.Environment.NewLine + System.Environment.NewLine +
                            "Please refer to the documentation for more details.", 
                            "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void saveToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            if (CurrentCityWeather == null)
            {
                MessageBox.Show("Please, find a city first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            saveToolStripMenuItem.Enabled = true;
            saveFileDialog1.Filter = "json files (*.json)|*.json";
            saveFileDialog1.FileName = $"{CurrentCityWeather.name}_{CurrentCityWeather.country}_weather_for_{UnixTimeStampToDateTime(CurrentCityWeather.dt):MM/dd/yy_H/mm/ss}.json";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;
            saveFileDialog1.OverwritePrompt = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string fileName = saveFileDialog1.FileName;
                WriteDataToFile(fileName);
            }
        }

        private void WriteDataToFile(string fileName)
        {
            try
            {
                using (FileStream fs = File.Open(fileName + ".json", FileMode.OpenOrCreate))
                using (StreamWriter sw = new StreamWriter(fs))
                using (JsonWriter jw = new JsonTextWriter(sw))
                {
                    jw.Formatting = Formatting.Indented;
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(jw, CurrentCityWeather);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace);
            }
        }

        public DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            return dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        }

        private void aboutProductToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("The Application was made by Evgeniy Kiprenko." + System.Environment.NewLine + System.Environment.NewLine + 
                "Email: zhenyakiprenko@gmail.com", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            OpenWeather.WeatherData[] temp = new OpenWeather.WeatherData[History.Count];
            History.CopyTo(temp);
            Array.Reverse(temp);
            int index = listBox1.SelectedIndex;
            if (index < History.Count)
            {
                ShowWeather(temp[index]);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (request != null)
            {
                request.Abort();
            }
        }
    }
}
