using _Microsoft.Android.Resource.Designer;
using VpnHood.AppLib.Ads.Chartboost.Android;
using VpnHood.Core.Client.Device.Droid;
using VpnHood.Core.Client.Device.Droid.ActivityEvents;

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
                ChartboostCredential.AppId, 
                ChartboostCredential.AdSignature, 
                ChartboostCredential.AdLocation,
                TimeSpan.FromSeconds(5));

            await adService.LoadAd(new AndroidUiContext(this), CancellationToken.None);
            await adService.ShowAd(new AndroidUiContext(this), customData: "", CancellationToken.None);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}