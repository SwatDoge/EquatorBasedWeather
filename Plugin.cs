using NLog;
using System.IO;
using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using Torch.Managers.PatchManager;
using Torch.Views;

namespace BetterRandomWeather
{
    public class Plugin : TorchPluginBase, IWpfPlugin
    {
        public static Persistent<Config> _config;
        public static Config Config => _config?.Data;

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            _config = Persistent<Config>.Load(Path.Combine(StoragePath, "BetterRandomWeather.cfg"));
        }

        public UserControl GetControl() => new PropertyGrid
        {
            Margin = new(3),
            DataContext = _config.Data
        };
    }
}