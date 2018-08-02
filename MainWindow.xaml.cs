using BitnuaVideoPlayer.ViewModels;
using CefSharp;
using CefSharp.Wpf;
using ColorFont;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml.Linq;

namespace BitnuaVideoPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string c_ConfFilePath = "conf.json";
        private static Random m_Random = new Random();
        private PresentaionWindow m_PlayerWindow;
        private FileSystemWatcher m_FileSysWatcher;
        private ClientInfo m_BitnuaClient;
        private IEnumerable<Tuple<string, string>> m_picSources;
        private IEnumerable<string> m_VideoSources;
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
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            MouseDown += Window_MouseDown;
            App.Current.Exit += App_Exit;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await InitAll();
        }

        private async Task InitAll()
        {
            DataContext = VM = MainViewModel.Create(c_ConfFilePath);
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
            if (fields.TryGetValue(Fields.Lyrics, out field))
                song.Lyrics = field.Value;
            return song;
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
                await DB_Plays.InsertOneAsync(new PlayEntry() { Song = song, Client = m_BitnuaClient });

                if (VM.CurrentClient == m_BitnuaClient)
                    await UpdateVM(song);
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
            m_VideoSources = GetAvaiableSongVideos().Shuffle(m_Random);
            m_Flyerfiles = Directory.EnumerateFiles(VM.FlayerDir, "*.*", SearchOption.AllDirectories).Shuffle(m_Random);

            m_PicLoopToken = new CancellationTokenSource();

            if (VM.SelectedLayout == eLayoutModes.Default)
            {
                var t = StartLeftPicTask(m_PicLoopToken.Token);
                var t1 = StartRightVideoTask(m_PicLoopToken.Token);
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

        private async Task StartRightVideoTask(CancellationToken token)
        {
            await Task.Delay(100);
            var playTime = DateTime.Now;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (VM.Song != null)
                    {
                        string currVideo = null;
                        foreach (var videoPath in IterateNextVideo(token))
                        {
                            if (videoPath != currVideo)
                            {
                                currVideo = videoPath;
                                long time = CalcLastPlayTime(playTime, videoPath);
                                VM.DefaultLayout_VideoItem = null;
                                VM.DefaultLayout_VideoItem = new VideoItem() { VideoSource = new VideoSource(videoPath, time) };
                            }

                            await Task.Delay(Math.Max(VM.LeftPicDelay, 10000), token).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private static long CalcLastPlayTime(DateTime playTime, string videoPath)
        {
            long time = 0;
            TimeSpan videoDuration;
            if (GetDuration(videoPath, out videoDuration))
            {
                var diff = (DateTime.Now - playTime).TotalMilliseconds;
                time = (long)(diff % TimeSpan.FromTicks(videoDuration.Ticks).TotalMilliseconds);
            }

            return time;
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

        private IEnumerable<string> IterateNextVideo(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                    using (var videos = m_VideoSources.GetEnumerator())
                        while (videos.MoveNext() && !token.IsCancellationRequested)
                            yield return videos.Current;
            }
        }

        private async Task<string> GetArtistPic()
        {
            await Task.Delay(50);
            if (VM.Song != null)
            {
                var path = VM.PicPathPerformer;
                var person = VM.Song.Performer;
                var dir = GetPicPath(path, person);
                if (Directory.Exists(dir))
                {
                    var files = Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories);
                    return files.First();
                }
            }

            return null;
        }

        private static string PickRandomFile(string dir, string searchPattern = null)
        {
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

        private IEnumerable<string> GetAvaiableSongVideos()
        {
            var videos = new List<string>();

            if (VM.Song != null)
            {
                var performerPath = Path.Combine(VM.VideoPathSinger, VM.Song.Performer);
                var performerVideo = PickRandomFile(performerPath, $"*{VM.Song.Title}*");
                if (!string.IsNullOrEmpty(performerVideo))
                    videos.Add(performerVideo);

                performerVideo = PickRandomFile(performerPath, $"*{VM.Song.HebTitle}*");
                if (!string.IsNullOrEmpty(performerVideo))
                    videos.Add(performerVideo);

                var eventVideo = PickRandomFile(Path.Combine(VM.VideoPathEvent, VM.Song.HebTitle));
                var danceVideo = PickRandomFile(Path.Combine(VM.VideoPathDance, VM.Song.HebTitle));

                if (!string.IsNullOrEmpty(eventVideo))
                    videos.Add(eventVideo);
                if (!string.IsNullOrEmpty(danceVideo))
                    videos.Add(danceVideo);
            }

            if (videos.Count == 0)
            {
                var defaultVideo = PickRandomFile(Path.Combine(VM.VideoPathDefault));
                videos.Add(defaultVideo);
            }

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

        private async Task<string> GetNextVideoPath()
        {
            await Task.Delay(50);

            string dir, file = null;

            if (VM.SelectedVideoMode == ViewModels.eVideoMode.VideoDir1)
                dir = VM.VideoPath1;
            else if (VM.SelectedVideoMode == ViewModels.eVideoMode.VideoDir2)
                dir = VM.VideoPath2;
            else //if (VM.SelectedVideoMode == ViewModels.eVideoMode.Clip)
            {
                var selectedClips = VM.ClipsNums.Where(cb => cb.IsChecked).ToArray();
                dir = selectedClips[m_Random.Next(selectedClips.Length)].Path;
            }

            var files = Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories);
            file = files.First();
            return file;
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
            VM.SaveTo(c_ConfFilePath);
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
            VM.Banner.Font = PickFont(VM.Banner.Font, (int)m_PlayerWindow.browser.Height);

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

        private static Color PickColor(Color color)
        {
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
        }

        public static bool GetDuration(string filename, out TimeSpan duration)
        {
            try
            {
                using (var shell = ShellObject.FromParsingName(filename))
                {
                    IShellProperty prop = shell.Properties.System.Media.Duration;
                    var t = (ulong)prop.ValueAsObject;
                    duration = TimeSpan.FromTicks((long)t);
                }
                return true;
            }
            catch (Exception)
            {
                duration = new TimeSpan();
                return false;
            }
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
