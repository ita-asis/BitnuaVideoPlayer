using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitnuaVideoPlayer.UI.Converters
{
    public class PresentationItemConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(PresentationItem).IsAssignableFrom(objectType);
        }
        public override bool CanWrite => false;


        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject item = JObject.Load(reader);
            var strKind = item["Kind"].Value<string>();
            var itemKind = (ePresentationKinds)Enum.Parse(typeof(ePresentationKinds), strKind);
            Type itemType = null;
            if (itemKind == ePresentationKinds.Picture)
            {
                itemType = typeof(PictureItem);
            }
            else if (itemKind == ePresentationKinds.PictureList)
            {
                itemType = typeof(PictureListItem);
            }
            else if (itemKind == ePresentationKinds.VideoList)
            {
                itemType = typeof(VideoListItem);
            }
            else if (itemKind == ePresentationKinds.Video)
            {
                itemType = typeof(VideoItem);
            }
            else if (itemKind == ePresentationKinds.YoutubeVideo)
            {
                itemType = typeof(YoutubeVideoItem);
            }
            else if (itemKind == ePresentationKinds.AmpsLive)
            {
                itemType = typeof(AmpsPresentationItem);
            }

            existingValue = Convert.ChangeType(existingValue, itemType);
            existingValue = Activator.CreateInstance(itemType);

            serializer.Populate(item.CreateReader(), existingValue);
            return existingValue;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
