
using System;
using UIKit;
using Foundation;
using WebKit;
using MvvmCross.iOS.Views;
using MvvmWebIOS.Core.ViewModels;

using MvvmCross.Binding.BindingContext;

namespace MvvmWebIOS.iOS.Views 
{
    [Register ("W2WebView")]
    public class W2WebView : MvxViewController 
    {
        protected UIBarButtonItem BackButton;
        protected UIBarButtonItem RefreshButton;
        protected UIBarButtonItem ForwardButton;

        public WKWebView Web { get; private set; }
        private readonly bool _navigationToolbar;
        private readonly bool _showPageAsTitle;

        ~W2WebView()
        {
            Console.WriteLine("All done with " + GetType().Name);
        }

        protected virtual void GoBack()
        {
            Web.GoBack();
        }

        protected virtual void Refresh()
        {
            Web.Reload();
        }

        protected virtual void GoForward()
        {
            Web.GoForward();
        }

        private static void InitUserAgent()
        {
            NSObject agent = NSUserDefaults.StandardUserDefaults["UserAgernt"];

            string newUserAgent = "";

            if (agent != null)
            {
                newUserAgent = agent.ToString();
            }

            if (!newUserAgent.Contains("coltrack_ios_mobile"))
            {
                newUserAgent += " coltrack_ios_mobile";

                var dictionary = new NSDictionary("UserAgent", newUserAgent);
                NSUserDefaults.StandardUserDefaults.RegisterDefaults(dictionary);

            }			
        }

        public W2WebView()
            : this(true, true)
        {
            InitUserAgent();
        }
        
        public W2WebView(bool navigationToolbar, bool showPageAsTitle = false)
        {
            NavigationItem.BackBarButtonItem = new UIBarButtonItem() {Title = ""};

            _navigationToolbar = navigationToolbar;
            _showPageAsTitle = showPageAsTitle;

            if (_navigationToolbar)
            {
                BackButton =
                    new UIBarButtonItem(Images.Web.Back, UIBarButtonItemStyle.Plain, (s, e) => GoBack())
                    {
                        Enabled = false
                    };
                ForwardButton =
                    new UIBarButtonItem(Images.Web.Forward, UIBarButtonItemStyle.Plain, (s, e) => GoForward())
                    {
                        Enabled = false
                    };
                RefreshButton =
                    new UIBarButtonItem(UIBarButtonSystemItem.Refresh, (s, e) => Refresh()) {Enabled = false};

                //BackButton.TintColor = Theme.CurrentTheme.WebButtonTint;
                //ForwardButton.TintColor = Theme.CurrentTheme.WebButtonTint;
                //RefreshButton.TintColor = Theme.CurrentTheme.WebButtonTint;
            }

            EdgesForExtendedLayout = UIRectEdge.None;

            InitUserAgent();
        }

        private class NavigationDelegate : WKNavigationDelegate
        {
            private readonly WeakReference<W2WebView> _webView;

            public NavigationDelegate(W2WebView webView)
            {
                System.Console.WriteLine( "=========================== NavigationDelegate ctr" );

                _webView = new WeakReference<W2WebView>(webView);
            }

            public override void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
            {
                System.Console.WriteLine( "=========================== DidFinishNavigation" );

                _webView.Get()?.OnLoadFinished(webView, navigation);
            }

            public override void DidStartProvisionalNavigation(WKWebView webView, WKNavigation navigation)
            {
                System.Console.WriteLine( "=========================== DidStartProvisionalNavigation" );
                
                _webView.Get()?.OnLoadStarted(null, EventArgs.Empty);
            }

            public override void DidFailNavigation(WKWebView webView, WKNavigation navigation, NSError error)
            {
                System.Console.WriteLine( "=========================== DidFailNavigation" );

                _webView.Get()?.OnLoadError(error);
            }

            public override void DecidePolicy(WKWebView webView, WKNavigationAction navigationAction,
                Action<WKNavigationActionPolicy> decisionHandler)
            {
                System.Console.WriteLine( "=========================== DecidePolicy" );

                var ret = _webView.Get()?.ShouldStartLoad(webView, navigationAction) ?? true;
                decisionHandler(ret ? WKNavigationActionPolicy.Allow : WKNavigationActionPolicy.Cancel);
            }
        }

        protected virtual bool ShouldStartLoad(WKWebView webView, WKNavigationAction navigationAction)
        {
            return true;
        }

        protected virtual void OnLoadError(NSError error)
        {
            System.Console.WriteLine( "=========================== OnLoadError" );

            //NetworkActivity.PopNetworkActive();

            if (BackButton != null)
            {
                BackButton.Enabled = Web.CanGoBack;
                ForwardButton.Enabled = Web.CanGoForward;
                RefreshButton.Enabled = true;
            }
        }

        protected virtual void OnLoadStarted(object sender, EventArgs e)
        {
            System.Console.WriteLine( "=========================== OnLoadStarted" );

            //NetworkActivity.PushNetworkActive();

            if (RefreshButton != null)
                RefreshButton.Enabled = false;
        }

        protected virtual void OnLoadFinished(WKWebView webView, WKNavigation navigation)
        {
            System.Console.WriteLine( "=========================== OnLoadFinished" );

            //NetworkActivity.PopNetworkActive();

            if (BackButton != null)
            {
                BackButton.Enabled = Web.CanGoBack;
                ForwardButton.Enabled = Web.CanGoForward;
                RefreshButton.Enabled = true;
            }

            if (_showPageAsTitle)
            {
                Web.EvaluateJavaScript("document.title", (o, _) => { Title = o as NSString; });
            }
        }

        public override void ViewWillDisappear(bool animated)
        {
            System.Console.WriteLine( "=========================== ViewWillDisappear" );
            
            base.ViewWillDisappear(animated);
            if (ToolbarItems != null)
                NavigationController.SetToolbarHidden(true, animated);
        }

        public override void ViewDidLoad()
        {
            System.Console.WriteLine( "=========================== ViewDidLoad" );
            
            base.ViewDidLoad();

            var set = this.CreateBindingSet<W2WebView, W2WebViewModel>();
            /*
            set.Bind(Label).To(vm => vm.Hello);
            set.Bind(TextField).To(vm => vm.Hello);
            */
            set.Apply();

            Web = new WKWebView(View.Bounds, new WKWebViewConfiguration());
            Web.NavigationDelegate = new NavigationDelegate(this);
            Add(Web);

            /*
            var loadableViewModel = ViewModel as LoadableViewModel;
            if (loadableViewModel != null)
            {
                loadableViewModel.Bind(x => x.IsLoading).Subscribe(x =>
                {
                    if (x) NetworkActivity.PushNetworkActive();
                    else NetworkActivity.PopNetworkActive();
                });
            }*/
            
            var url = "https://x.com/index.jsp"; // NOTE: https required for iOS 9 ATS
            Web.LoadRequest (new NSUrlRequest (new NSUrl (url)));

        }

        public override void ViewWillLayoutSubviews()
        {
            base.ViewWillLayoutSubviews();
            Web.Frame = View.Bounds;
        }

        /*
        protected static string JavaScriptStringEncode(string data)
        {
            return System.Web.HttpUtility.JavaScriptStringEncode(data);
        }

        protected static string UrlDecode(string data)
        {
            return System.Web.HttpUtility.UrlDecode(data);
        }*/

        protected string LoadFile(string path)
        {
            if (path == null)
                return string.Empty;

            var uri = Uri.EscapeUriString("file://" + path) + "#" + Environment.TickCount;
            InvokeOnMainThread(() => Web.LoadRequest(new Foundation.NSUrlRequest(new Foundation.NSUrl(uri))));
            return uri;
        }

        protected void LoadContent(string content)
        {
            Web.LoadHtmlString(content, NSBundle.MainBundle.BundleUrl);
        }

        public override void ViewWillAppear(bool animated)
        {
            System.Console.WriteLine( "=========================== ViewWillAppear" );

            base.ViewWillAppear(animated);

            var bounds = View.Bounds;
            if (_navigationToolbar)
                bounds.Height -= NavigationController.Toolbar.Frame.Height;
            Web.Frame = bounds;

            if (_navigationToolbar)
            {
                ToolbarItems = new[]
                {
                    BackButton,
                    new UIBarButtonItem(UIBarButtonSystemItem.FixedSpace) {Width = 40f},
                    ForwardButton,
                    new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
                    RefreshButton
                };

                BackButton.Enabled = Web.CanGoBack;
                ForwardButton.Enabled = Web.CanGoForward;
                RefreshButton.Enabled = !Web.IsLoading;
            }

            if (_showPageAsTitle)
            {
                Web.EvaluateJavaScript("document.title", (o, _) => { Title = o as NSString; });
            }

            if (ToolbarItems != null)
                NavigationController.SetToolbarHidden(false, animated);
        }

        public override void ViewDidDisappear(bool animated)
        {
            System.Console.WriteLine( "=========================== ViewDidDisappear" );

            base.ViewDidDisappear(animated);

            if (_navigationToolbar)
                ToolbarItems = null;
        }

        public override void DidRotate(UIInterfaceOrientation fromInterfaceOrientation)
        {
            System.Console.WriteLine( "=========================== DidRotate" );

            base.DidRotate(fromInterfaceOrientation);
            Web.Frame = View.Bounds;
        }














    }
}