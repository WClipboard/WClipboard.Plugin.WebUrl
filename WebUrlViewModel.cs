using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WClipboard.Core.WPF.Utilities;
using WClipboard.Core.WPF.ViewModels;

namespace WClipboard.Plugin.LinkedContent.WebUrl
{
    public class WebUrlViewModel : BaseViewModel<WebUrlModel>
    {
        public static ICommand OpenCommand { get; } = SimpleCommand.Create<Uri>(OpenUrl);

        private string? _title;
        public string? Title { 
            get => _title; 
            set => SetProperty(ref _title, value); 
        }

        private string? _description;
        public string? Description { 
            get => _description;
            set => SetProperty(ref _description, value); 
        }

        private BitmapSource? _image;
        public BitmapSource? Image { 
            get => _image; 
            set => SetProperty(ref _image, value); 
        }

        private string? _redirect;
        public string? Redirect
        {
            get => _redirect;
            set => SetProperty(ref _redirect, value);
        }

        public WebUrlViewModel(WebUrlModel model, WebUrlWorker worker, IImageDownloader imageDownloader) : base(model)
        {
            Init(worker, imageDownloader);
        }

        private async void Init(WebUrlWorker worker, IImageDownloader imageDownloader)
        {
            var info = await worker.GetInformation(Model).ConfigureAwait(false);
            Title = info.Title;
            Description = info.Description;
            Redirect = info.Redirect;

            foreach(var image in info.ImageUrls)
            {
                try
                {
                    Image = await imageDownloader.DownloadImageAsync(image).ConfigureAwait(false);
                    break;
                }
                catch (System.Net.WebException) 
                {
                    //We don't care, just dont show the image
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    //We don't care, just dont show the image
                }
                catch (Exception ex)
                {
                    //We don't care, just dont show the image
                }
            }
        }

        private static void OpenUrl(Uri uri)
        {
            try
            {
                Process.Start(new ProcessStartInfo(uri.ToString())
                {
                    UseShellExecute = true
                });
            }
            catch (Win32Exception ex)
            {
                //ErrorCode for No application is associated with the specified file for this operation
                if (ex.ErrorCode == -2147467259)
                {
                    Process.Start("rundll32.exe", $"shell32.dll, OpenAs_RunDLL {uri}");
                }
            }
        }
    }
}
