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
using BitnuaVideoPlayer.Properties;
using LibVLCSharp.Shared;
using System.Runtime.CompilerServices;
using CefSharp;
using CefSharp.Wpf;
using System.Reflection;

namespace BitnuaVideoPlayer
{

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string c_MainVmSettingKey = "MainVmJson";
        private static Random s_Random = new Random();


        private DispatcherTimer m_timer;
        private int m_timer_ticks;
        private MainWindow m_MainWindow;
        public PresentaionWindow m_PlayerWindow;

        private AppUpdateManager updateManager;
        private FileSystemWatcher m_FileSysWatcher;
        private MongoClient m_MongoClient;

        private IMongoCollection<PlayEntry> DB_Plays;
        private IMongoCollection<ClientInfo> DB_ActiveClients;
        private CancellationTokenSource m_WatchCloudDBToken;
        private ClientInfo m_BitnuaClient;
        private Song m_Song;
        private IEnumerator<Tuple<string, string>> m_songPics;
        private IEnumerator<Tuple<string, string>> m_leftPics;

        public MainViewModel VM { get; set; }
        public static App Instance { get; private set; }
        public LibVLC LibVLC { get; internal set; }

        public App()
        {
            try
            {
                DispatcherUnhandledException += Current_DispatcherUnhandledException;
                Instance = this;
                InitVLC();

                //Add Custom assembly resolver
                AppDomain.CurrentDomain.AssemblyResolve += Resolver;

                InitializeCefSharp();

                // Setup Quick Converter.
                // Add the System namespace so we can use primitive types (i.e. int, etc.).
                QuickConverter.EquationTokenizer.AddNamespace(typeof(object));
                // Add the System.Windows namespace so we can use Visibility.Collapsed, etc.
                QuickConverter.EquationTokenizer.AddNamespace(typeof(System.Windows.Visibility));
            }
            catch (Exception ex)
            {
                string log = LogException(ex);
                File.WriteAllText("crashReport.txt", log);
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void InitializeCefSharp()
        {
            var settings = new CefSettings();
            
            // Set BrowserSubProcessPath based on app bitness at runtime
            settings.BrowserSubprocessPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                                                   Environment.Is64BitProcess ? "x64" : "x86",
                                                   "CefSharp.BrowserSubprocess.exe");

            // Make sure you set performDependencyCheck false
            Cef.Initialize(settings, performDependencyCheck: false, browserProcessHandler: null);
        }

        // Will attempt to load missing assembly from either x86 or x64 subdir
        // Required by CefSharp to load the unmanaged dependencies when running using AnyCPU
        private static Assembly Resolver(object sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith("CefSharp"))
            {
                string assemblyName = args.Name.Split(new[] { ',' }, 2)[0] + ".dll";
                string archSpecificPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                                                       Environment.Is64BitProcess ? "x64" : "x86",
                                                       assemblyName);

                return File.Exists(archSpecificPath)
                           ? Assembly.LoadFile(archSpecificPath)
                           : null;
            }

            return null;
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Verify license for this app
            VerifyLicense();
            
            base.OnStartup(e);

            InitUpdateManager();
            await InitAll();

            MainWindow = m_MainWindow = new MainWindow()
            {
                VM = VM,
                DataContext = VM,
                Topmost = true,
                Height = Settings.Default.rmtCtrl.Height
            };
            MainWindow.Closed += M_MainWindow_Closed;

            m_MainWindow.Show();
        }

        private void InitVLC()
        {
            Core.Initialize();
            LibVLC = new LibVLC("--noaudio");
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

            m_timer?.Stop();
            m_WatchCloudDBToken?.Cancel();

            BitnuaVideoPlayer.Properties.Settings.Default.Save();
            var vmJson = JsonConvert.SerializeObject(VM, Formatting.Indented);
            UserSettings.Set(c_MainVmSettingKey, vmJson);

            if (!VM.OfflineMode)
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
            {
#if DEBUG == false
                throw new DirectoryNotFoundException($"Amps dir not found in {VM.WatchDir}");
#endif
            }
            else
            {
                m_FileSysWatcher = new FileSystemWatcher()
                {
                    Path = VM.WatchDir,
                    NotifyFilter = NotifyFilters.LastWrite,
                    Filter = "*.*",
                    EnableRaisingEvents = true
                };
                m_FileSysWatcher.Changed += new FileSystemEventHandler(OnWatchDirChanged);
            }

            RegisterBannerPicsChanged();
            await ShowLastSong();

            if (!VM.OfflineMode)
                await ConnectDb();
        }

        private async Task ConnectDb()
        {
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
            if (!Directory.Exists(VM.WatchDir))
                return;

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
                await UpdateVM();
                //if (VM.SelectedLayout == eLayoutModes.Default)
                //{
                //}
                //else if (VM.SelectedLayout == eLayoutModes.Presentation)
                //{
                //    //m_PicLoopToken?.Cancel();
                //}
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
                     e.PropertyName == nameof(VM.Pic_ShowSongName) ||
                     e.PropertyName == nameof(VM.Pic_ShowEvent) ||
                     e.PropertyName == nameof(VM.ClipTypes) ||
                     e.PropertyName == nameof(VM.SelectedVideoMode) ||
                     e.PropertyName == nameof(VM.SelectedPicMode))
            {
                await UpdateVM();
            }
        }

        private async Task InitMongoDb()
        {
            var url = new MongoUrl("mongodb+srv://svc_bitnua:zIGloC1lGQ3uw24d@bitnua-vplayer.b4rhw.mongodb.net/bitnua_vplayer?retryWrites=true&w=majority");
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
                DB_ActiveClients.DeleteMany(Builders<ClientInfo>.Filter.Eq("Name", m_BitnuaClient.Name));
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
            if (fields.TryGetValue(Fields.Type, out field))
                song.Type = field.Value;
            if (fields.TryGetValue(Fields.Performer, out field))
                song.Heb_Performer = field.Value;
            if (fields.TryGetValue(Fields.Eng_Performer, out field))
                song.Eng_Performer = field.Value;
            if (fields.TryGetValue(Fields.Creator, out field))
                song.Heb_Creator = field.Value;
            if (fields.TryGetValue(Fields.Eng_Creator, out field))
                song.Eng_Creator = field.Value;
            if (fields.TryGetValue(Fields.Composer, out field))
                song.Heb_Composer = field.Value;
            if (fields.TryGetValue(Fields.Eng_Composer, out field))
                song.Eng_Composer = field.Value;
            if (fields.TryGetValue(Fields.Writer, out field))
                song.Heb_Writer = field.Value;
            if (fields.TryGetValue(Fields.Eng_Writer, out field))
                song.Eng_Writer = field.Value;


            if (fields.TryGetValue(Fields.Year, out field) && int.TryParse(field.Value, out year))
                song.Year = year;

            if (fields.TryGetValue(Fields.Lyrics, out field) && !string.IsNullOrWhiteSpace(field.Value))
                song.Lyrics = field.Value;
            else if (fields.TryGetValue(Fields.Lyrics2, out field) && !string.IsNullOrWhiteSpace(field.Value))
                song.Lyrics = field.Value;


            if (fields.TryGetValue(Fields.EventName, out field) && !string.IsNullOrWhiteSpace(field.Value))
                song.EventName = field.Value;
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
                var song = ReadSongInfo(filePath);

                if (VM.CurrentClient == m_BitnuaClient)
                    await UpdateVM(song);

                if (DB_Plays != null)
                    await DB_Plays.InsertOneAsync(new PlayEntry() { Song = song, Client = m_BitnuaClient });
            }
            catch (Exception)
            {
            }
        }

        private Task UpdateVM() => UpdateVM(m_Song);
        private async Task UpdateVM(Song song)
        {
            m_timer?.Stop();
            if (song == null)
                return;

            m_Song = VM.Song = song;
            VM.ArtistPicSource = GetArtistPic(VM);
            VM.PicSources = GetAvaiableSongPics(VM).Shuffle(s_Random);

            if (!string.IsNullOrWhiteSpace(VM.FlayerDir))
            {
                VM.Flyerfiles = Directory.EnumerateFiles(VM.FlayerDir, "*.*", SearchOption.AllDirectories).Shuffle(s_Random);
            }

            StartLeftPicTimer();
            var songVideos = GetAvaiableSongVideos(VM).Shuffle(s_Random).ToList();
            if (VM.SelectedLayout == eLayoutModes.Default)
            {
                VM.DefaultLayout_VideoItem.VideoSources = songVideos;
            }
            else
            {
                foreach (var item in VM.PresentationVM.PresentationItems.OfType<AmpsPresentationItem>())
                {
                    item.VideoSources = songVideos;
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

        private static string GetArtistPic(MainViewModel VM)
        {
            string res = null;
            if (VM.Song != null)
            {
                var path = VM.PicPathPerformer;
                var person = VM.Song.Heb_Performer;
                var dir = GetPicPath(path, person);
                if (Directory.Exists(dir))
                {
                    var files = Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories);
                    res = files.FirstOrDefault();
                }
            }

            return res;
        }

        private Task<List<ClientInfo>> GetActiveClients() => DB_ActiveClients.Find(Builders<ClientInfo>.Filter.Empty).ToListAsync();


        private void StartLeftPicTimer()
        {

            VM.ArtistPicSource = null;
            VM.LeftPicSource = null;
            VM.LeftPicTitle.Text = null;
            VM.RTL = !(VM.ShowEng && VM.Song.HasEng && !VM.Song.HasHeb);


            m_songPics?.Dispose();
            m_leftPics?.Dispose();

            m_songPics = IterateInLoop(GetAvaiableSongPics(VM)).GetEnumerator();
            m_leftPics = IterateNextLeftPic(VM).GetEnumerator();

            m_timer_ticks = 0;
            if (m_timer is null)
            {
                m_timer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(Math.Max(VM.LeftPicDelay, 3000)) };
                m_timer.Tick += M_timer_Tick;
            }

            picsTimerTick();
            m_timer.IsEnabled = true;

        }

        private void M_timer_Tick(object sender, EventArgs e)
        {
            picsTimerTick();
        }

        private void picsTimerTick()
        {
            if (VM.Song != null)
            {
                if (VM.ShowEng && VM.ShowHeb && m_timer_ticks % VM.LangTicks == 0 && VM.Song.HasEng && VM.Song.HasHeb)
                    VM.RTL = !VM.RTL;

                if (m_songPics.MoveNext())
                    VM.ArtistPicSource = m_songPics.Current.Item1;


                if (VM.SelectedPicMode != ePicMode.Lyrics && m_leftPics.MoveNext())
                {
                    var fileNTitle = m_leftPics.Current;
                    VM.LeftPicSource = fileNTitle.Item1;
                    VM.LeftPicTitle.Text = fileNTitle.Item2;
                }
            }

            m_timer_ticks++;
        }

        private static IEnumerable<T> IterateInLoop<T>(IEnumerable<T> source, CancellationToken? token = null)
        {
            bool empty = true;
            while (token is null || !token.Value.IsCancellationRequested)
            {
                using (var items = source.GetEnumerator())
                    while (items.MoveNext() && (token is null || !token.Value.IsCancellationRequested))
                    {
                        empty = false;
                        yield return items.Current;
                    }

                if (empty)
                    yield break;
            }
        }
        private static IEnumerable<Tuple<string, string>> IterateNextLeftPic(MainViewModel VM, CancellationToken? token = null)
        {
            while (token is null || !token.Value.IsCancellationRequested)
            {
                bool empty = true;
                if (VM.SelectedPicMode == ePicMode.Flyer)
                {
                    using (var flyerPics = VM.Flyerfiles.GetEnumerator())
                        while (flyerPics.MoveNext() && (token is null || !token.Value.IsCancellationRequested))
                        {
                            empty = false;
                            yield return new Tuple<string, string>(flyerPics.Current, null);
                        }
                }
                else
                {
                    using (var songPics = VM.PicSources.GetEnumerator())
                        while (songPics.MoveNext() && (token is null || !token.Value.IsCancellationRequested))
                        {
                            empty = false;
                            yield return new Tuple<string, string>(songPics.Current.Item1, songPics.Current.Item2);
                        }
                }

                if (empty)
                    yield break;
            }
        }

        private static IEnumerable<Tuple<string, string>> GetAvaiableSongPics(MainViewModel VM, bool addDefault = true)
        {
            var pics = new List<Tuple<string, string>>();

            if (VM.Song != null)
            {
                var dir = GetDir(VM.Pic_ShowCreator, VM.Song.Heb_Creator, VM.PicPathCreator);
                if (dir != null) pics.Add(new Tuple<string, string>(dir, VM.Song.Creator));

                dir = GetDir(VM.Pic_ShowWriter, VM.Song.Heb_Writer, VM.PicPathWriter);
                if (dir != null) pics.Add(new Tuple<string, string>(dir, VM.Song.Writer));

                dir = GetDir(VM.Pic_ShowComposer, VM.Song.Heb_Composer, VM.PicPathComposer);
                if (dir != null) pics.Add(new Tuple<string, string>(dir, VM.Song.Composer));

                dir = GetDir(VM.Pic_ShowPerformer, VM.Song.Heb_Performer, VM.PicPathPerformer);
                if (dir != null) pics.Add(new Tuple<string, string>(dir, VM.Song.Performer));

                dir = GetDir(VM.Pic_ShowEvent, VM.Song.EventName, VM.PicPathEventName);
                if (dir != null) pics.Add(new Tuple<string, string>(dir, VM.Song.EventName));

                dir = GetDir(VM.Pic_ShowSongName, VM.Song.HebTitle, VM.PicPathSongName);
                if (dir != null) pics.Add(new Tuple<string, string>(dir, VM.Song.HebTitle));
            }

            if (addDefault && (pics.Count == 0 || VM.Pic_ShowDefault))
                pics.Add(new Tuple<string, string>(VM.PicPathDefault, null));

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
                    if (!string.IsNullOrWhiteSpace(VM.VideoPathSinger) && !string.IsNullOrWhiteSpace(VM.Song.Heb_Performer) && isChecked(eSongClipTypes.SongClips))
                    {
                        var performerPath = Path.Combine(VM.VideoPathSinger, VM.Song.Heb_Performer);
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

        private static string GetDir(bool showFlag, string person, string path)
        {
            if (showFlag && !string.IsNullOrWhiteSpace(person))
            {
                var dir = GetPicPath(path, person);
                if (Directory.Exists(dir))
                    return dir;
            }

            return null;
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
        public const string Lyrics = "Notes";
        public const string Lyrics2 = "רשימות";
        public const string YouTubeSong = "YouTubeSong";
        public const string YouTubeDance = "YouTubeDance";
        public const string EventName = "אירועים";


        public const string Composer = "לחן";
        public const string Creator = "יוצר";
        public const string Writer = "משורר";
        public const string Performer = "מבצע";

        public const string Eng_Composer = "Music By";
        public const string Eng_Creator = "Choreographer";
        public const string Eng_Writer = "Lyrics By";
        public const string Eng_Performer = "Performer";
    }
}
