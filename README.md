# VpnHood.AppLib.Ads.Chartboost.Android

Chartboost interstitial ads for an Android app built on [VpnHood](https://github.com/vpnhood/VpnHood):
an ad provider that the app's ad service loads and shows like its other ad networks. The package binds
the Chartboost SDK and brings everything the SDK needs at run time, so the app references nothing else
for Chartboost.

## Requirements

- .NET 11 for Android (`net11.0-android37.0`).
- Android 9 (API 28) or later on the device: on an older one the provider refuses to load.
- A Chartboost app id and app signature, and the ad location of an interstitial, from your Chartboost
  dashboard.

## Use

Add the provider to the list the app gets as `AppOptions.AdProviderItems`:

```csharp
using VpnHood.AppLib.Ads.Chartboost.Android;
using VpnHood.AppLib.App.Services.Ads;

if (ChartboostAdProvider.IsAndroidVersionSupported)
    adProviderItems.Add(new AppAdProviderItem {
        AdProvider = ChartboostAdProvider.Create(appId, appSignature, adLocation,
            initializeTimeout: TimeSpan.FromSeconds(5))
    });
```

The first ad it loads starts the Chartboost SDK, which has `initializeTimeout` to finish.

## Privacy

The provider tells Chartboost that the app is not directed at children (COPPA). An app that is must
not use it as it is.
