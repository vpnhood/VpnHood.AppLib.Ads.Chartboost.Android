using Com.Chartboost.Sdk;
using Com.Chartboost.Sdk.Callbacks;
using Com.Chartboost.Sdk.Events;
using Com.Chartboost.Sdk.Privacy.Model;
using VpnHood.Core.Common.Exceptions;
using VpnHood.Core.Toolkit.Utils;

namespace VpnHood.AppLib.Ads.Chartboost.Android;

public class ChartboostUtil
{
    private static readonly AsyncLock InitLock = new();
    public static bool IsInitialized { get; private set; }
    public static int RequiredAndroidVersion => 28;
    public static bool IsAndroidVersionSupported => OperatingSystem.IsAndroidVersionAtLeast(RequiredAndroidVersion);

    public static async Task Initialize(Activity activity, string appId, string adSignature,
        TimeSpan timeout, CancellationToken cancellationToken)
    {
        // it looks like there is conflict between this provider SDK and Xamarin.GooglePlayServices.Ads.Identifier 118.2.0
        // However we have decided to stop testing this provider in any android lower than 9.0
        if (!IsAndroidVersionSupported)
            throw new NotSupportedException("Chartboost SDK requires Android 9.0 or higher.");

        using var lockAsync = await InitLock.LockAsync(cancellationToken);
        if (IsInitialized)
            return;

        var sdkStartCallback = new StartCallback();
        Chartboost.AddDataUseConsent(activity, new COPPA(false));
        Chartboost.StartWithAppId(activity, appId, adSignature, sdkStartCallback);

        await sdkStartCallback.Task
            .WaitAsync(timeout, cancellationToken)
            .ConfigureAwait(false);

        IsInitialized = true;
    }

    private class StartCallback : Java.Lang.Object, IStartCallback
    {
        private readonly TaskCompletionSource _initCompletionSource = new();
        public Task Task => _initCompletionSource.Task;

        public void OnStartCompleted(StartError? error)
        {
            if (error != null)
                _initCompletionSource.TrySetException(
                    new LoadAdException(
                        $"Chartboost Ads initialization failed. Error: {error}, ErrorCode: {error.GetCode()}"));
            else
                _initCompletionSource.TrySetResult();
        }
    }
}
