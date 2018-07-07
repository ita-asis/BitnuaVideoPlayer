using System.Drawing;
using System;
using System.Configuration;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;
using BitnuaVideoPlayer.ViewModels;
using System.Collections.Generic;

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

        private BannerVM m_Banner;

        public BannerVM Banner
        {
            get
            {
                return m_Banner;
            }
            set { m_Banner = value; OnPropertyChanged(() => Banner); }
        }

        private ePicMode m_SelectedPicMode;

        public ePicMode SelectedPicMode
        {
            get { return m_SelectedPicMode; }
            set { m_SelectedPicMode = value; OnPropertyChanged(() => SelectedPicMode); }
        }

        private eVideoMode m_SelectedVideoMode;

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

        private TextVM m_Lyrics = new TextVM();

        public TextVM Lyrics
        {
            get { return m_Lyrics; }
            set { m_Lyrics = value; OnPropertyChanged(() => Lyrics); }
        }

        private double m_VideoCtrlHeight;

        public double VideoCtrlHeight
        {
            get { return m_VideoCtrlHeight; }
            set { m_VideoCtrlHeight = value; OnPropertyChanged(() => VideoCtrlHeight); }
        }

        private double m_VideoCtrlWidth;

        public double VideoCtrlWidth
        {
            get { return m_VideoCtrlWidth; }
            set { m_VideoCtrlWidth = value; OnPropertyChanged(() => VideoCtrlWidth); }
        }

        private List<CheckBoxItem> m_ClipsNums;

        private static List<CheckBoxItem> GetDefaultClipTypes()
        {
            const int count = 3;
            var res = new List<CheckBoxItem>(count);
            for (int i = 0; i < count; i++)
            {
                res.Add(new CheckBoxItem() { Text = $"Clip {i}" });
            }

            return res;
        }

        public List<CheckBoxItem> ClipsNums
        {
            get
            {
                return m_ClipsNums;
            }
            set { m_ClipsNums = value; OnPropertyChanged(() => ClipsNums); }
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

    public class TextVM : ViewModelBase
    {
        private string m_Text = "Text Here ...";

        public string Text
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
}