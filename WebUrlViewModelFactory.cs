using WClipboard.Core.WPF.Utilities;
using WClipboard.Core.WPF.ViewModels;

namespace WClipboard.Plugin.LinkedContent.WebUrl
{
    public class WebUrlViewModelFactory : ViewModelFactory<WebUrlModel>
    {
        private readonly WebUrlWorker worker;
        private readonly IImageDownloader imageDownloader;

        public WebUrlViewModelFactory(WebUrlWorker worker, IImageDownloader imageDownloader)
        {
            this.worker = worker;
            this.imageDownloader = imageDownloader;
        }

        public override BaseViewModel<WebUrlModel> Create(WebUrlModel model, object? parent)
        {
            return new WebUrlViewModel(model, worker, imageDownloader);
        }
    }
}
