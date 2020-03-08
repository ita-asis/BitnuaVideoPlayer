using System.Drawing;
using System;
using System.Configuration;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;
using BitnuaVideoPlayer.ViewModels;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using BitnuaVideoPlayer.UI.Converters;
using Newtonsoft.Json.Converters;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Windows;
using System.Linq;
using System.Collections.Specialized;

namespace BitnuaVideoPlayer
{
    [Serializable]
    [SettingsSerializeAs(SettingsSerializeAs.String)]
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
        }
        public static MainViewModel Create(string json)
        {
            MainViewModel vm = null;
            try
            {
                vm = JsonConvert.DeserializeObject<MainViewModel>(json);
                if (vm != null)
                {
                    if (vm.ClipTypes == null || vm.ClipTypes.Count != 3)
                    {
                        vm.ClipTypes = new ObservableCollection<ClipCollectionCBItem>()
                        {
                            new ClipCollectionCBItem() { Text = "Performer", Type = eSongClipTypes.SongClips, IsChecked = true},
                            new ClipCollectionCBItem() { Text = "Dance", Type = eSongClipTypes.Dance},
                            new ClipCollectionCBItem() { Text = "Event", Type = eSongClipTypes.Event},
                        };
                    }

                    if (vm.SongYoutubeVideos == null || vm.SongYoutubeVideos.Count != 2)
                    {
                        vm.SongYoutubeVideos = new ObservableCollection<ClipCollectionCBItem>()
                        {
                            new ClipCollectionCBItem() { Text = "Clip" , Type = eSongClipTypes.YouTubeClip, IsChecked = true},
                            new ClipCollectionCBItem() { Text = "Dance", Type = eSongClipTypes.YouTubeDance },
                        };
                    }
                }

            }
            catch { }
            finally
            {
                if (vm == null)
                {
                    vm = new MainViewModel();
                }
            }
            return vm;
        }

        public void SaveTo(string filepath)
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filepath, json);
        }

        public string ClientName { get; set; }
        public string WatchDir { get; set; } = @"C:\AMPS\vmmplay";
        private bool m_RTL = false;

        public bool RTL
        {
            get { return m_RTL; }
            set
            {
                m_RTL = value;
                OnPropertyChanged(nameof(RTL));
                OnPropertyChanged(nameof(Song));
            }
        }

        public int SongInfoRows
        {
            get
            {
                if (Song == null)
                    return 1;

                int c = 0;
                if (!string.IsNullOrWhiteSpace(Song.Creator)) c++;
                if (!string.IsNullOrWhiteSpace(Song.Composer)) c++;
                if (!string.IsNullOrWhiteSpace(Song.Performer)) c++;
                if (!string.IsNullOrWhiteSpace(Song.Writer)) c++;

                return (c + 1) / 2;
            }
        }

        public string LogoImage { get; set; }
        public string VideoPath1 { get; set; }
        public string VideoPath2 { get; set; }
        public string VideoPathSinger { get; set; }
        public string VideoPathEvent { get; set; }
        public string VideoPathDance { get; set; }
        public string VideoPathDefault { get; set; }
        public string FlayerDir { get; set; }

        public string PicPathPerformer { get; set; }
        public string PicPathCreator { get; set; }
        public string PicPathComposer { get; set; }
        public string PicPathWriter { get; set; }
        public string PicPathDefault { get; set; }

        private ObservableCollection<ClipCollectionCBItem> m_ClipTypes;

        public ObservableCollection<ClipCollectionCBItem> ClipTypes
        {
            get
            {
                return m_ClipTypes;
            }
            set
            {
                m_ClipTypes = value;
                OnPropertyChanged(nameof(ClipTypes));
                if (m_ClipTypes != null)
                {
                    m_ClipTypes.CollectionChanged += (s, e) => items_CollectionChanged(s, e, (s1, e1) => OnPropertyChanged(nameof(ClipTypes)));
                    items_CollectionChanged(null,
                                            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, m_ClipTypes),
                                            (s1, e1) => OnPropertyChanged(nameof(ClipTypes)));
                }
            }
        }

        static void items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e, PropertyChangedEventHandler itemPropChanged)
        {
            if (e.OldItems != null)
            {
                foreach (INotifyPropertyChanged item in e.OldItems)
                    item.PropertyChanged -= itemPropChanged;
            }
            if (e.NewItems != null)
            {
                foreach (INotifyPropertyChanged item in e.NewItems)
                    item.PropertyChanged += itemPropChanged;
            }
        }


        private ObservableCollection<ClipCollectionCBItem> m_SongYoutubeVideos;

        public ObservableCollection<ClipCollectionCBItem> SongYoutubeVideos
        {
            get
            {
                return m_SongYoutubeVideos;
            }
            set
            {
                m_SongYoutubeVideos = value;
                OnPropertyChanged(nameof(SongYoutubeVideos));
                if (m_SongYoutubeVideos != null)
                {
                    m_SongYoutubeVideos.CollectionChanged += (s, e) => items_CollectionChanged(s, e, (s1, e1) => OnPropertyChanged(nameof(SongYoutubeVideos)));
                    items_CollectionChanged(null,
                                            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, m_SongYoutubeVideos),
                                            (s1, e1) => OnPropertyChanged(nameof(SongYoutubeVideos)));
                }
            }
        }

        private ClockInfo m_Clock = new ClockInfo();
        public ClockInfo Clock
        {
            get { return m_Clock; }
            set { m_Clock = value; OnPropertyChanged(nameof(Clock)); }
        }


        public class ClockInfo: ViewModelBase
        {
            private bool m_IsShow;
            public bool IsShow
            {
                get { return m_IsShow; }
                set { m_IsShow = value; OnPropertyChanged(nameof(IsShow)); }
            }

            private double m_X;
            public double X
            {
                get { return m_X; }
                set { m_X = value; OnPropertyChanged(nameof(X)); }
            }

            private double m_Y;
            public double Y
            {
                get { return m_Y; }
                set { m_Y = value; OnPropertyChanged(nameof(Y)); }
            }

            private double m_Width;
            public double Width
            {
                get { return m_Width; }
                set { m_Width = value; OnPropertyChanged(nameof(Width)); }
            }

            private double m_Height;
            public double Height
            {
                get { return m_Height; }
                set { m_Height = value; OnPropertyChanged(nameof(Height)); }
            }
        }


        private BannerVM m_Banner;

        public BannerVM Banner
        {
            get { return m_Banner ?? (m_Banner = new BannerVM()); }
            set { m_Banner = value; OnPropertyChanged(nameof(Banner)); }
        }

        private ColoredTextVm m_Lyrics;

        public ColoredTextVm Lyrics
        {
            get { return m_Lyrics ?? (m_Lyrics = new ColoredTextVm()); }
            set { m_Lyrics = value; OnPropertyChanged(nameof(Lyrics)); }
        }

        private Song m_Song;
        [JsonIgnore]
        public Song Song
        {
            get { return m_Song; }
            set
            {
                m_Song = value;
                OnPropertyChanged(nameof(Song));
                OnPropertyChanged(nameof(SongInfoRows));
                OnPropertyChanged(nameof(TypeBar));
            }
        }


        private ePicMode m_SelectedPicMode = ePicMode.Pics;
        [JsonConverter(typeof(StringEnumConverter))]
        public ePicMode SelectedPicMode
        {
            get { return m_SelectedPicMode; }
            set { m_SelectedPicMode = value; OnPropertyChanged(nameof(SelectedPicMode)); }
        }

        private eVideoMode m_SelectedVideoMode;

        [JsonConverter(typeof(StringEnumConverter))]
        public eVideoMode SelectedVideoMode
        {
            get { return m_SelectedVideoMode; }
            set { m_SelectedVideoMode = value; OnPropertyChanged(nameof(SelectedVideoMode)); }
        }

        private bool m_Pic_ShowCreator;
        public bool Pic_ShowCreator
        {
            get { return m_Pic_ShowCreator; }
            set { m_Pic_ShowCreator = value; OnPropertyChanged(nameof(Pic_ShowCreator)); }
        }

        private bool m_Pic_ShowPerformer;
        public bool Pic_ShowPerformer
        {
            get { return m_Pic_ShowPerformer; }
            set { m_Pic_ShowPerformer = value; OnPropertyChanged(nameof(Pic_ShowPerformer)); }
        }

        private bool m_Pic_ShowComposer;
        public bool Pic_ShowComposer
        {
            get { return m_Pic_ShowComposer; }
            set { m_Pic_ShowComposer = value; OnPropertyChanged(nameof(Pic_ShowComposer)); }
        }

        private bool m_Pic_ShowWriter;
        public bool Pic_ShowWriter
        {
            get { return m_Pic_ShowWriter; }
            set { m_Pic_ShowWriter = value; OnPropertyChanged(nameof(Pic_ShowWriter)); }
        }

        private bool m_Pic_ShowDefault = true;
        public bool Pic_ShowDefault
        {
            get { return m_Pic_ShowDefault; }
            set { m_Pic_ShowDefault = value; OnPropertyChanged(nameof(Pic_ShowDefault)); }
        }

        private bool m_ShowSongInfo = true;
        public bool ShowSongInfo
        {
            get { return m_ShowSongInfo; }
            set { m_ShowSongInfo = value; OnPropertyChanged(nameof(ShowSongInfo)); }
        }

        private bool m_ShowSongInfoPic = true;
        public bool ShowSongInfoPic
        {
            get { return m_ShowSongInfoPic; }
            set { m_ShowSongInfoPic = value; OnPropertyChanged(nameof(ShowSongInfoPic)); }
        }

        private bool m_ShowType = true;
        public bool ShowType
        {
            get { return m_ShowType; }
            set { m_ShowType = value; OnPropertyChanged(nameof(ShowType)); }
        }

        private bool m_ShowLeftPic = true;
        public bool ShowLeftPic
        {
            get { return m_ShowLeftPic; }
            set { m_ShowLeftPic = value; OnPropertyChanged(nameof(ShowLeftPic)); }
        }

        private ColoredTextVm m_SongInfo01;
        public ColoredTextVm SongInfo01
        {
            get { return m_SongInfo01 ?? (m_SongInfo01 = new ColoredTextVm()); }
            set { m_SongInfo01 = value; OnPropertyChanged(nameof(SongInfo01)); }
        }

        public class TypeFormat
        {
            public string Type { get; set; }
            public ColoredTextVm Format { get; set; } = new ColoredTextVm();
        }

        public TypeFormat DefaultTypeFormat { get; set; } = new TypeFormat() { Type = "Default" };

        private TypeFormat[] __TypesFormat;
        [JsonProperty("TypesFormats")]
        private TypeFormat[] _TypesFormats
        {
            get
            {
                return __TypesFormat ?? (__TypesFormat = new TypeFormat[] {
                    new TypeFormat() { Type = "מעגלים" },
                    new TypeFormat() { Type = "זוגות" },
                    new TypeFormat() { Type = "שורות" }
                });
            }
            set { __TypesFormat = value; OnPropertyChanged(nameof(TypesFormats)); OnPropertyChanged(nameof(TypeBar)); }
        }

        private TypeFormat[] m_TypesFormat;
        [JsonIgnore]
        public TypeFormat[] TypesFormats
        {
            get
            {
                if (m_TypesFormat != null)
                    return m_TypesFormat;

                m_TypesFormat = new TypeFormat[_TypesFormats.Length + 1];
                m_TypesFormat[0] = DefaultTypeFormat;
                _TypesFormats.CopyTo(m_TypesFormat, 1);

                return m_TypesFormat;
            }
        }

        private TypeFormat m_SelectedTypeFormat;
        [JsonIgnore]
        public TypeFormat SelectedTypeFormat
        {
            get { return m_SelectedTypeFormat ?? (m_SelectedTypeFormat = TypesFormats.FirstOrDefault()); }
            set { m_SelectedTypeFormat = value; OnPropertyChanged(nameof(SelectedTypeFormat)); }
        }

        [JsonIgnore]
        public ColoredTextVm TypeBar => (TypesFormats.FirstOrDefault(f => f.Type == Song?.Type) ?? DefaultTypeFormat).Format;

        public void RefreshTypeBar() => OnPropertyChanged(nameof(TypeBar));

        private ColoredTextVm m_SongInfo02;
        public ColoredTextVm SongInfo02
        {
            get { return m_SongInfo02 ?? (m_SongInfo02 = new ColoredTextVm()); }
            set { m_SongInfo02 = value; OnPropertyChanged(nameof(SongInfo02)); }
        }

        private double m_TopPanelHeight;
        public double TopPanelHeight
        {
            get { return m_TopPanelHeight; }
            set { m_TopPanelHeight = value; OnPropertyChanged(nameof(TopPanelHeight)); }
        }

        #region DefaultLayout

        private bool m_DefaultLayout_SongInfoShowOnTop;

        public bool DefaultLayout_SongInfoShowOnTop
        {
            get { return m_DefaultLayout_SongInfoShowOnTop; }
            set { m_DefaultLayout_SongInfoShowOnTop = value; OnPropertyChanged(nameof(DefaultLayout_SongInfoShowOnTop)); }
        }


        private double m_DefaultLayout_LeftWidth;
        public double DefaultLayout_LeftWidth
        {
            get { return m_DefaultLayout_LeftWidth; }
            set { m_DefaultLayout_LeftWidth = value; OnPropertyChanged(nameof(DefaultLayout_LeftWidth)); }
        }

        private double m_DefaultLayout_SongInfoHeight;
        public double DefaultLayout_SongInfoHeight
        {
            get { return m_DefaultLayout_SongInfoHeight; }
            set { m_DefaultLayout_SongInfoHeight = value; OnPropertyChanged(nameof(DefaultLayout_SongInfoHeight)); }
        }

        private double m_DefaultLayout_SongInfoTypeBarHeight;
        public double DefaultLayout_SongInfoTypeBarHeight
        {
            get { return m_DefaultLayout_SongInfoTypeBarHeight; }
            set { m_DefaultLayout_SongInfoTypeBarHeight = value; OnPropertyChanged(nameof(DefaultLayout_SongInfoTypeBarHeight)); }
        }

        private VideoListItem m_DefaultLayout_VideoItem;
        [JsonIgnore]
        public VideoListItem DefaultLayout_VideoItem
        {
            get { return m_DefaultLayout_VideoItem ?? (m_DefaultLayout_VideoItem = new VideoListItem()); }
            set { m_DefaultLayout_VideoItem = value; OnPropertyChanged(nameof(DefaultLayout_VideoItem)); }
        }
      
        #endregion

        private object m_LastLayoutData;
        private eLayoutModes m_SelectedLayout = eLayoutModes.Default;
        [JsonConverter(typeof(StringEnumConverter))]
        public eLayoutModes SelectedLayout
        {
            get { return m_SelectedLayout; }
            set
            {
                if (value != m_SelectedLayout)
                {
                    m_SelectedLayout = value;
                    if (value == eLayoutModes.Default)
                    {
                        DefaultLayout_VideoItem = (VideoListItem)m_LastLayoutData;
                        m_LastLayoutData = PresentationVM.PresentationItems;
                        PresentationVM.PresentationItems = null;
                    }
                    else if (value == eLayoutModes.Presentation)
                    {
                        if (m_LastLayoutData is ObservableCollection<PresentationItem>)
                        {
                            PresentationVM.PresentationItems = m_LastLayoutData as ObservableCollection<PresentationItem>;
                        }
                        m_LastLayoutData = DefaultLayout_VideoItem;
                        DefaultLayout_VideoItem = null;
                    }
                    OnPropertyChanged(nameof(SelectedLayout));
                }

            }
        }

        private string m_PicStretch;

        public string PicStretch
        {
            get { return m_PicStretch; }
            set { m_PicStretch = value; OnPropertyChanged(nameof(PicStretch)); }
        }    

        private TextVM m_LeftPicTitle;

        public TextVM LeftPicTitle
        {
            get { return m_LeftPicTitle ?? (m_LeftPicTitle = new TextVM()); }
            set { m_LeftPicTitle = value; OnPropertyChanged(nameof(LeftPicTitle)); }
        }

        private string m_LeftPicSource;
        [JsonIgnore]
        public string LeftPicSource
        {
            get { return m_LeftPicSource; }
            set { m_LeftPicSource = value; OnPropertyChanged(nameof(LeftPicSource)); }
        }

        private string m_ArtistPicSource;
        [JsonIgnore]
        public string ArtistPicSource
        {
            get { return m_ArtistPicSource; }
            set { m_ArtistPicSource = value; OnPropertyChanged(nameof(ArtistPicSource)); }
        }

        private ClientInfo m_CurrentClient;

        [JsonIgnore]
        public ClientInfo CurrentClient
        {
            get { return m_CurrentClient; }
            set { m_CurrentClient = value; OnPropertyChanged(nameof(CurrentClient)); }
        }

        private List<ClientInfo> m_ActiveClients;
        [JsonIgnore]
        public List<ClientInfo> ActiveClients
        {
            get { return m_ActiveClients; }
            set { m_ActiveClients = value; OnPropertyChanged(nameof(ActiveClients)); }
        }

        private PresentationModeViewModel m_PresentationVM;
        public PresentationModeViewModel PresentationVM
        {
            get { return m_PresentationVM ?? (m_PresentationVM = new PresentationModeViewModel()); }
            set { m_PresentationVM = value; OnPropertyChanged(nameof(PresentationVM)); }
        }

        public int LeftPicDelay { get; set; }
        public int DbUpdateDelay { get; set; } = 5000; // 5 sec default


        private DateTime m_CurrDate;
        [JsonIgnore]
        public DateTime CurrDate
        {
            get { return m_CurrDate; }
            set { m_CurrDate = value; OnPropertyChanged(nameof(CurrDate)); }
        }

        [JsonIgnore]
        public IEnumerable<Tuple<string, Func<string>>> PicSources;
        [JsonIgnore]
        public IEnumerable<string> Flyerfiles;


    }

    public class PresentationModeViewModel : ViewModelBase
    {
        private ObservableCollection<PresentationItem> m_PresentationItems;
        private ObservableCollection<PresentationItem> m_PresentationItems_Curr;
        [JsonIgnore]
        public ObservableCollection<PresentationItem> PresentationItems
        {
            get { return m_PresentationItems_Curr; }
            set
            {
                m_PresentationItems_Curr = value;
                if (value != null)
                {
                    m_PresentationItems = value;
                }
                OnPropertyChanged(nameof(PresentationItems));
            }
        }


        [JsonProperty("PresentationItems")]
        private ObservableCollection<PresentationItem> _PresentationItems
        {
            get { return m_PresentationItems; }
            set
            {
                m_PresentationItems_Curr = m_PresentationItems = value ?? new ObservableCollection<PresentationItem>();
                OnPropertyChanged(nameof(PresentationItems));
            }
        }

        private List<PresentationItem> GetDefaultPresentationItems()
        {
            return new List<PresentationItem>()
            {
                new PictureItem() { Path = @"C:\Users\iasis\Pictures\321807-flowers.jpg" },
                new VideoItem() { VideoSource = new VideoSource(@"\\192.168.1.120\c\AMPS\vmm\VideoClip\זמרים\A-WA\A-WA.mpg") },
                new VideoItem() { VideoSource = new VideoSource(@"\\192.168.1.120\c\AMPS\vmm\VideoClip\זמרים\אביהו מדינה\אביהו מדינה - שיר השיכור.mpg") },
                new YoutubeVideoItem(@"https://www.youtube.com/watch?v=I0BOnxAHztk"),
            };
        }
    }

    [JsonConverter(typeof(PresentationItemConverter))]
    public abstract class PresentationItem : ViewModelBase
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public abstract ePresentationKinds Kind { get; }


        public virtual double X { get; set; }
        public virtual double Y { get; set; }
        public virtual double Width { get; set; }
        public virtual double Height { get; set; }
        public virtual double Angle { get; set; }
        public virtual string Text { get; set; }
        public virtual string Path { get; set; }

        // factory method
        public static PresentationItem Create(ePresentationKinds kind, string path)
        {
            switch (kind)
            {
                case ePresentationKinds.AmpsLive:
                    return new AmpsPresentationItem();
                case ePresentationKinds.AmpsLiveWithSongInfo:
                    return new AmpsPresentationItem() { ShowSongInfo = true };
                case ePresentationKinds.Picture:
                    return new PictureItem() { Path = path };
                case ePresentationKinds.PictureList:
                    return new PictureListItem() { Path = path };
                case ePresentationKinds.Video:
                    return new VideoItem() { VideoSource = new VideoSource(path) };
                case ePresentationKinds.VideoList:
                    return new VideoListItem(path);
                case ePresentationKinds.YoutubeVideo:
                    return new YoutubeVideoItem() { VideoSource = new YoutubeVideoSource(path)};
                default:
                    throw new NotImplementedException();
            }
        }

    
    }
    public class PictureListItem : PictureItem
    {
        public override ePresentationKinds Kind => ePresentationKinds.PictureList;

    }

    public class PictureItem : PresentationItem
    {
        public override ePresentationKinds Kind => ePresentationKinds.Picture;

       
        private string m_Stretch;

        public string Stretch
        {
            get { return m_Stretch; }
            set { m_Stretch = value; OnPropertyChanged(nameof(Stretch)); }
        }
    }

    public class VideoItem : PresentationItem
    {
        public override ePresentationKinds Kind => ePresentationKinds.Video;

        private VideoSource m_VideoSource;

        public VideoSource VideoSource
        {
            get { return m_VideoSource; }
            set { m_VideoSource = value; OnPropertyChanged(nameof(VideoSource)); }
        }

    }

    public class AmpsPresentationItem : VideoListItem
    {
        public override ePresentationKinds Kind => ePresentationKinds.AmpsLive;

        public bool ShowSongInfo { get; set; }
    }


    public class VideoListItem : PresentationItem
    {
        public override ePresentationKinds Kind => ePresentationKinds.VideoList;

        public VideoListItem(string path)
        {
            Path = path;
        }

        public VideoListItem()
        {
        }

        public override string Path
        {
            get
            {
                return base.Path;
            }

            set
            {
                base.Path = value;
                VideoSources = ReadVideos(value);
            }
        }

        private List<VideoSource> m_VideoSources;

        [JsonIgnore]
        public List<VideoSource> VideoSources
        {
            get { return m_VideoSources; }
            set { m_VideoSources = value; OnPropertyChanged(nameof(VideoSources)); }
        }
        private static List<VideoSource> ReadVideos(string path)
        {
            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                return files.Select(filePath => new VideoSource(filePath)).ToList();
            }

            return null;
        }
    }

    public class YoutubeVideoSource : VideoSource
    {
        [JsonConstructor]
        public YoutubeVideoSource([JsonProperty("Path")] string videoPath, [JsonProperty("Time")] long? time = default(long?)) 
            : base(videoPath, time)
        {
            m_VideoId = Parse(videoPath);
        }

        private static string Parse(string videoPath)
        {
            if (videoPath == null)
                return null;

            const int idLen = 11;
            var prefixes = new[] { "v=", "youtu.be/" };
            string id = videoPath;
            int i;
            foreach (var prefix in prefixes)
            {
                if ((i = videoPath.IndexOf(prefix)) > 0)
                {
                    id = videoPath.Substring(i + prefix.Length, idLen);
                    break;
                }
            }

            return id;
        }

        private string m_VideoId;

        [JsonIgnore]
        public string VideoId
        {
            get { return m_VideoId; }
            set { m_VideoId = value; OnPropertyChanged(nameof(VideoId)); }
        }

    }

    public class YoutubeVideoItem : PresentationItem
    {
        public override ePresentationKinds Kind => ePresentationKinds.YoutubeVideo;

        private YoutubeVideoSource m_VideoSource;

        public YoutubeVideoItem()
        {
        }

        public YoutubeVideoItem(string source)
        {
            m_VideoSource = new YoutubeVideoSource(source);
        }

        public YoutubeVideoSource VideoSource
        {
            get { return m_VideoSource; }
            set { m_VideoSource = value; OnPropertyChanged(nameof(VideoSource)); }
        }
    }

    public enum ePresentationKinds
    {
        AmpsLive,
        AmpsLiveWithSongInfo,
        Picture,
        PictureList,
        Video,
        VideoList,
        YoutubeVideo,
    }

    public enum eSongClipTypes
    {
        SongClips,
        Event,
        Dance,
        YouTubeDance,
        YouTubeClip,
        Default
    }

    public class ClipCollectionCBItem : CheckBoxItem
    {
        public eSongClipTypes Type { get; set; }
    }

    public class CheckBoxItem : ViewModelBase
    {
        private bool m_IsChecked;

        public bool IsChecked
        {
            get { return m_IsChecked; }
            set { m_IsChecked = value; OnPropertyChanged(nameof(IsChecked)); }
        }

        private string m_Text;

        public string Text
        {
            get { return m_Text; }
            set { m_Text = value; OnPropertyChanged(nameof(Text)); }
        }

    }

}

namespace BitnuaVideoPlayer.ViewModels
{
    public class BannerVM : TextVM
    {
        private int m_Speed = 1;

        public int Speed
        {
            get { return m_Speed; }
            set { m_Speed = value; OnPropertyChanged(nameof(Speed)); }
        }

        private FlowDirection m_Direction = FlowDirection.LeftToRight;

        public FlowDirection Direction
        {
            get { return m_Direction; }
            set { m_Direction = value; OnPropertyChanged(nameof(Direction)); }
        }

        private bool m_IsVisible;

        public bool IsVisible
        {
            get { return m_IsVisible; }
            set { m_IsVisible = value; OnPropertyChanged(nameof(IsVisible)); }
        }

        private bool m_ShowOnTop;

        public bool ShowOnTop
        {
            get { return m_ShowOnTop; }
            set { m_ShowOnTop = value; OnPropertyChanged(nameof(ShowOnTop)); }
        }

        private double m_Height = 60d;

        public double Height
        {
            get { return m_Height; }
            set { m_Height = value; OnPropertyChanged(nameof(Height)); }
        }


        private string m_PicsPath;

        public string PicsPath
        {
            get { return m_PicsPath; }
            set { m_PicsPath = value; OnPropertyChanged(nameof(PicsPath)); }
        }

        private ObservableCollection<PictureItem> m_Pics;
        [JsonIgnore]
        public ObservableCollection<PictureItem> Pics
        {
            get { return m_Pics; }
            set { m_Pics = value; OnPropertyChanged(nameof(Pics)); }
        }


    }

    public class ColoredTextVm : ViewModelBase
    {
        private Color m_BackColor;

        [TypeConverter(typeof(Color))]
        public Color BackColor
        {
            get { return m_BackColor; }
            set { m_BackColor = value; OnPropertyChanged(nameof(BackColor)); }
        }

        private Color m_ForeColor = Color.Black;

        [TypeConverter(typeof(Color))]
        public Color ForeColor
        {
            get { return m_ForeColor; }
            set { m_ForeColor = value; OnPropertyChanged(nameof(ForeColor)); }
        }

        private Font m_Font = new Font("Times New Roman",
                                            15f,
                                            System.Drawing.FontStyle.Regular,
                                            GraphicsUnit.Pixel
                                        );

        [TypeConverter(typeof(Font))]
        public Font Font
        {
            get { return m_Font; }
            set
            {
                m_Font = value;
                OnPropertyChanged(nameof(Font));
                OnPropertyChanged(nameof(FontFamily));
                OnPropertyChanged(nameof(FontSize));
                OnPropertyChanged(nameof(IsBold));
            }
        }

        [JsonIgnore]
        public double FontSize => Font.Size;


        [JsonIgnore]
        public bool IsBold => Font.Bold;

        [JsonIgnore]
        public System.Windows.Media.FontFamily FontFamily => new System.Windows.Media.FontFamily(Font.Name);
    }

    public class TextVM : ColoredTextVm
    {
        private string m_Text = "Text Here ...";

        public virtual string Text
        {
            get { return m_Text; }
            set { m_Text = value; OnPropertyChanged(nameof(Text)); }
        }
    }

    public class Song
    {
        public string Title { get; set; }

        public string Heb_Performer { get; set; }
        public string Heb_Creator { get; set; }
        public string Heb_Composer { get; set; }
        public string Heb_Writer { get; set; }

        public string Eng_Performer { get; set; }
        public string Eng_Creator { get; set; }
        public string Eng_Composer { get; set; }
        public string Eng_Writer { get; set; }

        public string Performer => App.Instance.VM.RTL ? Heb_Performer : (!string.IsNullOrEmpty(Eng_Performer) ? Eng_Performer : Heb_Performer);
        public string Creator => App.Instance.VM.RTL ? Heb_Creator : (!string.IsNullOrEmpty(Eng_Creator) ? Eng_Creator : Heb_Creator);
        public string Composer => App.Instance.VM.RTL ? Heb_Composer : (!string.IsNullOrEmpty(Eng_Composer) ? Eng_Composer : Heb_Composer);
        public string Writer => App.Instance.VM.RTL ? Heb_Writer : (!string.IsNullOrEmpty(Eng_Writer) ? Eng_Writer : Heb_Writer);

        public int? Year { get; set; }
        public string HebTitle { get; set; }
        public string Lyrics { get; set; }
        public string YouTubeSong { get; set; }
        public string YouTubeDance { get; set; }
        public string Type { get; set; }
    }

    public enum ePicMode
    {
        Lyrics,
        Pics,
        Flyer
    }

    public enum eVideoMode
    {
        Clip,
        VideoDir1,
        VideoDir2,
        Youtube
    }

    public enum eLayoutModes
    {
        Default,
        Presentation,
        //TwoVideos,
        //ThreePics
    }

    public class ClientInfo
    {
        [BsonId]
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

        public string Name { get; set; }

        public DateTime CreationDate { get; set; } = DateTime.UtcNow;

        public override string ToString() => string.IsNullOrEmpty(Name) ? Id.ToString() : Name;

        public override bool Equals(object obj)
        {
            return obj is ClientInfo && ((ClientInfo)obj).Id.Equals(Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    public class PlayEntry
    {
        [BsonId]
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
        public ClientInfo Client { get; set; }
        public Song Song { get; set; }
        public DateTime Time { get; set; } = DateTime.UtcNow;


        public override bool Equals(object obj)
        {
            return obj is PlayEntry && ((PlayEntry)obj).Id.Equals(Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}