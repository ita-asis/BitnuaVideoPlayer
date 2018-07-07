using CefSharp;
using CefSharp.Wpf;
using ColorFont;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace BitnuaVideoPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class PresentaionWindow : Window
    {
        private MainViewModel VM;
        public PresentaionWindow()
        {
            InitializeComponent();

            DataContextChanged += PresentaionWindow_DataContextChanged;
            Loaded += (s, e) => { InitAll(); };
            MouseDown += Window_MouseDown;
        }

        private void PresentaionWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is MainViewModel)
                ((MainViewModel)e.OldValue).PropertyChanged -= VM_PropertyChanged;

            VM = e.NewValue as MainViewModel;
            VM.PropertyChanged += VM_PropertyChanged;
            VM.Banner.PropertyChanged += VM_PropertyChanged;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private bool m_IsLeftPicRegistered;
        private void VM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender == VM.Banner)
            {
                InitBanner();
            }
        }

        void InitAll()
        {
            InitPlayer();
            InitBanner();
            InitLeftPic();
        }

        private void InitBanner()
        {
            var html =
                $@"<html><head>
                <meta charset=utf-8>
                <style>
                    body {{
                        overflow: -moz-scrollbars-vertical;
                        overflow-x: hidden;
                        overflow-y: auto;
                        background-color: rgba({VM.Banner.BackColor.R},{VM.Banner.BackColor.G},{VM.Banner.BackColor.B},{VM.Banner.BackColor.A});
                    }}
                    .parent {{
                        height: 40px;
                        text-align: center;
                    }}
                    .parent > .child {{
                        line-height: 50px;
                    }}
                    marquee {{
                        font-size: {VM.Banner.Font.Size}px;
                        font-family: {VM.Banner.Font.FontFamily.Name};
                        font-style: {(VM.Banner.Font.Italic ? "italic" :"normal")};
                        font-weight: {(VM.Banner.Font.Bold ? "bold" : "normal")};
                        text-decoration: {(VM.Banner.Font.Underline ? "underline" : "")} {(VM.Banner.Font.Strikeout ? "line-through" : "")};
                        color: rgba({VM.Banner.ForeColor.R},{VM.Banner.ForeColor.G},{VM.Banner.ForeColor.B},{VM.Banner.ForeColor.A});
                    }}

                </style>
                </head><body>
                        <div class=""parent"">
                            <marquee class=""child"" scrolldelay=""{VM.Banner.Speed}"" behavior=""scroll"" direction=""{VM.Banner.Direction}"">{ VM.Banner.Text}</marquee>
                        </div>
                </body></html>";

            browser.Dispatcher.BeginInvoke(new Action(() =>
            {
                browser.LoadHtml(html);
            }), System.Windows.Threading.DispatcherPriority.DataBind);
        }

        private void InitPlayer()
        {
            var player = VlcControl.MediaPlayer;
            player.VlcLibDirectoryNeeded += OnVlcControlNeedsLibDirectory;
            player.EndInit();

            // This can also be called before EndInit
            player.Log += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine(string.Format("libVlc : {0} {1} @ {2}", args.Level, args.Message, args.Module));
            };

            VlcControl.MouseDown += Window_MouseDown;
            player.Audio.IsMute = true;
            player.Play(new Uri(@"D:\Videos\Movies\Baby Driver (2017) [YTS.AG]\Baby.Driver.2017.720p.BluRay.x264-[YTS.AG].mp4"));
            player.EndReached += (s, e) => { player.Play(new Uri(@"D:\Videos\Overwolf\Desktop 02-28-2017 15-20-36-931.mp4")); };
        }

        private void InitLeftPic()
        {

            VM.LeftPicSource = @"D:\Videos\Overwolf\Thumbnails\Desktop 01-23-2017 16-11-07-290.jpg";

            if (VM.LeftPicTitle == null)
            {
                VM.LeftPicTitle = new ViewModels.TextVM()
                {
                    Text = "Sample Composer Name",
                    Font = new Font("Times New Roman",
                                                26f,
                                                System.Drawing.FontStyle.Regular,
                                                GraphicsUnit.Pixel
                                    ),
                    BackColor = Color.AliceBlue,
                    ForeColor = Color.Goldenrod
                };
            }
            else
            {
                VM.LeftPicTitle.Text = "Sample Composer Name";
            }
        }

        private void OnVlcControlNeedsLibDirectory(object sender, Vlc.DotNet.Forms.VlcLibDirectoryNeededEventArgs e)
        {
            var currentAssembly = Assembly.GetEntryAssembly();
            var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
            if (currentDirectory == null)
                return;
            if (IntPtr.Size == 4)
                e.VlcLibDirectory = new DirectoryInfo(Path.Combine(currentDirectory, @"libvlc\win-x86\"));
            else
                e.VlcLibDirectory = new DirectoryInfo(Path.Combine(currentDirectory, @"libvlc\win-x64\"));
        }
    }
}
