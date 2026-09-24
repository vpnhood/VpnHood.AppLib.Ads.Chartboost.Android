using _Microsoft.Android.Resource.Designer;
using VpnHood.AppLib.Ads.Chartboost.Android;
using VpnHood.Core.Client.Devices.Android;
using VpnHood.Core.Client.Devices.Android.ActivityEvents;

namespace Sample;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : ActivityEvent
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        // Set our view from the "main" layout resource
        SetContentView(ResourceConstant.Layout.activity_main);
        _ = Foo();


    }

    // Test
    private async Task Foo()
    {
        try
        {
            await Task.CompletedTask;
            var adService = ChartboostAdProvider.Create(
                ReadUserValue("app_id.txt"),
                ReadUserValue("app_signature.txt"),
                ReadUserValue("ad_location.txt"),
                TimeSpan.FromSeconds(5));

            await adService.LoadAd(new AndroidUiContext(this), CancellationToken.None);
            var result = await adService.ShowAd(new AndroidUiContext(this), customData: "", CancellationToken.None);
            Console.WriteLine($"Chartboost ShowAd result: {result}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    // The ids live in the private .user folder beside this repo, one value per file
    // (.user/ad/Chartboost); the csproj embeds each one by its file name.
    private static string ReadUserValue(string fileName)
    {
        using var stream = typeof(MainActivity).Assembly.GetManifestResourceStream(fileName)
            ?? throw new InvalidOperationException($"{fileName} is missing. Put it in .user/ad/Chartboost beside this repo.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Trim();
    }
}