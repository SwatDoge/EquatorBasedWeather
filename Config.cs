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
    }
}
