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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Torch.Views;

namespace GridSplitNameKeeper
{
    /// <summary>
    /// Interaction logic for Control
    /// </summary>
    public partial class Control : UserControl
    {
        private PluginCore Plugin { get; }

        public Control()
        {
            InitializeComponent();
        }

        public Control(PluginCore plugin) : this()
        {
            Plugin = plugin;
            DataContext = plugin.Config;
        }

        private void EditBlocks_OnClick(object sender, RoutedEventArgs e)
        {
            var editor = new CollectionEditor() {Owner = Window.GetWindow(this)};
            editor.Edit<string>(Plugin.Config.IgnoreBlockList, "Ignored Blocks - Use ONLY TypeId or subtypeId");
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            Plugin.Save();
        }
    }
}
