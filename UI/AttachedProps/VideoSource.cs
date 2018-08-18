using Newtonsoft.Json;

namespace BitnuaVideoPlayer
{
    public class VideoSource : ViewModelBase
    {
        private readonly long? m_Time;
        private readonly string m_VideoPath;


        [JsonConstructor]
        public VideoSource([JsonProperty(nameof(Path))] string videoPath, [JsonProperty(nameof(Time))] long? time = null)
        {
            m_VideoPath = videoPath;
            m_Time = time;
        }

        public string Path => m_VideoPath;
        public long? Time => m_Time;

        public override bool Equals(object obj)
        {
            var b = (VideoSource)obj;
            return Path == b?.Path;
        }

        public override int GetHashCode()
        {
            return Path?.GetHashCode() ?? 0;
        }

        public static bool operator ==(VideoSource a, VideoSource b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(VideoSource a, VideoSource b)
        {
            return !(a == b);
        }
    }
}