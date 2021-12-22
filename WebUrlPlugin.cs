using Microsoft.Extensions.DependencyInjection;
using WClipboard.Core.DI;
using WClipboard.Core.WPF.Extensions;
using WClipboard.Core.WPF.LifeCycle;
using WClipboard.Core.WPF.Managers;
using WClipboard.Plugin.ClipboardImplementations.Text.LinkedContent;
using WClipboard.Plugin.LinkedContent.WebUrl.Text;

namespace WClipboard.Plugin.LinkedContent.WebUrl
{
    public sealed class WebUrlPlugin : IPlugin, IAfterWPFAppStartupListener
    {
        public string Name => "WebUrl";

        public WebUrlPlugin() { }

        public void AfterWPFAppStartup()
        {
            var serviceProvider = DiContainer.SP!;
            serviceProvider.AddTypeDateTemplate<WebUrlViewModel>("WebUrlView.xaml");
        }

        public void ConfigureServices(IServiceCollection services, IStartupContext context)
        {
            services.AddViewModelFactory<WebUrlViewModelFactory>();
            services.AddSingleton<WebUrlWorker>();

            services.AddSingleton<ILinkedTextContentFactory, WebUrlLinkedContentFactory>();
        }
    }
}
