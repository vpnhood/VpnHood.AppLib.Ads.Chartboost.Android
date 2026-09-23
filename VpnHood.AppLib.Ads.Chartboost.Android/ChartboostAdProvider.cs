using Com.Chartboost.Sdk.Ads;
using Com.Chartboost.Sdk.Callbacks;
using Com.Chartboost.Sdk.Events;
using VpnHood.AppLib.Abstractions.Ads;
using VpnHood.AppLib.Abstractions.Ads.AdExceptions;
using VpnHood.Core.Client.Devices.Abstractions.UiContexts;
using VpnHood.Core.Client.Devices.Android;
using VpnHood.Core.Client.Devices.Android.Utils;

namespace VpnHood.AppLib.Ads.Chartboost.Android;

public class ChartboostAdProvider(string appId, string adSignature, string adLocation, TimeSpan initializeTimeout)
    : IAdProvider
{
    private Interstitial? _chartboostInterstitialAd;
    private MyInterstitialCallBack? _myInterstitialCallBack;

    public string NetworkName => "Chartboost";
    public AdType AdType => AdType.InterstitialAd;
    public DateTime? AdLoadedTime { get; private set; }
    public TimeSpan AdLifeSpan { get; } = TimeSpan.FromMinutes(45);
    public static int RequiredAndroidVersion => ChartboostUtil.RequiredAndroidVersion;
    public static bool IsAndroidVersionSupported => ChartboostUtil.IsAndroidVersionSupported;

    public static ChartboostAdProvider Create(string appId, string adSignature, string adLocation, TimeSpan initializeTimeout)
    {
        var ret = new ChartboostAdProvider(appId, adSignature, adLocation, initializeTimeout);
        return ret;
    }

    public async Task LoadAd(IUiContext uiContext, CancellationToken cancellationToken)
    {
        var appUiContext = (AndroidUiContext)uiContext;
        var activity = appUiContext.Activity;
        if (activity.IsDestroyed)
            throw new LoadAdException("MainActivity has been destroyed before loading the ad.");

        // initialize
        await ChartboostUtil.Initialize(activity, appId, adSignature, initializeTimeout, cancellationToken);

        // reset the last loaded ad
        AdLoadedTime = null;

        // Load a new Ad
        _myInterstitialCallBack = new MyInterstitialCallBack();
        _chartboostInterstitialAd = new Interstitial(adLocation, _myInterstitialCallBack, null);
        _chartboostInterstitialAd.Cache();

        await _myInterstitialCallBack.LoadTask
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        // the app ages ads against UTC
        AdLoadedTime = DateTime.UtcNow;
    }

    public async Task<ShowAdResult> ShowAd(IUiContext uiContext, string? customData, CancellationToken cancellationToken)
    {
        var appUiContext = (AndroidUiContext)uiContext;
        var activity = appUiContext.Activity;
        if (activity.IsDestroyed)
            throw new ShowAdException("MainActivity has been destroyed before showing the ad.");

        var interstitialAd = _chartboostInterstitialAd;
        var callBack = _myInterstitialCallBack;
        try
        {
            if (AdLoadedTime == null || interstitialAd == null || callBack == null)
                throw new ShowAdException($"The {AdType} has not been loaded.");

            await AndroidUtils.RunOnUiThread(activity, () => interstitialAd.Show())
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            // wait until the ad is dismissed; a click before that makes it a Clicked result
            return await callBack.DismissedTask
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            interstitialAd?.ClearCache();
            _chartboostInterstitialAd = null;
            AdLoadedTime = null;
        }
    }

    private class MyInterstitialCallBack : Java.Lang.Object, IInterstitialCallback
    {
        private bool _isClicked;

        private readonly TaskCompletionSource _loadedCompletionSource = new();
        public Task LoadTask => _loadedCompletionSource.Task;

        private readonly TaskCompletionSource<ShowAdResult> _dismissedCompletionSource = new();
        public Task<ShowAdResult> DismissedTask => _dismissedCompletionSource.Task;

        public void OnAdClicked(ClickEvent e, ClickError? error)
        {
            if (error == null)
                _isClicked = true;
        }

        public void OnAdLoaded(CacheEvent e, CacheError? error)
        {
            if (error != null)
                _loadedCompletionSource.TrySetException(new LoadAdException(
                    $"Chartboost ad failed to load. Error: {error}, ErrorCode: {error.GetCode()}"));
            else
                _loadedCompletionSource.TrySetResult();
        }

        public void OnAdRequestedToShow(ShowEvent e)
        {

        }

        public void OnAdShown(ShowEvent e, ShowError? error)
        {
            if (error != null)
                _dismissedCompletionSource.TrySetException(new ShowAdException(
                    $"Chartboost ad failed to show. Error: {error}, ErrorCode: {error.GetCode()}"));
        }

        public void OnImpressionRecorded(ImpressionEvent e)
        {

        }

        public void OnAdDismiss(DismissEvent e)
        {
            _dismissedCompletionSource.TrySetResult(_isClicked ? ShowAdResult.Clicked : ShowAdResult.Closed);
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
