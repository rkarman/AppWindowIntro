# Welcome, AppWindow!
_This blogpost refers to APIs available in Windows 10 SDK Preview Build 18327, for devices running on the same or later Windows Insider build._

At //build/ last year I had the privilege to share our future-looking Windowing story for UWP with our community. In general, we got positive feedback and excitement among the partners we reached out to for deep-dives and we were stoked to finally be shipping updates to this area! However, we also got feedback that led us to re-think some of our concepts and choices for the API surface we had presented. This led to us deciding to postpone the general release of the API by one iteration of the developer platform. What I’m about to present in this blog post (and others that will follow) is going to look a little different from what you saw and heard at //build/, but hopefully you’ll agree with me that what we have here are easier to use APIs and more straight-forward concepts.

Let’s start with a few disclaimers though. The API surface is by no means fully featured yet, we know that we still have a long way to go to get to that north-star vision of what modern windowing should be capable of in Windows. The API surface also have some limitations in what you can do with content in these new windows, and you will still have a CoreWindow/ApplicaitonView for that first window that gets created for your app. Having said this, I think we’re still off to a pretty good start.

One of the main scenarios we’re want to achieve with this initial version of our new windowing API is to make it easier to create multi-window experiences in your UWP apps, and we do this by removing one of the major hurdles that has been part of multi-window for UWP since its inception – that each window must have its own UI thread.

With the introduction of our new window class, AppWindow, we remove that bar completely. All AppWindows you create run on the same UI thread from which you created them.

This post is only meant to be a quick introduction to the new API so that you can start trying it out, it is not meant to be an exhaustive description of the underlying architecture, design choices, limitations or functionality of each method/property.

Without further ado, let’s get to the thing you’re here for – the code!

### Creating an AppWindow, connecting content, and showing it.
```C#
private async void ShowNewWindow(object sender, RoutedEventArgs e)
{
    // Create a new window
    appWindow = await AppWindow.TryCreateAsync();
    // Navigate our frame to the page we want to show in the new window
    appWindowFrame.Navigate(typeof(AppWindowMainPage));
    // Attach the XAML content to our window
    ElementCompositionPreview.SetAppWindowContent(appWindow, appWindowFrame);
    // Show the window
    appWindow.TryShowAsync();
}
```

Well look at that. Sure looks a lot easier than what you’d be doing with ApplicationViews, right? Let’s dissect the code and talk about the different parts in more detail.

First off, creating the AppWindow

As you can see from the snippet the actual window has been declared outside of the method, I’ve done this to make it globally accessible in my little sample app, but you can of course pass it around among the methods as you see fit.
```C#
AppWindow appWindow = await AppWindow.TryCreateAsync();
```

TryCreateAsync? Yes, we’re async here. The reason being that there are a lot of things happening behind the scene that could potentially be blocking your UI thread for a (relatively) long period of time here. By the time the AppWindow is accessible to you, all properties that needs to be populated by the system will be so and any method on the object can now be called. There is no delayed-initialization to take into account, once the async operation completes you’re good to go!

Second, attaching content to the window

Again, a Frame object has been defined globally for the page so that it sticks around when we exit the ShowNewWindow method. The snippet above is in other words just missing this line of code:
```C#
Frame appWindowFrame = new Frame();
```

What about that ElementCompositionPreview call, it has “preview” in its name, does that mean that this is all preview APIs? No. What this means is that the final shape of how to generically connect content to AppWindow has not been settled yet and instead of delaying the entire feature until a time when we have that in place, we decided to create a specific “connector” API for XAML that you can use for now. We’ll have more to share on the long-term story here later this year, so stay tuned.
Does this mean that I cannot select other content to present in these windows? Yes, that is true. For now you can only attach XAML content to your AppWindows, in other words you have to be “a XAML app” to use AppWindow for now. However, feel free to add a SwapChainPanel control to your AppWindow and work with content inside that if that is what you want to do.
Any type of XAML content can be attached to AppWindow, you are not limited to having to create a Frame and Page for your content. You could, if you really wanted to, create and show an AppWindow that contains only a Button...
```C#
appWindow = await AppWindow.TryCreateAsync();
Button myButton = new Button();
myButton.Content = "Click me!";
ElementCompositionPreview.SetAppWindowContent(appWindow, myButton);
appWindow.TryShowAsync();
```
But who would ever want to do that? 😉

Lastly, showing the window

Another async method? That is correct. The show operation is async as well since there are policy evaluations being done to see if the show can be satisfied (the user might have locked the device, or the app have lost its foreground rights, in which case the call to show the window will be denied). However, if your app does not do anything specific in case of a declined show (which would be the general app logic case, and the platform declining a show is a rare exception), feel free to not await this call.

That’s it. You now how a second window up for your app, and all your in-app logic continues to run on the same thread for this window. Easy-peasy!

### Lifetime of multi-windowed apps
Now that we have two windows in our app we need to talk about lifetime management. Ah yes, nothing in this world comes for free now does it?

#### The CoreWindow/ApplicationView
Once you have multiple windows in your app the ApplicationView starts to behave a little different. First off, when the user closes the ApplicationView window and an AppWindow is still accessible in the system (can be minimized, on another virtual desktop, or hidden behind some other window, so not necessarily currently visible to the user) the app does not get suspended. Instead a new event fires on the ApplicationView called “Consolidated”. This event lets you know that it is now safe to release any resources that were associated with the ApplicationView (unloading your content, let go of exclusive resources such as cameras, stop playing audio, etc.). If you are familiar with multi-threaded multi-window apps this is nothing new, this is the same behavior as when you have multiple ApplicationView windows in your app.

However, if the ApplicationView is the last window to get closed the app will suspend as normal and you will NOT get a Consolidated event for this window. So do not move all your shutdown code into the Consolidated event handler when you go multi-window!

If the ApplicationView gets closed, but there is still at least one AppWindow accessible to the user, the CoreWindow will live on. This means that just because your ApplicationView closed you cannot close the CoreWindow itself. The CoreWindow is being used to back the AppWindows, so we need it to stick around. In other words, if you are adding AppWindows to an app that already have multiple CoreWindow/ApplicationViews you need to pay attention to when to call CoreWindow.Close and when not to do it. For the purpose of this blog post though, I will not dive deeper into this scenario, but I will be returning to it in a future post.

#### The AppWindow
The AppWindow lifetime is more straight forward. Once it has been created and shown you will always get a Closed event when it is being…well…closed. The concept of “Consolidation” does not exist for AppWindow. It is either accessible to the user, or it is closed. Again if this was the last window to be closed for the app, the AppWindow will not get a Closed event and instead the suspend event will fire for the app.

If the AppWindow was the last window to be closed for a single-CoreWindow based app (i.e. the ApplicationView had been closed before the AppWindow, and you had not created any secondary ApplicationViews) the system will close the CoreWindow for you.

#### Activation
If you ran ahead of me and just checked all the completions for the API surface in your code editor, you will already have noticed that the equivalent of CoreWindow.Activated is not present on the AppWindow. In other words, the AppWindow itself does not have a method for figuring out if it has been activated or not, so how do you know if you should be rendering, playing audio, etc?

Your AppWindow will in most cases get foreground when TryShowAsync is called, this is the golden path case when you show a window as a result of a user interaction with another window in your app. Now, a lot can happen between the user interacting with your window which may result in your new window being displayed but not taking foreground (this is true for ApplicationView as well), so you cannot solely rely on the visibility of your window to make these decisions.

Enter the new InputActivationListener. The introduction of this class and the future of input APIs deserves a whole post for itself, so for the purpose of this quick intro I’ll just give you a very brief description.

The InputActivationListener is used to get information about activation of your AppWindow, in the current release it will not support any other type of window. You can get to the InputActivationListener object for your AppWindow from the InputActivationListenerPreview class. Once you have an instance you register for the InputActivationChanged and this will give you the information on what just happened to your window (activated, activated but not foreground, or deactivated).
```C#
void SetupListenersForWindow(AppWindow window)
{
    InputActivationListener activationListener = InputActivationListenerPreview.CreateForApplicationWindow(window);
    activationListener.InputActivationChanged += ActivationListener_InputActivationChanged;
}

private void ActivationListener_InputActivationChanged(InputActivationListener sender, InputActivationListenerActivationChangedEventArgs args)
{
    switch (args.State)
    {
        case InputActivationState.ActivatedInForeground:
            // The user will be interacting with this window, so make sure the full user experience is running
            break;
        case InputActivationState.ActivatedNotForeground:
            // The window is showing, but the user is interacting with another window, adjust accordingly
            break;
        case InputActivationState.Deactivated:
            // The user moved on, they have switched to another window, time to go back to inactive state.
            break;
        default:
            break;
    }
}
```
Yes, another preview method here. This is the same reason as for the XAML preview API above – we have a north-star architecture that we are rolling out in stages, and in order to not block shipping on every piece of the puzzle being ready we need the preview classes. The scenario here is not preview, just the way to get to the object for an AppWindow for now.

For the activation type (pointer or code activated), you listen to the XAML Window.Activated event just as you do for an ApplicationView with XAML.

### Give me a project to play around with already!
The snippets in this README are from the “quick and dirty” sample for getting a second window on screen that you can find here. The project is ApiTeaser.sln and all the funtionality is in the MainPage.xaml.cs file.

**Note:** Please don’t submit PRs, I am unlikely to accept them since this really isn’t meant to be anything else than a quick-start sample for you to start experimenting with the feature. Stay tuned for more complete samples targeting specific scenarios and functionality though, and the blogposts/readmes to go along with them.

## Wait... There’s one more thing.
_“I saw your twitter replies about UIContext, what is this one about?”_
OK, so there is this new currency in the platform called UIContext, let’s take a quick look at what it is and why we’re introducing it.
Previously we’re always had a thread for each window (for which the currency name was “view”), and all UI content was parented within that window, which made it easy to identify UI content as “for current view”. Hence a lot of “GetForCurrentView” in our API surfaces.

Now that we introduce multiple “islands of content”, parented to different windows but running on the same thread, the “GetForCurrentView” no longer does the job for identifying which of the windows it was you really wanted to “get the object/resource for”.

Enter UIContext. UIContext is our new currency for identifying which “island of content” a request was made for. Each AppWindow has a UIContext and anything parented to this window will have the same UIContext.

In XAML you can get to the UIContext directly from your element. From our sample above it would be “appWindowFrame.UIContext” which you can then use to identify which AppWindow a piece of content is parented to so that you go and update the right UI at the right time.

This currency can also used by APIs where you previously would use GetForCurrentView to retrieve an instance. These APIs will now have a GetForUIContext method, to which you can pass in your UIContext and get the object associated with your window.

For example, the InputPane is a per-window specific object, and to get the correct one for an AppWindow you would follow this pattern:
```C#
private void TestInputPaneAw(AppWindow window)
{
    var myInputPane = InputPane.GetForUIContext(window.UIContext);

    var occlusionRect = myInputPane.OccludedRect;

    // Adjust various scrollers to move edit controls out from on-screen keyboard.
}
```

# Important notes about the state of functionality for AppWindows
Please note that for this release not all GetForCurrentView APIs have been updated to also work for UIContext. We’re rolling this out in stages, please let us know if there are certain APIs that you consider higher priority and we’ll be happy to incorporate that feedback in our plans.

### Known major limitations for the current Windows Insider Build and Universal SDK
Like I said above, this is a v1 of the API surface and you should consider the current SDK state to be “preview”. There are a lot of limitations to what works and what does not. We are working on fixing these bugs for the public release, some of them are already fixed but not flighted yet.

* The XAML WebView control does not work, input will not be received and content will stop rendering if the CoreWindow is minimized or consolidated.

* The XAML MediaElement control will stop playback if the CoreWindow is minimized or consolidated

* The XAML Maps control will show context menus offset to the CoreWindow instead of offset to the AppWindow, or at 0,0 of the main display if the CoreWindow is consolidated.

* In some cases, trying to position an AppWindow at a specific position within a mutli-display system may end up in it being positioned at 0,0 on the main display.
If you set a specific size of the AppWindow but not a position, the system will position it at 0,0 of the current display.

### Caveats and differences from ApplicationView to be aware of
The size of an AppWindow is that of its frame, in DisplayRegion coordinates – not client coordinates! It is also including the TitleBar. There is no straight-forward way to get the height of the TitleBar for a normal AppWindow without going full customization of it, which makes programmatically sizing a window to a “pixel perfect size” hard.

DisplayRegion coordinates are currently in physical pixels. This may change. In order to translate size between the content inside your window and the size of your AppWindow frame, use the XAMLRoot.RasterizationScale of your topmost UIElement in your AppWindow content to calculate the frame size.
