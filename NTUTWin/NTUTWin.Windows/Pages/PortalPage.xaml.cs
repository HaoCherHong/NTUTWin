using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace NTUTWin
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class PortalPage : Page
	{
		private static Uri baseUri = new Uri("http://nportal.ntut.edu.tw/myPortal.do");

		public PortalPage()
		{
			this.InitializeComponent();
			webView.NavigationStarting += WebView_NavigationStarting;
            //webView.FrameDOMContentLoaded += WebView_FrameDOMContentLoaded;
			//webView.FrameNavigationCompleted += WebView_FrameNavigationCompleted;
            webView.DOMContentLoaded += WebView_FrameDOMContentLoaded;
            webView.NavigationCompleted += WebView_FrameNavigationCompleted;

        }

		private void WebView_FrameNavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
		{
			//Hide progress indicator
			progressStackPanel.Visibility = Visibility.Collapsed;
		}

		private async void WebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
		{
			if (args.Uri.LocalPath == "/logout.do")
			{
				//Prevent from being logged out
				args.Cancel = true;

				//Send GA Event
				App.Current.GATracker.SendEvent("Portal", "Attempted to log out", null, 0);
			}
			else if (args.Uri.LocalPath == "/index.do")
			{
				//If logged out, login automatically
				await Login();

				//Send GA Event
				App.Current.GATracker.SendEvent("Portal", "Been logged out, re-login", null, 0);
			}
		}

		private async void WebView_FrameDOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args)
		{
			//Prevent new windows
			await webView.InvokeScriptAsync("eval", new[]
			{
				@"
				(function() {
					var updateHyperlinks = function() {
						var hyperlinks = document.getElementsByTagName('a');
						for (var i = 0; i < hyperlinks.length; i++) {
							if ((target = hyperlinks[i].getAttribute('target')) != null && target != '_self') {
								console.log(hyperlinks[i]);
								hyperlinks[i].setAttribute('target', '_self');
								console.log(hyperlinks[i]);
							}
						}
					}

					var observeDOM = (function() {
						var MutationObserver = window.MutationObserver || window.WebKitMutationObserver,
							eventListenerSupported = window.addEventListener;

						return function(obj, callback) {
							if (MutationObserver) {
								// define a new observer
								var obs = new MutationObserver(function(mutations, observer) {
									if (mutations[0].addedNodes.length || mutations[0].removedNodes.length)
										callback();
								});
								// have the observer observe foo for changes in children
								obs.observe(obj, {
									childList: true,
									subtree: true
								});
							} else if (eventListenerSupported) {
								obj.addEventListener('DOMNodeInserted', callback, false);
								obj.addEventListener('DOMNodeRemoved', callback, false);
							}
						}
					})();

					observeDOM(document, updateHyperlinks);
					//setInterval(updateHyperlinks, 500);

					//
					//updateHyperlinks();

					//Override window.open 
					window.open = function() {
						return function(url) {
							window.location.href = url;
						};
					}(window.open);
				})();
				"
			});
		}

		private async void Page_Loaded(object sender, RoutedEventArgs e)
		{
			//Send GA View
			App.Current.GATracker.SendView("PortalPage");

			//Start login process
			await Login();
		}

		async private Task Login()
		{
			//Set progress
			progressStackPanel.Visibility = Visibility.Visible;
			progressRing.IsActive = true;
			progressTextBlock.Text = "檢查登入狀態";

            try
            {
                var isLoggedIn = await NPAPI.IsLoggedIn();
                var roamingSettings = ApplicationData.Current.RoamingSettings;

                if (!isLoggedIn)
                {
                    //Not logged in, login now
                    progressTextBlock.Text = "登入中";
                    throw new NPAPI.SessionExpiredException();
                }

                //Set global cookie to current saved JSESSIONID
                HttpBaseProtocolFilter filter = new HttpBaseProtocolFilter();
                HttpCookie cookie = new HttpCookie("JSESSIONID", baseUri.Host, "/");
                cookie.Value = roamingSettings.Values["JSESSIONID"].ToString();
                filter.CookieManager.SetCookie(cookie, false);

                //Set progress
                progressTextBlock.Text = "載入中";

                //
                GoHome();
            }
            catch (NPAPI.SessionExpiredException)
            {
                //Send GA Event
                App.Current.GATracker.SendEvent("Session", "Session Expired", null, 0);

                //Try background login
                try
                {
                    await NPAPI.BackgroundLogin();
                    await Login();
                }
                catch
                {
                    Frame.Navigate(typeof(LoginPage));
                }
            }
            catch (Exception e)
            {
                //Show message
                progressRing.IsActive = false;
                progressTextBlock.Text = e.Message;
            }
		}

		private void GoHome()
		{
			HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, baseUri);
			webView.NavigateWithHttpRequestMessage(httpRequestMessage);
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if (sender == goBackButton)
				webView.GoBack();
			else if (sender == goForwardButton)
				webView.GoForward();
			else if(sender == homeButton)
				GoHome();
		}
	}
}
