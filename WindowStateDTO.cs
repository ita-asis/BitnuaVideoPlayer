using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitnuaVideoPlayer
{
    [Serializable]
    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class WindowStateDTO
    {
        public double Height { get; set; }
        public double Width { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
    }
}
