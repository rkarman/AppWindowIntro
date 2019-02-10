using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml.Hosting;
using Windows.Foundation.Metadata;
using Windows.UI.Input.Preview;
using Windows.UI.Input;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AppWindowIntro
{
    /// <summary>
    /// Main page for our little experiment. This first page of the app is still a CoreWindow+ApplicationView.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        AppWindow appWindow;
        Frame appWindowFrame = new Frame();

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void ShowNewAppWindow_Click(object sender, RoutedEventArgs e)
        {
            // Only ever create and show one window. If the AppWindow exists call TryShow on it to bring it to foreground.
            if (appWindow == null)
            {
                // Create a new window
                appWindow = await AppWindow.TryCreateAsync();
                // Navigate our frame to the page we want to show in the new window
                appWindowFrame.Navigate(typeof(AppWindowMainPage));
                // Attach the XAML content to our window
                ElementCompositionPreview.SetAppWindowContent(appWindow, appWindowFrame);
                // Set up event handlers for the window
                // This is not really needed for this demo, but I mention this in the blogpost so it's here for reference.
                RegisterEventHandlersForWindow(appWindow);
                // Let's make this new window 500x500, just to show that the new window doesn't have to be of the same size as the main app window
                appWindow.RequestSize(new Windows.Foundation.Size(500, 500));
                // Show the window
                appWindow.TryShowAsync();
            }
            else
            {
                appWindow.TryShowAsync();
            }
        }

        void RegisterEventHandlersForWindow(AppWindow window)
        {
            //Set up the activation handler
            InputActivationListener activationListener = InputActivationListenerPreview.CreateForApplicationWindow(window);
            activationListener.InputActivationChanged += ActivationListener_InputActivationChanged;

            // Make sure we release the reference to this window, and release XAML resources, when it's closed
            appWindow.Closed += delegate { appWindow = null; appWindowFrame.Content = null; };
        }

        private void ActivationListener_InputActivationChanged(InputActivationListener sender, InputActivationListenerActivationChangedEventArgs args)
        {
            // Dummy method for now just to show the outline.
            // This sample has no content that needs modification due to activation state.
            // We are entirely relying on XAML to do the right thing for our simple app. :)
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
    }
}
