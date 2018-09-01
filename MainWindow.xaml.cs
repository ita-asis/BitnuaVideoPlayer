using BitnuaVideoPlayer.ViewModels;
using CefSharp;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml.Linq;
using System.Windows.Threading;
using System.Diagnostics;
using System.Reflection;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Text;
using Newtonsoft.Json;

namespace BitnuaVideoPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string c_MainVmSettingKey = "MainVmJson";
        private static Random m_Random = new Random();
        private PresentaionWindow m_PlayerWindow;
        private FileSystemWatcher m_FileSysWatcher;
        private ClientInfo m_BitnuaClient;
        private IEnumerable<Tuple<string, string>> m_picSources;
        private IEnumerable<string> m_Flyerfiles;
        private CancellationTokenSource m_PicLoopToken;
        private MongoClient m_MongoClient;
        private IMongoCollection<PlayEntry> DB_Plays;
        private IMongoCollection<ClientInfo> DB_ActiveClients;
        private CancellationTokenSource m_WatchCloudDBToken;
        private Song m_Song;

        //private IMongoCollection<ClientInfo> DB_OnlineClients;

        public MainViewModel VM { get; set; }

        public MainWindow()
        {
            System.Windows.Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            InitializeComponent();

            Loaded += MainWindow_Loaded;
            MouseDown += Window_MouseDown;
            App.Current.Exit += App_Exit;

            VersionLbl.Content = GetVersion();
            AppUpdateManager.VersionUpdateProgressChanged += p => VersionLbl.Dispatcher.BeginInvoke(new Action(() => VersionLbl.Content = $"Update in progress: {(p / 100d).ToString("P0")}"));
        }

        private object GetVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return string.Format("Version {0}.{1}.{2}", version.Major, version.Minor, version.Build);
        }

        internal static string LogException(Exception exception)
        {
            var sb = new StringBuilder();

            Trace.WriteLine(null);
            Trace.WriteLine(null);
            Trace.TraceError($"{DateTime.Now}");

            var ex = exception;
            while (ex != null)
            {
                Trace.TraceError(ex.Message);
                sb.AppendLine(ex.Message);
                Trace.TraceError(ex.StackTrace);
                sb.AppendLine();
                sb.AppendLine(ex.StackTrace);
                sb.AppendLine();
                sb.AppendLine();


                ex = ex.InnerException;
            }
            Trace.Flush();

            return sb.ToString();
        }

        private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            var msg = LogException(e.Exception);
            System.Windows.MessageBox.Show(msg, "Exception!", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await InitAll();
        }

        private async Task InitAll()
        {
            string vmJson = Convert.ToString(UserSettings.Get(c_MainVmSettingKey));
            DataContext = VM = MainViewModel.Create(vmJson);
            VM.CurrentClient = m_BitnuaClient = new ClientInfo() { Name = VM.ClientName };
            VM.PropertyChanged += VM_PropertyChanged;
            await InitMongoDb();


            m_FileSysWatcher = new FileSystemWatcher()
            {
                Path = VM.WatchDir,
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = "*.*",
                EnableRaisingEvents = true
            };
            m_FileSysWatcher.Changed += new FileSystemEventHandler(OnWatchDirChanged);

            m_PlayerWindow = new PresentaionWindow() { DataContext = VM };
            m_PlayerWindow.WindowStyle = WindowStyle.None;
            m_PlayerWindow.Show();
            m_PlayerWindow.Owner = this;


            RegisterBannerPicsChanged();

            await ShowLastSong();
        }

        private void RegisterBannerPicsChanged()
        {
            if (VM?.Banner != null)
            {
                VM.Banner.PropertyChanged += (s,e) => 
                {
                    if (e.PropertyName == nameof(VM.Banner.PicsPath))
                        ReadBannerPics();
                };
            }

            ReadBannerPics();
        }

        private void ReadBannerPics()
        {
            if (!string.IsNullOrEmpty(VM?.Banner?.PicsPath))
            {
                var directory = new DirectoryInfo(VM.Banner.PicsPath);
                VM.Banner.Pics = new ObservableCollection<PictureItem>(from f in directory.GetFiles() select new PictureItem() { Path = f.FullName });
            }
        }

        private async Task ShowLastSong()
        {
            var directory = new DirectoryInfo(VM.WatchDir);
            var lastFile = (from f in directory.GetFiles("*.xml")
                            orderby f.LastWriteTimeUtc descending
                            select f).FirstOrDefault();

            if (lastFile == null || lastFile.LastWriteTimeUtc + TimeSpan.FromMinutes(15) < DateTime.UtcNow)
                return;

            var song = ReadSongInfo(lastFile.FullName);
            await UpdateVM(song);
        }

        private async void VM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VM.SelectedLayout))
            {
                if (VM.SelectedLayout == eLayoutModes.Default)
                {
                    await UpdateVM();
                }
                else if (VM.SelectedLayout == eLayoutModes.Presentation)
                {
                    m_PicLoopToken?.Cancel();
                }
            }
        }

        private async Task InitMongoDb()
        {
            var url = new MongoUrl("mongodb://svc_bitnua:zIGloC1lGQ3uw24d@ds247141.mlab.com:47141/bitnua_vplayer");
            m_MongoClient = new MongoClient(url);

            var db = m_MongoClient.GetDatabase("bitnua_vplayer");
            DB_Plays = db.GetCollection<PlayEntry>("ampsVmmplay");
            DB_ActiveClients = db.GetCollection<ClientInfo>("activeClients");
            DB_ActiveClients.InsertOne(m_BitnuaClient);

            await UpdateActiveClients();
        }

        private async Task UpdateActiveClients()
        {
            VM.ActiveClients = await GetActiveClients();
        }

        private static Song ReadSongInfo(string filePath)
        {
            var song = new Song();
            var fields = ReadSongXml(filePath);

            Field field;
            int year;
            if (fields.TryGetValue(Fields.Title, out field))
                song.Title = field.Value;
            if (fields.TryGetValue(Fields.Heb_Title, out field))
                song.HebTitle = field.Value;
            if (fields.TryGetValue(Fields.Performer, out field))
                song.Performer = field.Value;
            if (fields.TryGetValue(Fields.Creator, out field))
                song.Creator = field.Value;
            if (fields.TryGetValue(Fields.Composer, out field))
                song.Composer = field.Value;
            if (fields.TryGetValue(Fields.Writer, out field))
                song.Writer = field.Value;
            if (fields.TryGetValue(Fields.Year, out field) && int.TryParse(field.Value, out year))
                song.Year = year;

            if (fields.TryGetValue(Fields.Lyrics, out field) && !string.IsNullOrWhiteSpace(field.Value))
                song.Lyrics = field.Value;
            else if (fields.TryGetValue(Fields.Lyrics2, out field) && !string.IsNullOrWhiteSpace(field.Value))
                song.Lyrics = field.Value;

            if (fields.TryGetValue(Fields.YouTubeSong, out field) && !string.IsNullOrWhiteSpace(field.Value))
                song.YouTubeSong = field.Value;
            if (fields.TryGetValue(Fields.YouTubeDance, out field) && !string.IsNullOrWhiteSpace(field.Value))
                song.YouTubeDance = field.Value;

            TrimNames(song);

            return song;
        }

        private static void TrimNames(Song song)
        {
            int seperatorInd = song.Title?.IndexOf('-') ?? -1;
            if (seperatorInd > 0)
            {
                song.Title = song.Title.Substring(0, seperatorInd).Trim();
            }

            seperatorInd = song.HebTitle?.IndexOf('-') ?? -1;
            if (seperatorInd > 0)
            {
                song.HebTitle = song.HebTitle.Substring(0, seperatorInd).Trim();
            }
        }

        private async void OnWatchDirChanged(object sender, FileSystemEventArgs e)
        {
            await Task.Delay(100);
            await LogSong(e.FullPath.Replace(@"//", @"/"));
        }

        private async Task LogSong(string filePath)
        {
            try
            {
                var song = m_Song = ReadSongInfo(filePath);

                if (VM.CurrentClient == m_BitnuaClient)
                    await UpdateVM(song);

                await DB_Plays.InsertOneAsync(new PlayEntry() { Song = song, Client = m_BitnuaClient });
            }
            catch (Exception)
            {
            }
        }

        private Task UpdateVM() => UpdateVM(m_Song);
        private async Task UpdateVM(Song song)
        {
            m_PicLoopToken?.Cancel();

            if (song == null)
                return;

            VM.Song = song;
            VM.ArtistPicSource = await GetArtistPic();
            m_picSources = GetAvaiableSongPics().Shuffle(m_Random);
            VM.DefaultLayout_VideoItem.VideoSources = GetAvaiableSongVideos().Shuffle(m_Random).ToList();

            if (!string.IsNullOrWhiteSpace(VM.FlayerDir))
            {
                m_Flyerfiles = Directory.EnumerateFiles(VM.FlayerDir, "*.*", SearchOption.AllDirectories).Shuffle(m_Random);
            }

            m_PicLoopToken = new CancellationTokenSource();

            if (VM.SelectedLayout == eLayoutModes.Default)
            {
                var t = Task.Run(() => StartLeftPicTask(m_PicLoopToken.Token));
            }
        }

        private IEnumerable<Tuple<string, string>> IterateNextLeftPic(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (VM.SelectedPicMode == ViewModels.ePicMode.Flyer)
                {
                    using (var flyerPics = m_Flyerfiles.GetEnumerator())
                        while (flyerPics.MoveNext() && !token.IsCancellationRequested)
                            yield return new Tuple<string, string>(flyerPics.Current, null);
                }
                else
                {
                    using (var songPics = m_picSources.GetEnumerator())
                        while (songPics.MoveNext() && !token.IsCancellationRequested)
                            yield return songPics.Current;
                }
            }
        }

        private async Task StartLeftPicTask(CancellationToken token)
        {
            await Task.Delay(100);
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var st = DateTime.UtcNow.Ticks;
                    if (VM.Song != null && (VM.SelectedPicMode != ViewModels.ePicMode.Lyrics))
                    {
                        foreach (var fileNTitle in IterateNextLeftPic(token))
                        {
                            VM.LeftPicSource = fileNTitle.Item1;
                            VM.LeftPicTitle.Text = fileNTitle.Item2;
                            Console.WriteLine($"{TimeSpan.FromTicks(DateTime.UtcNow.Ticks - st)}: {fileNTitle.Item2}");
                            await Task.Delay(Math.Max(VM.LeftPicDelay, 3000), token).ConfigureAwait(false);
                        }
                    }

                }
                catch (Exception)
                {
                }
            }
        }

        private async Task StartWatchCloudDBTask(CancellationToken token)
        {
            PlayEntry currEntry = null;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var newEntry = await ReadLastPlayEntry(VM.CurrentClient, token);
                    if ((!newEntry?.Equals(currEntry)) ?? false)
                    {
                        currEntry = newEntry;
                        await UpdateVM(newEntry.Song);
                    }

                    await Task.Delay(VM.DbUpdateDelay, token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
#if DEBUG
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
#endif
                }
            }
        }

        private async Task<PlayEntry> ReadLastPlayEntry(ClientInfo currentClient, CancellationToken token)
        {
            var filter = Builders<PlayEntry>.Filter.Eq("Client._id", currentClient.Id);
            var sort = Builders<PlayEntry>.Sort.Descending("Time");
            var playEntry = (await DB_Plays.Find(filter).Sort(sort).Limit(1).ToListAsync(token)).SingleOrDefault();

            return playEntry;
        }

        private Task<string> GetArtistPic()
        {
            string res = null;
            if (VM.Song != null)
            {
                var path = VM.PicPathPerformer;
                var person = VM.Song.Performer;
                var dir = GetPicPath(path, person);
                if (Directory.Exists(dir))
                {
                    var files = Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories);
                    res = files.FirstOrDefault();
                }
            }

            return Task.FromResult(res);
        }

        private static string PickRandomFile(string dir, string searchPattern = null)
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                return null;

            string file = null;
            try
            {
                var files = Directory.EnumerateFiles(dir, searchPattern ?? "*.*", SearchOption.AllDirectories);
                file = files.FirstOrDefault();
            }
            catch (Exception)
            {
            }

            return file;
        }

        private IEnumerable<Tuple<string, string>> GetAvaiableSongPics()
        {
            var pics = new List<Tuple<string, string>>() { new Tuple<string, string>(VM.PicPathDefault, string.Empty) };

            if (VM.Song != null)
            {
                AddDir(pics, VM.Pic_ShowCreator, VM.Song.Creator, VM.PicPathCreator);
                AddDir(pics, VM.Pic_ShowWriter, VM.Song.Writer, VM.PicPathWriter);
                AddDir(pics, VM.Pic_ShowComposer, VM.Song.Composer, VM.PicPathComposer);
                AddDir(pics, VM.Pic_ShowPerformer, VM.Song.Performer, VM.PicPathPerformer);
            }

            return pics.Select(dir => new Tuple<string, string>(PickRandomFile(dir.Item1), dir.Item2));
        }

        private IEnumerable<VideoSource> GetAvaiableSongVideos()
        {
            var isChecked = new Func<eSongClipTypes, bool>((eSongClipTypes clipType) => VM.ClipTypes.Single(c => c.Type == clipType).IsChecked);

            var videos = new List<VideoSource>();

            if (VM.Song != null)
            {
                if (!string.IsNullOrWhiteSpace(VM.VideoPathSinger) && !string.IsNullOrWhiteSpace(VM.Song.Performer) && isChecked(eSongClipTypes.SongClips))
                {
                    var performerPath = Path.Combine(VM.VideoPathSinger, VM.Song.Performer);
                    var performerVideo = PickRandomFile(performerPath, $"*{VM.Song.Title}*");
                    if (!string.IsNullOrEmpty(performerVideo))
                        videos.Add(new VideoSource(performerVideo));

                    performerVideo = PickRandomFile(performerPath, $"*{VM.Song.HebTitle}*");
                    if (!string.IsNullOrEmpty(performerVideo))
                        videos.Add(new VideoSource(performerVideo));
                }

                if (!string.IsNullOrWhiteSpace(VM.VideoPathEvent) && !string.IsNullOrWhiteSpace(VM.Song.HebTitle) && isChecked(eSongClipTypes.Event))
                {
                    var eventVideo = PickRandomFile(Path.Combine(VM.VideoPathEvent, VM.Song.HebTitle));
                    if (!string.IsNullOrEmpty(eventVideo))
                        videos.Add(new VideoSource(eventVideo));
                }

                if (!string.IsNullOrWhiteSpace(VM.VideoPathDance) && !string.IsNullOrWhiteSpace(VM.Song.HebTitle) && isChecked(eSongClipTypes.Dance))
                {
                    var danceVideo = PickRandomFile(Path.Combine(VM.VideoPathDance, VM.Song.HebTitle));
                    if (!string.IsNullOrEmpty(danceVideo))
                        videos.Add(new VideoSource(danceVideo));
                }

                if (!string.IsNullOrEmpty(VM.Song.YouTubeSong) && isChecked(eSongClipTypes.YouTubeClip))
                    videos.Add(new YoutubeVideoSource(VM.Song.YouTubeSong));

                if (!string.IsNullOrEmpty(VM.Song.YouTubeDance) && isChecked(eSongClipTypes.YouTubeDance))
                    videos.Add(new YoutubeVideoSource(VM.Song.YouTubeDance));

            }

            if (videos.Count == 0 && !string.IsNullOrWhiteSpace(VM.VideoPathDefault))
            {
                var defaultVideo = PickRandomFile(Path.Combine(VM.VideoPathDefault));
                videos.Add(new VideoSource(defaultVideo));
            }
            //new VideoSource(p)
            return videos;
        }

        private void AddDir(List<Tuple<string, string>> pics, bool showFlag, string person, string path)
        {
            if (showFlag && !string.IsNullOrWhiteSpace(person))
            {
                var dir = GetPicPath(path, person);
                if (Directory.Exists(dir))
                    pics.Add(new Tuple<string, string>(dir, person));
            }
        }

        private static string GetPicPath(string dir, string person)
        {
            if (person == null || dir == null)
                return null;
            else
                return Path.Combine(dir, person);
        }

        private static Dictionary<string, Field> ReadSongXml(string filePath)
        {
            var fields = new Dictionary<string, Field>();
            var xml = File.ReadAllText(filePath);
            var doc = XDocument.Parse(xml);

            var song = doc.Element("Song");

            foreach (XElement current in song.Descendants("Field"))
            {
                var name = current.Attribute("Name").Value;
                var leng = current.Attribute("Language").Value;
                var value = current.Value;

                var field = new Field(name, value, leng);
                fields.Add(field.Name, field);
            }

            return fields;
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            m_PicLoopToken?.Cancel();
            m_WatchCloudDBToken?.Cancel();
            DeInitMongoDb();
            Properties.Settings.Default.Save();
            var vmJson = JsonConvert.SerializeObject(VM, Formatting.Indented);
            UserSettings.Set(c_MainVmSettingKey, vmJson);
        }

        private void DeInitMongoDb()
        {
            var clientId = m_BitnuaClient.Id;
            DB_Plays.DeleteMany(Builders<PlayEntry>.Filter.Eq("Client._id", clientId));
            DB_ActiveClients.DeleteOne(Builders<ClientInfo>.Filter.Eq("_id", clientId));
        }


        private Task<List<ClientInfo>> GetActiveClients() => DB_ActiveClients.Find(Builders<ClientInfo>.Filter.Empty).ToListAsync();

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        #region Font And Color pickers
        private void btnLeftPicHeaderFontPicker(object sender, RoutedEventArgs e) =>
            VM.LeftPicTitle.Font = PickFont(VM.LeftPicTitle.Font);

        private void btnLeftPicHeaderBackColorPicker(object sender, RoutedEventArgs e) =>
            VM.LeftPicTitle.BackColor = PickColor(VM.LeftPicTitle.BackColor);

        private void btnLeftPicHeaderForeColorPicker(object sender, RoutedEventArgs e) =>
            VM.LeftPicTitle.ForeColor = PickColor(VM.LeftPicTitle.ForeColor);

        private void btnLyricsFontPicker(object sender, RoutedEventArgs e) =>
            VM.Lyrics.Font = PickFont(VM.Lyrics.Font);

        private void btnLyricsBackColorPicker(object sender, RoutedEventArgs e) =>
            VM.Lyrics.BackColor = PickColor(VM.Lyrics.BackColor);

        private void btnLyricsForeColorPicker(object sender, RoutedEventArgs e) =>
            VM.Lyrics.ForeColor = PickColor(VM.Lyrics.ForeColor);

        private void btnBannerFontPicker(object sender, RoutedEventArgs e) =>
            VM.Banner.Font = PickFont(VM.Banner.Font);

        private void btnBannerBackColorPicker(object sender, RoutedEventArgs e) =>
            VM.Banner.BackColor = PickColor(VM.Banner.BackColor);

        private void btnBannerForeColorPicker(object sender, RoutedEventArgs e) =>
            VM.Banner.ForeColor = PickColor(VM.Banner.ForeColor);

        private static Font PickFont(Font font, int maxHeight = 72)
        {
            var fntDialog = new FontDialog()
            {
                FontMustExist = true,
                ShowEffects = false,
                MaxSize = maxHeight,
                Font = font
            };
            var dr = fntDialog.ShowDialog();
            if (dr != System.Windows.Forms.DialogResult.Cancel)
            {
                return fntDialog.Font;
            }

            return font;
        }

        private static Color PickColor(Color? i_color = null)
        {
            var color = i_color ?? Color.Black;
            var colorDialog = new ColorDialog() { AnyColor = true, Color = color };
            var dr = colorDialog.ShowDialog();
            if (dr != System.Windows.Forms.DialogResult.Cancel)
            {
                return colorDialog.Color;
            }

            return color;
        }
        #endregion

        public struct Field
        {
            public readonly string Leng;
            public readonly string Name;
            public readonly string Value;

            public Field(string name, string value, string leng)
            {
                Name = name;
                Value = value;
                Leng = leng;
            }
        }

        public static class Fields
        {
            public const string Title = "Title";
            public const string Heb_Title = "שם הריקוד";
            public const string Type = "Type";
            public const string Year = "שנה";
            public const string Tab2 = "לשונית משנה";
            public const string Performer = "מבצע";
            public const string LastPlayed = "Last Played";
            public const string LastMarked = "LastMarked";
            public const string TotalPlays = "TotalPlays";
            public const string SongChanged = "SongChanged";
            public const string SongAdded = "SongAdded";
            public const string SongLastPlayed = "SongLastPlayed";
            public const string ID = "ID";
            public const string Valid = "Valid";
            public const string FileModified = "FileModified";
            public const string TypeID = "TypeID";
            public const string FileName = "FileName";
            public const string FilePath = "FilePath";
            public const string FileExt = "FileExt";
            public const string SongLength = "Song Length";
            public const string Flag = "Flag";
            public const string Show = "Show";
            public const string Speed = "Speed";
            public const string Pitch = "Pitch";
            public const string Volume = "Volume";
            public const string FullPath = "FullPath";
            public const string id3_Title = "id3 Title";
            public const string id3_Artist = "id3_Artist";
            public const string id3_Album = "id3_Album";
            public const string id3_Year = "id3 Year";
            public const string id3_Genre = "id3 Genre";
            public const string id3_Comment = "id3_Comment";
            public const string Composer = "לחן";
            public const string Creator = "יוצר";
            public const string Writer = "משורר";
            public const string Lyrics = "Notes";
            public const string Lyrics2 = "רשימות";
            public const string YouTubeSong = "YouTubeSong";
            public const string YouTubeDance = "YouTubeDance";
        }

        private async void CurrentClientChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            m_WatchCloudDBToken?.Cancel();

            if (VM.CurrentClient != m_BitnuaClient)
            {
                m_WatchCloudDBToken = new CancellationTokenSource();
                await StartWatchCloudDBTask(m_WatchCloudDBToken.Token);
            }
        }

        private void addPresentationItemClick(object sender, RoutedEventArgs e)
        {
            ePresentationKinds kind = (ePresentationKinds)presentaionItemCB.SelectedItem;
            var item = PresentationItem.Create(kind, persentaionItemPath.Text);
            item.X = (int)(m_PresentationCanvas.Width - item.Width)/ 2;
            item.Y = (int)(m_PresentationCanvas.Height - item.Height) / 2;
            VM.PresentationVM.PresentationItems.Add(item);
        }

        private void pItemKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                OnDeletePresentationItem();
            }
        }

        private void OnDeletePresentationItem()
        {
            if (PVM.SelectedPresentationItem != null)
            {
                VM.PresentationVM.PresentationItems.Remove(PVM.SelectedPresentationItem);
            }
        }

        private void pItemPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var ctrl = ((FrameworkElement)sender);
            PVM.SelectedPresentationItem = (PresentationItem)ctrl.DataContext;
            //var listItem = (System.Windows.Controls.ListViewItem)sender;

            //listItem.pa

            //listItem.IsSelected = !listItem.IsSelected;
        }


        private MainWindowVM m_MainWindowVM;
        private Canvas m_PresentationCanvas;

        public MainWindowVM PVM => m_MainWindowVM ?? (m_MainWindowVM = new MainWindowVM());

        public class MainWindowVM: ViewModelBase
        {

            private PresentationItem m_SelectedPresentationItem;

            public PresentationItem SelectedPresentationItem
            {
                get { return m_SelectedPresentationItem; }
                set { m_SelectedPresentationItem = value; OnPropertyChanged(() => SelectedPresentationItem); }
            }
        }

        private void btnPresentationPathBrowse(object sender, RoutedEventArgs e)
        {
            var selectedType = (ePresentationKinds)presentaionItemCB.SelectedItem;
            switch (selectedType)
            {
                case ePresentationKinds.PictureList:
                case ePresentationKinds.VideoList:
                    using (var fileDialog = new CommonOpenFileDialog()
                    {
                        IsFolderPicker = true,
                        EnsureFileExists = false
                    })
                    {
                        if (fileDialog.ShowDialog() == CommonFileDialogResult.Ok)
                            persentaionItemPath.Text = fileDialog.FileName;
                    }
                    break;
                case ePresentationKinds.Picture:
                case ePresentationKinds.Video:
                    using (var fileDialog = new OpenFileDialog())
                    {
                        if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            persentaionItemPath.Text = fileDialog.FileName;
                    }
                    break;
                case ePresentationKinds.YoutubeVideo:
                    break;
                default:
                    break;
            }
        }

        private void designerCanvasLoaded(object sender, RoutedEventArgs e)
        {
            m_PresentationCanvas = sender as Canvas;
        }
    }

    public class ColorToSolidColorBrushValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (null == value)
            {
                return null;
            }
            Type type = value.GetType();

            if (value is System.Windows.Media.Color)
                return new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)value);
            else if (value is System.Drawing.Color)
            {
                var sdColor = (System.Drawing.Color)value;
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(sdColor.A, sdColor.R, sdColor.G, sdColor.B));
            }

            throw new InvalidOperationException("Unsupported type [" + type.Name + "]");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // If necessary, here you can convert back. Check if which brush it is (if its one),
            // get its Color-value and return it.

            throw new NotImplementedException();
        }
    }

    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            return source.Shuffle(new Random());
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rng)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (rng == null) throw new ArgumentNullException("rng");

            return source.ShuffleIterator(rng);
        }

        private static IEnumerable<T> ShuffleIterator<T>(
            this IEnumerable<T> source, Random rng)
        {
            List<T> buffer = source.ToList();
            for (int i = 0; i < buffer.Count; i++)
            {
                int j = rng.Next(i, buffer.Count);
                yield return buffer[j];

                buffer[j] = buffer[i];
            }
        }
    }
}
