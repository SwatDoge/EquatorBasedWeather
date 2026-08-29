using Torch;
using Torch.Views;

namespace BetterRandomWeather
{
    public class Config : ViewModel
    {
        private bool _enabled = true;
        [Display(Order = 0, Name = "Enable plugin", Description = "Enables the custom weather")]
        public bool Enabled { 
            get => _enabled; 
            set => SetValue(ref _enabled, value); 
        }

        private bool _debug = false;
        [Display(Order = 1, Name = "Debug logs", Description = "Enables debug logs")]
        public bool Debug
        {
            get => _debug;
            set => SetValue(ref _debug, value);
        }

        private int _minWeatherDuration = 600;
        [Display(Order = 2, Name = "Minimum weather duration", Description = "Set the minimum time a piece of weather will last")]
        public int minWeatherDuration
        {
            get => _minWeatherDuration;
            set => SetValue(ref _minWeatherDuration, value);
        }

        private int _maxWeatherDuration = 900;
        [Display(Order = 2, Name = "Maximum weather duration", Description = "Set the maximum time a piece of weather will last")]
        public int maxWeatherDuration
        {
            get => _maxWeatherDuration;
            set => SetValue(ref _maxWeatherDuration, value);
        }
    }
}
