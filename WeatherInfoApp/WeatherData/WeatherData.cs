namespace WeatherInfoApp.OpenWeather
{
    class WeatherData
    {
        public double temp { get; set; }

        private double _pressure;
        public double pressure
        {
            get
            {
                return _pressure;
            }
            set
            {
                _pressure = value / 1.333;
            }
        }
        public double humidity { get; set; }
        public double dt { get; set; }
        public string country { get; set; }
        public string name { get; set; }
    }
}
