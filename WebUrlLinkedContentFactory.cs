using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WClipboard.Core.Utilities;
using WClipboard.Plugin.ClipboardImplementations.Text;
using WClipboard.Plugin.ClipboardImplementations.Text.LinkedContent;

namespace WClipboard.Plugin.LinkedContent.WebUrl.Text
{
    public class WebUrlLinkedContentFactory : BaseLinkedTextContentFactory
    {
        public WebUrlLinkedContentFactory() : base(new[] { new Regex(@"\b(http(?:s)?:\/\/|www\.)[0-9a-z.\-_~+#,%&=*;:@?/]+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture) })
        {
        }

        public override Task<ILinkedTextContent?> Create(TextClipboardImplementation textClipboardImplementation, Regex regex, Match match)
        {
            try
            {
                return Task.FromResult<ILinkedTextContent?>(WebUrlModel.GetLinkedTextContent(match));
            } 
            catch(UriFormatException)
            {
                Logger.Log(LogLevel.Info, $"{match.Value} is not a valid {nameof(Uri)}");
                return Task.FromResult<ILinkedTextContent?>(null);
            }
        }
    }
}
