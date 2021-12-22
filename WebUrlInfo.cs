using System.Collections.Generic;

namespace WClipboard.Plugin.LinkedContent.WebUrl
{
    public class WebUrlInfo
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public Queue<string> ImageUrls { get; }
        public string? Redirect { get; set; }

        public WebUrlInfo()
        {
            ImageUrls = new Queue<string>();
        }
    }
}
