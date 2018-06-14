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
using System.Windows.Shapes;

namespace coyote
{
    /// <summary>
    /// 入力された文字列についてファイルが存在するか、ファイル名があっているかを判定する
    /// </summary>
    public class ExePathExistsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                var v = System.Convert.ToString(value);

                if (!File.Exists(v))
                {
                    return false;
                }

                var info = new System.IO.FileInfo(v);
                string target = parameter as string;
                return info.Name == target;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 入力された文字列についてフォルダが存在するかを判定する
    /// </summary>
    public class PathExistsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var v = System.Convert.ToString(value);
            return Directory.Exists(v);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Window1.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingWindow : Window
    {
        public SettingWindow()
        {
            this.DataContext = Properties.Settings.Default.MyConfig.Copy();
            InitializeComponent();
        }

        private void SteamBtsExeButton_Click(object sender, RoutedEventArgs e)
        {
            var pc = ShowOpenFileDialog(steamBtsExePath.Text);
            if (pc != null)
            {
                (DataContext as Config).Path2 = pc;
            }
        }

        private void BtsExeButton_Click(object sender, RoutedEventArgs e)
        {
            var pc = ShowOpenFileDialog(btsExePath.Text);
            if (pc != null)
            {
                (DataContext as Config).Path1 = pc;
            }
        }

        private PathConfig ShowOpenFileDialog(string initialDir)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                CheckFileExists = true,
                CheckPathExists = true,
                DereferenceLinks = true,
                Filter = "Civ4|Civ4BeyondSword.exe|Civ4日本語化|Civ4BeyondSword_japan.exe",
                InitialDirectory = initialDir
            };

            var Result = dialog.ShowDialog();

            if (Result.HasValue && Result.Value)
            {
                return new PathConfig(dialog.FileName);
            }
            else
            {
                return null;
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.MyConfig = ((Config)this.DataContext).Copy();
            Properties.Settings.Default.Save();
            this.DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
