using BitnuaVideoPlayer.ViewModels;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Squirrel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Linq;
using System.Windows.Navigation;

namespace BitnuaVideoPlayer
{

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string c_MainVmSettingKey = "MainVmJson";
        private static Random m_Random = new Random();


        private MainWindow m_MainWindow;
        public PresentaionWindow m_PlayerWindow;

        private AppUpdateManager updateManager;
        private FileSystemWatcher m_FileSysWatcher;
        private MongoClient m_MongoClient;

        private IMongoCollection<PlayEntry> DB_Plays;
        private IMongoCollection<ClientInfo> DB_ActiveClients;
        private CancellationTokenSource m_PicLoopToken;
        private CancellationTokenSource m_WatchCloudDBToken;
        private ClientInfo m_BitnuaClient;
        private Song m_Song;

        public MainViewModel VM { get; set; }
        public static App Instance { get; private set; }

        public App()
        {
            DispatcherUnhandledException += Current_DispatcherUnhandledException;
            Instance = this;
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Verify license for this app
            VerifyLicense();

            base.OnStartup(e);

            InitUpdateManager();
            await InitAll();

            MainWindow = m_MainWindow = new MainWindow();
            m_MainWindow.DataContext = m_MainWindow.VM = VM;
            m_MainWindow.Closed += M_MainWindow_Closed;
            m_MainWindow.Topmost = true;

            m_PlayerWindow = new PresentaionWindow() { DataContext = VM };
            m_PlayerWindow.WindowStyle = WindowStyle.None;

#if DEBUG
            m_PlayerWindow.Topmost = false;
#else
            m_PlayerWindow.Topmost = true;
#endif

            m_MainWindow.Show();
            m_PlayerWindow.Show();
        }

        private static void VerifyLicense()
        {
            try
            {
                MyAppActivation.ValidateActivation();
            }
            catch (Exception ex)
            {
                ShowExceptionMsgBox(ex);
                Environment.Exit(0);
            }
        }

        public void ShowPresentaionWindow()
        {
            m_PlayerWindow.Show();
            UpdateVM();
        }
        public void HidePresentaionWindow()
        {
            m_PlayerWindow.Hide();
        }

        private void M_MainWindow_Closed(object sender, EventArgs e) => Shutdown();

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            m_PicLoopToken?.Cancel();
            m_WatchCloudDBToken?.Cancel();
            BitnuaVideoPlayer.Properties.Settings.Default.Save();
            var vmJson = JsonConvert.SerializeObject(VM, Formatting.Indented);
            UserSettings.Set(c_MainVmSettingKey, vmJson);
            DeInitMongoDb();
        }

        private void InitUpdateManager()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            // Use SecurityProtocolType.Ssl3 if needed for compatibility reasons
            updateManager = AppUpdateManager.Instance;
        }

        private async Task InitAll()
        {
            string vmJson = Convert.ToString(UserSettings.Get(c_MainVmSettingKey));
            VM = MainViewModel.Create(vmJson);
            VM.PropertyChanged += VM_PropertyChanged;
            VM.CurrentClient = m_BitnuaClient = new ClientInfo() { Name = VM.ClientName };

            if (!Directory.Exists(VM.WatchDir))
                throw new DirectoryNotFoundException($"Amps dir not found in {VM.WatchDir}");

            m_FileSysWatcher = new FileSystemWatcher()
                {
                    Path = VM.WatchDir,
                    NotifyFilter = NotifyFilters.LastWrite,
                    Filter = "*.*",
                    EnableRaisingEvents = true
                };
                m_FileSysWatcher.Changed += new FileSystemEventHandler(OnWatchDirChanged);
                RegisterBannerPicsChanged();
                await ShowLastSong();


            if (CheckForInternetConnection())
            {
                await InitMongoDb();
            }
            else
            {
                m_WatchCloudDBToken = new CancellationTokenSource();
#pragma warning disable 
                CheckConnectionLoop(m_WatchCloudDBToken.Token).ContinueWith(t =>
                 {
                     if (t.IsCompleted)
                         return InitMongoDb();
                     else
                         return Task.FromCanceled(m_WatchCloudDBToken.Token);
                 });
            }
#pragma warning restore
        }

        public static async Task CheckConnectionLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested && !CheckForInternetConnection())
            {
                await Task.Delay(1000);
            }
        }

        public static bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("http://google.com"))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private void RegisterBannerPicsChanged()
        {
            if (VM?.Banner != null)
            {
                VM.Banner.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(VM.Banner.PicsPath))
                        ReadBannerPics();
                };
            }

            ReadBannerPics();
        }

        private void ReadBannerPics()
        {
            VM.Banner.Pics = null;
            if (!string.IsNullOrEmpty(VM?.Banner?.PicsPath) && Directory.Exists(VM.Banner.PicsPath))
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
            else if (e.PropertyName == nameof(VM.CurrentClient))
            {
                CurrentClientChanged();
            }
            else if (e.PropertyName == nameof(VM.Pic_ShowComposer) ||
                     e.PropertyName == nameof(VM.Pic_ShowCreator) ||
                     e.PropertyName == nameof(VM.Pic_ShowPerformer) ||
                     e.PropertyName == nameof(VM.Pic_ShowWriter) ||
                     e.PropertyName == nameof(VM.Pic_ShowDefault) ||
                     e.PropertyName == nameof(VM.ClipTypes) ||
                     e.PropertyName == nameof(VM.SongYoutubeVideos) ||
                     e.PropertyName == nameof(VM.SelectedVideoMode) ||
                     e.PropertyName == nameof(VM.SelectedPicMode))
            {
                await UpdateVM();
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

            VM.ActiveClients = await GetActiveClients();
        }

        private void DeInitMongoDb()
        {
            try
            {
                var clientId = m_BitnuaClient.Id;
                DB_Plays.DeleteMany(Builders<PlayEntry>.Filter.Eq("Client._id", clientId));
                DB_ActiveClients.DeleteOne(Builders<ClientInfo>.Filter.Eq("_id", clientId));
            }
            catch (Exception)
            {
            }
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
            VM.ArtistPicSource = await GetArtistPic(VM);
            VM.PicSources = GetAvaiableSongPics(VM).Shuffle(m_Random);
            VM.DefaultLayout_VideoItem.VideoSources = GetAvaiableSongVideos(VM).Shuffle(m_Random).ToList();

            if (!string.IsNullOrWhiteSpace(VM.FlayerDir))
            {
                VM.Flyerfiles = Directory.EnumerateFiles(VM.FlayerDir, "*.*", SearchOption.AllDirectories).Shuffle(m_Random);
            }

            m_PicLoopToken = new CancellationTokenSource();

            if (VM.SelectedLayout == eLayoutModes.Default)
            {
                var t = Task.Run(() => StartLeftPicTask(m_PicLoopToken.Token));
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
                    LogException(ex);
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

        private static IEnumerable<string> GetDirFiles(string dir, string searchPattern = null, bool pickAnyIfNotMatchingPattern = false)
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                return null;

            try
            {
                var files = Directory.EnumerateFiles(dir, searchPattern ?? "*.*", SearchOption.AllDirectories);
                if (pickAnyIfNotMatchingPattern && searchPattern != null && !files.Any())
                    files = Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories);

                return files;
            }
            catch (Exception)
            {
            }

            return null;
        }

        private static string PickRandomFile(string dir, string searchPattern = null, bool pickAnyIfNotMatchingPattern = false)
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                return null;

            string file = null;
            try
            {
                var files = Directory.EnumerateFiles(dir, searchPattern ?? "*.*", SearchOption.AllDirectories);
                if (pickAnyIfNotMatchingPattern && !files.Any())
                    files = Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories);

                file = files.Shuffle().FirstOrDefault();
            }
            catch (Exception)
            {
            }

            return file;
        }

        private static Task<string> GetArtistPic(MainViewModel VM)
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

        private Task<List<ClientInfo>> GetActiveClients() => DB_ActiveClients.Find(Builders<ClientInfo>.Filter.Empty).ToListAsync();


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
                        foreach (var fileNTitle in IterateNextLeftPic(VM, token))
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

        private static IEnumerable<Tuple<string, string>> IterateNextLeftPic(MainViewModel VM, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (VM.SelectedPicMode == ViewModels.ePicMode.Flyer)
                {
                    using (var flyerPics = VM.Flyerfiles.GetEnumerator())
                        while (flyerPics.MoveNext() && !token.IsCancellationRequested)
                            yield return new Tuple<string, string>(flyerPics.Current, null);
                }
                else
                {
                    using (var songPics = VM.PicSources.GetEnumerator())
                        while (songPics.MoveNext() && !token.IsCancellationRequested)
                            yield return songPics.Current;
                }
            }
        }

        private static IEnumerable<Tuple<string, string>> GetAvaiableSongPics(MainViewModel VM)
        {
            var pics = new List<Tuple<string, string>>();

            if (VM.Song != null)
            {
                AddDir(pics, VM.Pic_ShowCreator, VM.Song.Creator, VM.PicPathCreator);
                AddDir(pics, VM.Pic_ShowWriter, VM.Song.Writer, VM.PicPathWriter);
                AddDir(pics, VM.Pic_ShowComposer, VM.Song.Composer, VM.PicPathComposer);
                AddDir(pics, VM.Pic_ShowPerformer, VM.Song.Performer, VM.PicPathPerformer);
            }

            if (pics.Count == 0 || VM.Pic_ShowDefault)
                pics.Add(new Tuple<string, string>(VM.PicPathDefault, string.Empty));

            return pics.Select(dir => new Tuple<string, string>(PickRandomFile(dir.Item1), dir.Item2));
        }

        private static IEnumerable<VideoSource> GetAvaiableSongVideos(MainViewModel VM)
        {
            var isChecked = new Func<eSongClipTypes, bool>((eSongClipTypes clipType) => VM.ClipTypes.Single(c => c.Type == clipType).IsChecked);

            var videos = new List<VideoSource>();

            if (VM.Song != null)
            {
                if (VM.SelectedVideoMode == eVideoMode.Clip)
                {
                    if (!string.IsNullOrWhiteSpace(VM.VideoPathSinger) && !string.IsNullOrWhiteSpace(VM.Song.Performer) && isChecked(eSongClipTypes.SongClips))
                    {
                        var performerPath = Path.Combine(VM.VideoPathSinger, VM.Song.Performer);
                        var preformerFiles = GetDirFiles(performerPath, $"*{VM.Song.Title}*", false);
                        if (preformerFiles != null && performerPath.Any())
                            videos.AddRange(preformerFiles.Select(f => new VideoSource(f)).Shuffle());

                        preformerFiles = GetDirFiles(performerPath, $"*{VM.Song.HebTitle}*", true);
                        if (preformerFiles != null && performerPath.Any())
                            videos.AddRange(preformerFiles.Select(f => new VideoSource(f)).Shuffle());

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
                }
                else if (VM.SelectedVideoMode == eVideoMode.Youtube)
                {
                    if (!string.IsNullOrEmpty(VM.Song.YouTubeSong) && VM.SongYoutubeVideos.Single(c => c.Type == eSongClipTypes.YouTubeClip).IsChecked)
                        videos.Add(new YoutubeVideoSource(VM.Song.YouTubeSong));

                    if (!string.IsNullOrEmpty(VM.Song.YouTubeDance) && VM.SongYoutubeVideos.Single(c => c.Type == eSongClipTypes.YouTubeDance).IsChecked)
                        videos.Add(new YoutubeVideoSource(VM.Song.YouTubeDance));
                }
                else if (VM.SelectedVideoMode == eVideoMode.VideoDir1 && !string.IsNullOrEmpty(VM.VideoPath1))
                {
                    var dirVideos = GetAllVideos((VM.VideoPath1));
                    if (dirVideos != null && dirVideos.Any())
                        videos.AddRange(dirVideos);
                }
                else if (VM.SelectedVideoMode == eVideoMode.VideoDir2 && !string.IsNullOrEmpty(VM.VideoPath2))
                {
                    var dirVideos = GetAllVideos((VM.VideoPath2));
                    if (dirVideos != null && dirVideos.Any())
                        videos.AddRange(dirVideos);
                }
            }

            if (videos.Count == 0 && !string.IsNullOrWhiteSpace(VM.VideoPathDefault))
            {
                var defaultVideo = PickRandomFile(Path.Combine(VM.VideoPathDefault));
                videos.Add(new VideoSource(defaultVideo));
            }

            return videos;
        }

        private static IEnumerable<VideoSource> GetAllVideos(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                return null;

            try
            {
                var files = Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories);
                return files.Select(f => new VideoSource(f)).Shuffle();
            }
            catch (Exception)
            {
            }

            return null;
        }
        private static void AddDir(List<Tuple<string, string>> pics, bool showFlag, string person, string path)
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

        private async void CurrentClientChanged()
        {
            m_WatchCloudDBToken?.Cancel();

            if (VM.CurrentClient != m_BitnuaClient)
            {
                m_WatchCloudDBToken = new CancellationTokenSource();
                await StartWatchCloudDBTask(m_WatchCloudDBToken.Token);
            }
        }

        private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            var ex = e.Exception;
            ShowExceptionMsgBox(ex);
        }

        private static void ShowExceptionMsgBox(Exception ex)
        {
            var msg = LogException(ex);
            System.Windows.MessageBox.Show(msg, "Exception!", MessageBoxButton.OK, MessageBoxImage.Error);
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

    }

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

}
