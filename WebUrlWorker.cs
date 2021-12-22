using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WClipboard.Core.Extensions;
using WClipboard.Core.Utilities;

namespace WClipboard.Plugin.LinkedContent.WebUrl
{
    public class WebUrlWorker
    {
        private readonly string start = "<head";
        private readonly string stop = "</head";

        private const int bufferSize = 1024;
        private const int startTimeout = 10000;

        private readonly Regex metaPropertyRegex = new Regex(@"<meta[^>]*(?:(?:property\s*=\s*""([^""]*)""|content\s*=\s*""([^""]*)"")[^>]*){2}>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex metaNameRegex = new Regex(@"<meta[^>]*(?:(?:name\s*=\s*""([^""]*)""|content\s*=\s*""([^""]*)"")[^>]*){2}>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex titleRegex = new Regex(@"<title[^>]*>([^<]*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex linkRegex = new Regex(@"<link[^>]*(?:(?:rel\s*=\s*""([^""]*)""|href\s*=\s*""([^""]*)"")[^>]*){2}>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex spaceRegex = new Regex(@"\s+", RegexOptions.Compiled);

        private readonly HttpClient httpClient;

        public WebUrlWorker(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<WebUrlInfo> GetInformation(WebUrlModel model)
        {
            var info = new WebUrlInfo();
            var uri = model.Url;

            string? title = null;
            string? description = null;
            string? imageLocation = null;

            try
            {
                var respone = await httpClient.GetAsync(model.Url).ConfigureAwait(false);

                if(respone.RequestMessage?.RequestUri != null && respone.RequestMessage?.RequestUri != model.Url)
                {
                    info.Redirect = respone.RequestMessage!.RequestUri.ToString();
                    uri = respone.RequestMessage!.RequestUri;
                }

                if(respone.IsSuccessStatusCode) 
                { 
                    var head = await ReadHead(respone).ConfigureAwait(false);

                    if (head.Length > 10) //some information in head
                    {
                        var metaPropertyMatches = metaPropertyRegex.Matches(head).ToDictionaryLast(m => m.Groups[1].Value.ToLower(), m => m.Groups[2].Value);
                        metaPropertyMatches.TryGetValue("og:title", out title);
                        metaPropertyMatches.TryGetValue("og:description", out description);
                        if (metaPropertyMatches.TryGetValue("og:image", out imageLocation))
                            AddImageLocation(uri, imageLocation, info);

                        if (!string.IsNullOrWhiteSpace(title) && metaPropertyMatches.TryGetValue("og:site_name", out var site_name))
                            title = $"{site_name}: {title}";

                        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
                        {
                            var metaNameMatches = metaNameRegex.Matches(head).ToDictionaryLast(m => m.Groups[1].Value.ToLower(), m => m.Groups[2].Value);
                            if (string.IsNullOrWhiteSpace(title)) metaNameMatches.TryGetValue("title", out title);
                            if (string.IsNullOrWhiteSpace(description)) metaNameMatches.TryGetValue("description", out description);
                        }

                        if (string.IsNullOrWhiteSpace(title))
                        {
                            var titleMatch = titleRegex.Match(head);
                            if (titleMatch.Success)
                            {
                                title = titleMatch.Groups[1].Value;
                            }
                        }

                        var linkRegexMatches = linkRegex.Matches(head).ToDictionaryLast(m => m.Groups[1].Value.ToLower(), m => m.Groups[2].Value);
                        if(linkRegexMatches.TryGetValue("icon", out imageLocation))
                            AddImageLocation(uri, imageLocation, info);

                        if (linkRegexMatches.TryGetValue("shortcut icon", out imageLocation))
                            AddImageLocation(uri, imageLocation, info);
                    }

                    var contentHeaders = respone.Content.Headers;
                    if (string.IsNullOrWhiteSpace(description))
                        description = CheckDownload(contentHeaders);

                    if (string.IsNullOrWhiteSpace(description))
                        description = CheckContentType(contentHeaders);

                    info.Title = string.IsNullOrWhiteSpace(title) ? model.Url.ToString() : WebUtility.HtmlDecode(spaceRegex.Replace(title, " ").Trim());
                    info.Description = string.IsNullOrWhiteSpace(description) ? null : WebUtility.HtmlDecode(description);
                } 
                else
                {
                    info.Title = model.Url.ToString();
                    info.Description = $"Error: {(int)respone.StatusCode} {respone.ReasonPhrase}";
                }
            }
            catch (HttpRequestException ex)
            {
                info.Title = model.Url.ToString();
                info.Description = $"Error: {ex.Message}";
            } 
            catch(TaskCanceledException ex)
            {
                info.Title = model.Url.ToString();
                info.Description = $"Error: {ex.Message}";
            }

            AddImageLocation(uri, "/favicon.ico", info);

            return info;
        }

        private void AddImageLocation(Uri url, string? imageLocation, WebUrlInfo info)
        {
            if (imageLocation is null)
                return;

            if (imageLocation.StartsWith("/"))
            {
                imageLocation = url.GetLeftPart(UriPartial.Authority) + imageLocation;
            }

            info.ImageUrls.Enqueue(imageLocation);
        }

        private async Task<string> ReadHead(HttpResponseMessage response)
        {
            using (var sr = new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(false)))
            {
                var headContent = new StringBuilder();
                bool end = false;
                bool insideHead = false;
                var buffer = new char[bufferSize];
                int progress = 0;
                int currentStreamPosition = 0;

                while (!end)
                {
                    end = await sr.ReadBlockAsync(buffer, 0, buffer.Length).ConfigureAwait(false) < buffer.Length;
                    currentStreamPosition += buffer.Length;

                    if (insideHead)
                    {
                        headContent.Append(buffer);
                    }
                    else if (currentStreamPosition > startTimeout) //In the case that it may not be a html file, stop searching if it reaches the startTimeout 
                    {
                        break;
                    }

                    int index = 0;
                    if (!insideHead)
                    {
                        index = buffer.FirstIndexOf(start, ref progress, index);
                        if (index > int.MinValue)
                        {
                            insideHead = true;
                            index += start.Length;
                            headContent.Append(start);
                            headContent.Append(buffer, index, buffer.Length - index);
                        }
                    }

                    if (insideHead)
                    {
                        index = buffer.FirstIndexOf(stop, ref progress, index);
                        if (index > int.MinValue)
                        {
                            end = true;
                            index += stop.Length;
                            headContent.Length -= buffer.Length - index;
                        }
                    }
                }

                return headContent.ToString();
            }
        }

        private string? CheckDownload(HttpContentHeaders headers)
        {
            if (headers.ContentDisposition?.DispositionType == DispositionTypeNames.Attachment)
            {
                if(headers.ContentDisposition.FileName is not null) { 
                    return $"Download {headers.ContentDisposition.FileName}{CheckContentLength(headers)}";
                }
                return $"Download{CheckContentLength(headers)}";
            }

            return null;
        }

        private string? CheckContentType(HttpContentHeaders headers)
        {
            var mediaType = headers.ContentType?.MediaType;
            
            if(mediaType is not null && !mediaType.Contains("html"))
            {
                mediaType = mediaType[(mediaType.IndexOf("/") + 1)..];

                return $"{mediaType} file{CheckContentLength(headers)}";
            }
            return null;
        }

        private string? CheckContentLength(HttpContentHeaders headers)
        {          

            if (headers.ContentLength.HasValue)
                return $" of {HumanReadable.GetBytesSize(headers.ContentLength.Value)}";
            return "";
        }
    }
}
