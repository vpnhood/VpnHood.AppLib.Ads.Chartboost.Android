using VpnHood.AppLib.Abstractions;
using VpnHood.Core.Client.Device.Droid;
using VpnHood.Core.Client.Device.Droid.Utils;
using VpnHood.Core.Common.Exceptions;
using Com.Chartboost.Sdk.Ads;
using Com.Chartboost.Sdk.Callbacks;
using Com.Chartboost.Sdk.Events;
using VpnHood.Core.Client.Device.UiContexts;

namespace VpnHood.AppLib.Ads.Chartboost.Android;

public class ChartboostAdProvider(string appId, string adSignature, string adLocation, TimeSpan initializeTimeout) 
    : IAppAdProvider
{
    private Interstitial? _chartboostInterstitialAd;
    private MyInterstitialCallBack? _myInterstitialCallBack;

    public string NetworkName => "Chartboost";
    public AppAdType AdType => AppAdType.InterstitialAd;
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
            throw new AdException("MainActivity has been destroyed before loading the ad.");

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

        AdLoadedTime = DateTime.Now;
    }

    public async Task ShowAd(IUiContext uiContext, string? customData, CancellationToken cancellationToken)
    {
        var appUiContext = (AndroidUiContext)uiContext;
        var activity = appUiContext.Activity;
        if (activity.IsDestroyed)
            throw new AdException("MainActivity has been destroyed before showing the ad.");

        try
        {
            if (AdLoadedTime == null || _chartboostInterstitialAd == null || _myInterstitialCallBack == null)
                throw new AdException($"The {AdType} has not been loaded.");

            await AndroidUtil.RunOnUiThread(activity, () => _chartboostInterstitialAd.Show())
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            // wait for show or dismiss
            await _myInterstitialCallBack.ShownTask
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _chartboostInterstitialAd?.ClearCache();
            _chartboostInterstitialAd = null;
            AdLoadedTime = null;
        }
    }

    private class MyInterstitialCallBack : Java.Lang.Object, IInterstitialCallback
    {
        private readonly TaskCompletionSource _loadedCompletionSource = new();
        public Task LoadTask => _loadedCompletionSource.Task;

        private readonly TaskCompletionSource _shownCompletionSource = new();
        public Task ShownTask => _shownCompletionSource.Task;

        public void OnAdClicked(ClickEvent e, ClickError? error)
        {
        }

        public void OnAdLoaded(CacheEvent e, CacheError? error)
        {
            if (error != null)
                _loadedCompletionSource.TrySetException(new LoadAdException(
                    $"Chartboost Ads initialization failed. Error: {error}, ErrorCode: {error.GetCode()}"));
            else
                _loadedCompletionSource.TrySetResult();
        }

        public void OnAdRequestedToShow(ShowEvent e)
        {

        }

        public void OnAdShown(ShowEvent e, ShowError? error)
        {
            if (error != null)
                _shownCompletionSource.TrySetException(new LoadAdException(
                    $"Chartboost Ads show failed. Error: {error}, ErrorCode: {error.GetCode()}"));
        }

        public void OnImpressionRecorded(ImpressionEvent e)
        {

        }

        public void OnAdDismiss(DismissEvent e)
        {
            _shownCompletionSource.TrySetResult();
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}