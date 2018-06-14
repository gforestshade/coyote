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

using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;

namespace coyote
{
    public class BindableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual bool SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName]string propertyName = null)
        {
            if (Equals(field, value)) { return false; }
            field = value;
            var h = this.PropertyChanged;
            if (h != null) { h(this, new PropertyChangedEventArgs(propertyName)); }
            return true;
        }
    }

    public class Config : BindableBase
    {
        private PathConfig path1;
        [global::System.Configuration.UserScopedSettingAttribute()]
        public PathConfig Path1
        {
            get { return this.path1; }
            set { this.SetProperty(ref this.path1, value); }
        }
        private PathConfig path2;
        public PathConfig Path2
        {
            get { return this.path2; }
            set { this.SetProperty(ref this.path2, value); }
        }

        public Config Copy()
        {
            return new Config { Path1 = this.Path1.Copy(), Path2 = this.path2.Copy() };
        }
    }

    public class PathConfig : BindableBase
    {
        private string _BtsExePath;
        public string BtsExePath
        {
            get { return this._BtsExePath; }
            set { this.SetProperty(ref this._BtsExePath, value); }
        }

        private string _UsersModsPath;
        public string UsersModsPath
        {
            get { return this._UsersModsPath; }
            set { this.SetProperty(ref this._UsersModsPath, value); }
        }

        private string _BtsModsPath;
        public string BtsModsPath
        {
            get { return this._BtsModsPath; }
            set { this.SetProperty(ref this._BtsModsPath, value); }
        }

        public PathConfig Copy()
        {
            return this.MemberwiseClone() as PathConfig;
        }

        public PathConfig()
        {
            BtsExePath = "";
            UsersModsPath = "";
            BtsModsPath = "";
        }

        public PathConfig(string exePath)
        {
            BtsExePath = exePath;

            var info = new FileInfo(BtsExePath);
            if (info.Exists)
            {
                var dirMods = info.Directory.GetDirectories("MODS");
                if (dirMods.Length == 1)
                {
                    BtsModsPath = dirMods[0].FullName;
                }

                var usersModsLnks = info.Directory.GetFiles("_Civ4CustomMods.lnk");
                if (usersModsLnks.Length == 1)
                {
                    // ショートカットを辿る
                    var shell = new IWshRuntimeLibrary.WshShell();
                    var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(usersModsLnks[0].FullName);
                    UsersModsPath = shortcut.TargetPath.ToString();
                }
            }
        }

        public static PathConfig Default = new PathConfig(@"C:\Program Files (x86)\CYBERFRONT\Sid Meier's Civilization 4(J)\Beyond the Sword(J)\Civ4BeyondSword.exe");
        public static PathConfig Steam = new PathConfig(@"C:\Program Files (x86)\Steam\SteamApps\common\Sid Meier's Civilization IV Beyond the Sword\Beyond the Sword\Civ4BeyondSword_japan.exe");

    }

    public class MyList1 : ObservableCollection<System.IO.DirectoryInfo>
    {
        public MyList1(PathConfig path)
        {
            Reset(path);
        }

        public void Reset(PathConfig path)
        {
            ClearItems();
            AddFolderInfo(path.UsersModsPath);
            AddFolderInfo(path.BtsModsPath);
        }

        private void AddFolderInfo(string folderName)
        {
            if (Directory.Exists(folderName))
            {
                var info = new DirectoryInfo(folderName);
                foreach (var d in info.EnumerateDirectories())
                {
                    Add(d);
                }
            }

        }
    }
 
    public class ModPathConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value[0] is string fullpath && value[1] is PathConfig pathConfig)
            {
                if (fullpath.Contains(pathConfig.BtsModsPath))
                {
                    return 1;
                }
                else if (fullpath.Contains(pathConfig.UsersModsPath))
                {
                    return 2;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }
        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MyList1 mylist1 { get; set; }

        public PathConfig CurrentPathConfig { get; private set; }

        public Config config;

        public MainWindow()
        {
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.MyConfig == null)
            {
                Properties.Settings.Default.MyConfig = new Config { Path1 = PathConfig.Default, Path2 = PathConfig.Steam };
            }

            config = Properties.Settings.Default.MyConfig;
            CurrentPathConfig = config.Path1;
            mylist1 = new MyList1(config.Path1);

            DataContext = this;

            InitializeComponent();

            var cv = CollectionViewSource.GetDefaultView(mylist1);
            cv.Filter += MyList1_Filter;

            searchText.Focus();
        }


       private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = dirList.SelectedItem as System.IO.DirectoryInfo;
            StartMod(item.Name);
            this.Close();
        }

        private void StartMod(string modName)
        {
            var exefileinfo = new System.IO.FileInfo(CurrentPathConfig.BtsExePath);

            ProcessStartInfo psInfo = new ProcessStartInfo()
            {
                FileName = exefileinfo.FullName,
                WorkingDirectory = exefileinfo.DirectoryName,
                CreateNoWindow = true,
                UseShellExecute = false,
            };
            if (modName != "")
            {
                psInfo.Arguments = String.Format("mod=\"{0}\"", modName);
            }

            Process p = Process.Start(psInfo);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (dirList.SelectedItem is DirectoryInfo item)
            {
                StartMod(item.Name);
                this.Close();
            }
        }

        private void Config_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SettingWindow();
            bool? Return = dlg.ShowDialog();

            if (Return.HasValue && Return.Value)
            {
                config = Properties.Settings.Default.MyConfig;
                SetCurrentPathFromCombobox();
            }
        }

        private void GameKind_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetCurrentPathFromCombobox();
        }

        private void SetCurrentPathFromCombobox()
        {
            if (gameKind.SelectedIndex == 0)
            {
                CurrentPathConfig = config.Path1;
            }
            else if (gameKind.SelectedIndex == 1)
            {
                CurrentPathConfig = config.Path2;
            }
            mylist1.Reset(CurrentPathConfig);
        }

        private void OpenVanilla_Click(object sender, RoutedEventArgs e)
        {
            StartMod("");
            this.Close();
        }

        private void OpenExplorer_Click(object sender, RoutedEventArgs e)
        {
            if (dirList.SelectedItem is System.IO.DirectoryInfo item)
            {
                Process.Start(item.FullName);
            }
        }

        private void OpenCmd_Click(object sender, RoutedEventArgs e)
        {
            if (dirList.SelectedItem is System.IO.DirectoryInfo item)
            {
                ProcessStartInfo psInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    WorkingDirectory = item.FullName,
                };
                Process.Start(psInfo);
            }
        }

        private void SortKind_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cv = CollectionViewSource.GetDefaultView(mylist1);
            if (sortKind.SelectedIndex == 0)
            {
                cv.SortDescriptions.Clear();
            }
            else if (sortKind.SelectedIndex == 1)
            {
                cv.SortDescriptions.Clear();
                cv.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            }
            else if (sortKind.SelectedIndex == 2)
            {
                cv.SortDescriptions.Clear();
                cv.SortDescriptions.Add(new SortDescription("LastWriteTime", ListSortDirection.Descending));
            }
        }

        private bool MyList1_Filter(object obj)
        {
            if (obj is DirectoryInfo info)
            {
                // 両方を小文字にして部分一致
                string s1 = info.Name.ToLower(), s2 = searchText.Text.ToLower();
                return s1.Contains(s2);
            }
            else
            {
                return false;
            }
        }

        private void searchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(mylist1).Refresh();
            if (mylist1.Count != 0)
            {
                dirList.SelectedIndex = 0;
            }
        }

        private void searchText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                int i = dirList.SelectedIndex;
                i = Math.Max(i - 1, 0);
                dirList.SelectedIndex = i;
                dirList.ScrollIntoView(mylist1[i]);
            }
            else if (e.Key == Key.Down)
            {
                int i = dirList.SelectedIndex;
                i = Math.Min(i + 1, mylist1.Count-1);
                dirList.SelectedIndex = i;
                dirList.ScrollIntoView(mylist1[i]);
            }
        }
    }
}
