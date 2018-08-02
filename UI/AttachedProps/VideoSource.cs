using Newtonsoft.Json;

namespace BitnuaVideoPlayer
{
    public struct VideoSource
    {
        private readonly long? m_Time;
        private readonly string m_VideoPath;


        [JsonConstructor]
        public VideoSource([JsonProperty(nameof(Path))] string videoPath, [JsonProperty(nameof(Time))] long? time = null) : this()
        {
            m_VideoPath = videoPath;
            m_Time = time;
        }

        public string Path => m_VideoPath;
        public long? Time => m_Time;
    }
}