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

namespace BitnuaVideoPlayer
{
    [Serializable]
    [SettingsSerializeAs(SettingsSerializeAs.String)]
    public class MainViewModel : ViewModelBase
    {
        private MainViewModel()
        {
        }

        public static MainViewModel Create(string json)
        {
            MainViewModel vm = null;
            try
            {
                var conf = File.ReadAllText(json);
                vm = JsonConvert.DeserializeObject<MainViewModel>(conf);
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

        private List<ClipCollectionCBItem> m_ClipsNums;
        public List<ClipCollectionCBItem> ClipsNums
        {
            get
            {
                return m_ClipsNums;
            }
            set { m_ClipsNums = value; OnPropertyChanged(() => ClipsNums); }
        }

        private BannerVM m_Banner;

        public BannerVM Banner
        {
            get
            {
                return m_Banner;
            }
            set { m_Banner = value; OnPropertyChanged(() => Banner); }
        }

        private LyricsVM m_Lyrics = new LyricsVM();

        public LyricsVM Lyrics
        {
            get { return m_Lyrics; }
            set { m_Lyrics = value; OnPropertyChanged(() => Lyrics); }
        }

        private Song m_Song;
        [JsonIgnore]
        public Song Song
        {
            get { return m_Song; }
            set { m_Song = value; OnPropertyChanged(() => Song); }
        }

        private double m_SongTitleFontSize;

        public double SongTitleFontSize
        {
            get { return m_SongTitleFontSize; }
            set { m_SongTitleFontSize = value; OnPropertyChanged(() => SongTitleFontSize); }
        }

        private double m_Song2ndFontSize;

        public double Song2ndFontSize
        {
            get { return m_Song2ndFontSize; }
            set { m_Song2ndFontSize = value; OnPropertyChanged(() => Song2ndFontSize); }
        }


        private ePicMode m_SelectedPicMode;
        [JsonConverter(typeof(StringEnumConverter))]
        public ePicMode SelectedPicMode
        {
            get { return m_SelectedPicMode; }
            set { m_SelectedPicMode = value; OnPropertyChanged(() => SelectedPicMode); }
        }

        private eVideoMode m_SelectedVideoMode;

        [JsonConverter(typeof(StringEnumConverter))]
        public eVideoMode SelectedVideoMode
        {
            get { return m_SelectedVideoMode; }
            set { m_SelectedVideoMode = value; OnPropertyChanged(() => SelectedVideoMode); }
        }

        private bool m_Pic_ShowCreator;
        public bool Pic_ShowCreator
        {
            get { return m_Pic_ShowCreator; }
            set { m_Pic_ShowCreator = value; OnPropertyChanged(() => Pic_ShowCreator); }
        }

        private bool m_Pic_ShowPerformer;

        public bool Pic_ShowPerformer
        {
            get { return m_Pic_ShowPerformer; }
            set { m_Pic_ShowPerformer = value; OnPropertyChanged(() => Pic_ShowPerformer); }
        }

        private bool m_Pic_ShowComposer;

        public bool Pic_ShowComposer
        {
            get { return m_Pic_ShowComposer; }
            set { m_Pic_ShowComposer = value; OnPropertyChanged(() => Pic_ShowComposer); }
        }

        private bool m_Pic_ShowWriter;

        public bool Pic_ShowWriter
        {
            get { return m_Pic_ShowWriter; }
            set { m_Pic_ShowWriter = value; OnPropertyChanged(() => Pic_ShowWriter); }
        }

        private double m_TopPanelHeight;

        public double TopPanelHeight
        {
            get { return m_TopPanelHeight; }
            set { m_TopPanelHeight = value; OnPropertyChanged(() => TopPanelHeight); }
        }

        #region DefaultLayout

        private double m_DefaultLayout_VideoCtrlWidth;

        public double DefaultLayout_VideoCtrlWidth
        {
            get { return m_DefaultLayout_VideoCtrlWidth; }
            set { m_DefaultLayout_VideoCtrlWidth = value; OnPropertyChanged(() => DefaultLayout_VideoCtrlWidth); }
        }

        private double m_DefaultLayout_SongInfoHeight;

        public double DefaultLayout_SongInfoHeight
        {
            get { return m_DefaultLayout_SongInfoHeight; }
            set { m_DefaultLayout_SongInfoHeight = value; OnPropertyChanged(() => DefaultLayout_SongInfoHeight); }
        }

        private VideoItem m_DefaultLayout_VideoItem;
        [JsonIgnore]
        public VideoItem DefaultLayout_VideoItem
        {
            get { return m_DefaultLayout_VideoItem ?? (m_DefaultLayout_VideoItem = new VideoItem()); }
            set { m_DefaultLayout_VideoItem = value; OnPropertyChanged(() => DefaultLayout_VideoItem); }
        }

      
        #endregion

        private double m_ThreePicsLayout_LeftWidth;

        public double ThreePicsLayout_LeftWidth
        {
            get { return m_ThreePicsLayout_LeftWidth; }
            set { m_ThreePicsLayout_LeftWidth = value; OnPropertyChanged(() => ThreePicsLayout_LeftWidth); }
        }

        private double m_ThreePicsLayout_MidWidth;

        public double ThreePicsLayout_MidWidth
        {
            get { return m_ThreePicsLayout_MidWidth; }
            set { m_ThreePicsLayout_MidWidth = value; OnPropertyChanged(() => ThreePicsLayout_MidWidth); }
        }

        private double m_TwoVidoesLayout_LeftWidth;

        public double TwoVidoesLayout_LeftWidth
        {
            get { return m_TwoVidoesLayout_LeftWidth; }
            set { m_TwoVidoesLayout_LeftWidth = value; OnPropertyChanged(() => TwoVidoesLayout_LeftWidth); }
        }


        private object m_LastLayoutData;
        private eLayoutModes m_SelectedLayout;
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
                        if (m_LastLayoutData is VideoItem)
                        {
                            DefaultLayout_VideoItem = m_LastLayoutData as VideoItem;
                        }
                        m_LastLayoutData = PresentationVM.PresentationItems;
                        PresentationVM.PresentationItems = null;
                    }
                    else if (value == eLayoutModes.Presentation)
                    {
                        if (m_LastLayoutData is List<PresentationItem>)
                        {
                            PresentationVM.PresentationItems = m_LastLayoutData as List<PresentationItem>;
                        }
                        m_LastLayoutData = DefaultLayout_VideoItem;
                        DefaultLayout_VideoItem = null;
                    }
                    OnPropertyChanged(() => SelectedLayout);
                }

            }
        }

        private string m_PicStretch;

        public string PicStretch
        {
            get { return m_PicStretch; }
            set { m_PicStretch = value; OnPropertyChanged(() => PicStretch); }
        }


        private TextVM m_LeftPicTitle;

        [JsonIgnore]
        public TextVM LeftPicTitle
        {
            get { return m_LeftPicTitle; }
            set { m_LeftPicTitle = value; OnPropertyChanged(() => LeftPicTitle); }
        }

        private string m_LeftPicSource;
        [JsonIgnore]
        public string LeftPicSource
        {
            get { return m_LeftPicSource; }
            set { m_LeftPicSource = value; OnPropertyChanged(() => LeftPicSource); }
        }

        private string m_ArtistPicSource;
        [JsonIgnore]
        public string ArtistPicSource
        {
            get { return m_ArtistPicSource; }
            set { m_ArtistPicSource = value; OnPropertyChanged(() => ArtistPicSource); }
        }

        private string m_ThreePicsLayout_LeftPicSource;

        [JsonIgnore]
        public string ThreePicsLayout_LeftPicSource
        {
            get { return m_ThreePicsLayout_LeftPicSource; }
            set { m_ThreePicsLayout_LeftPicSource = value; OnPropertyChanged(() => ThreePicsLayout_LeftPicSource); }
        }

        private string m_ThreePicsLayout_MidPicSource;

        [JsonIgnore]
        public string ThreePicsLayout_MidPicSource
        {
            get { return m_ThreePicsLayout_MidPicSource; }
            set { m_ThreePicsLayout_MidPicSource = value; OnPropertyChanged(() => ThreePicsLayout_MidPicSource); }
        }

        private string m_ThreePicsLayout_RightPicSource;

        [JsonIgnore]
        public string ThreePicsLayout_RightPicSource
        {
            get { return m_ThreePicsLayout_RightPicSource; }
            set { m_ThreePicsLayout_RightPicSource = value; OnPropertyChanged(() => ThreePicsLayout_RightPicSource); }
        }

        private ClientInfo m_CurrentClient;

        [JsonIgnore]
        public ClientInfo CurrentClient
        {
            get { return m_CurrentClient; }
            set { m_CurrentClient = value; OnPropertyChanged(() => CurrentClient); }
        }

        private List<ClientInfo> m_ActiveClients;
        [JsonIgnore]
        public List<ClientInfo> ActiveClients
        {
            get { return m_ActiveClients; }
            set { m_ActiveClients = value; OnPropertyChanged(() => ActiveClients); }
        }

        private PresentationModeViewModel m_PresentationVM;
        public PresentationModeViewModel PresentationVM
        {
            get { return m_PresentationVM ?? (m_PresentationVM = new PresentationModeViewModel()); }
            set { m_PresentationVM = value; OnPropertyChanged(() => PresentationVM); }
        }



        public int LeftPicDelay { get; set; }
        public int DbUpdateDelay { get; set; } = 5000; // 5 sec default
    }

    public class PresentationModeViewModel : ViewModelBase
    {
        private int m_Rows = 1;

        public int Rows
        {
            get { return m_Rows; }
            set { m_Rows = value; OnPropertyChanged(() => Rows); }
        }

        private int m_Cols = 1;

        public int Cols
        {
            get { return m_Cols; }
            set { m_Cols = value; OnPropertyChanged(() => Cols); }
        }

        private List<PresentationItem> m_PresentationItems;
        private List<PresentationItem> m_PresentationItems_Curr;
        [JsonIgnore]
        public List<PresentationItem> PresentationItems
        {
            get { return m_PresentationItems_Curr; }
            set
            {
                m_PresentationItems_Curr = value;
                if (value != null)
                {
                    m_PresentationItems = value;
                }
                OnPropertyChanged(() => PresentationItems);
            }
        }


        [JsonProperty("PresentationItems")]
        private List<PresentationItem> _PresentationItems
        {
            get { return m_PresentationItems; }
            set { m_PresentationItems_Curr = m_PresentationItems = value; OnPropertyChanged(() => PresentationItems); }
        }

        private List<PresentationItem> GetDefaultPresentationItems()
        {
            return new List<PresentationItem>()
            {
                new PictureItem() { Path = @"C:\Users\iasis\Pictures\321807-flowers.jpg" },
                new VideoItem() { VideoSource = new VideoSource(@"\\192.168.1.120\c\AMPS\vmm\VideoClip\זמרים\A-WA\A-WA.mpg") },
                new VideoItem() { VideoSource = new VideoSource(@"\\192.168.1.120\c\AMPS\vmm\VideoClip\זמרים\אביהו מדינה\אביהו מדינה - שיר השיכור.mpg") },
                new YoutubeVideoItem() { Path = @"https://www.youtube.com/watch?v=I0BOnxAHztk" },
            };
        }
    }

    [JsonConverter(typeof(PresentationItemConverter))]
    public abstract class PresentationItem : ViewModelBase
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public abstract ePresentationKinds Kind { get; }
    }

    public class PictureItem : PresentationItem
    {
        public override ePresentationKinds Kind => ePresentationKinds.Picture;

        private string m_Path;
        public string Path
        {
            get { return m_Path; }
            set { m_Path = value; OnPropertyChanged(() => Path); }
        }

        private string m_Stretch;

        public string Stretch
        {
            get { return m_Stretch; }
            set { m_Stretch = value; OnPropertyChanged(() => Stretch); }
        }
    }

    public class VideoItem : PresentationItem
    {
        public override ePresentationKinds Kind => ePresentationKinds.Video;

        private VideoSource m_VideoSource;

        public VideoSource VideoSource
        {
            get { return m_VideoSource; }
            set { m_VideoSource = value; OnPropertyChanged(() => VideoSource); }
        }

    }

    public class YoutubeVideoItem : PresentationItem
    {
        public override ePresentationKinds Kind => ePresentationKinds.YoutubeVideo;

        private string m_Path;
        public string Path
        {
            get { return m_Path; }
            set { m_Path = value; OnPropertyChanged(() => Path); }
        }

    }

    public enum ePresentationKinds
    {
        Picture,
        Video,
        YoutubeVideo,
    }

    public class ClipCollectionCBItem : CheckBoxItem
    {
        private string m_Path;

        public string Path
        {
            get { return m_Path; }
            set { m_Path = value; OnPropertyChanged(() => Path); }
        }

    }

    public class CheckBoxItem : ViewModelBase
    {
        private bool m_IsChecked;

        public bool IsChecked
        {
            get { return m_IsChecked; }
            set { m_IsChecked = value; OnPropertyChanged(() => IsChecked); }
        }

        private string m_Text;

        public string Text
        {
            get { return m_Text; }
            set { m_Text = value; OnPropertyChanged(() => Text); }
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
            set { m_Speed = value; OnPropertyChanged(() => Speed); }
        }

        private string m_Direction = "left";

        public string Direction
        {
            get { return m_Direction; }
            set { m_Direction = value; OnPropertyChanged(() => Direction); }
        }
    }

    public class LyricsVM : TextVM
    {
        [JsonIgnore]
        public override string Text
        {
            get
            {
                return base.Text;
            }

            set
            {
                base.Text = value;
            }
        }
    }
    public class TextVM : ViewModelBase
    {
        private string m_Text = "Text Here ...";

        public virtual string Text
        {
            get { return m_Text; }
            set { m_Text = value; OnPropertyChanged(() => Text); }
        }


        private Color m_BackColor;

        [TypeConverter(typeof(Color))]
        public Color BackColor
        {
            get { return m_BackColor; }
            set { m_BackColor = value; OnPropertyChanged(() => BackColor); }
        }

        private Color m_ForeColor = Color.Black;

        [TypeConverter(typeof(Color))]
        public Color ForeColor
        {
            get { return m_ForeColor; }
            set { m_ForeColor = value; OnPropertyChanged(() => ForeColor); }
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
                OnPropertyChanged(() => Font);
                OnPropertyChanged(() => FontFamily);
                OnPropertyChanged(() => FontSize);
            }
        }

        [JsonIgnore]
        public double FontSize => Font.Size;
        [JsonIgnore]
        public System.Windows.Media.FontFamily FontFamily => new System.Windows.Media.FontFamily(Font.Name);
    }

    public class Song
    {
        public string Title { get; set; }
        public string Performer { get; set; }
        public string Creator { get; set; }
        public string Composer { get; set; }
        public string Writer { get; set; }
        public int? Year { get; set; }
        public string HebTitle { get; set; }
        public string Lyrics { get; set; }
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
        VideoDir2
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