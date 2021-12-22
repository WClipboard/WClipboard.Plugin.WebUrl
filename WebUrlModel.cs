using System;
using System.Text.RegularExpressions;
using WClipboard.Plugin.ClipboardImplementations.Text.LinkedContent;

namespace WClipboard.Plugin.LinkedContent.WebUrl
{
    public class WebUrlModel
    {
        public Uri Url { get; }

        public WebUrlModel(Uri url)
        {
            Url = url;
        }

        public static ILinkedTextContent GetLinkedTextContent(Capture capture)
        {
            return new LinkedTextContent<WebUrlModel>(capture, new WebUrlModel(new Uri(capture.Value)), "Web url");
        }
    }
}
