using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
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
			webView.FrameDOMContentLoaded += WebView_FrameDOMContentLoaded;
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
			await Login();
		}

		async private Task Login()
		{
			var result = await NPAPI.IsLoggedIn();
			if (result.Success)
			{
				var roamingSettings = ApplicationData.Current.RoamingSettings;
				bool isLoggedIn = result.Data;

				if (!isLoggedIn)
					await NPAPI.BackgroundLogin();

				
				HttpBaseProtocolFilter filter = new HttpBaseProtocolFilter();
				HttpCookie cookie = new HttpCookie("JSESSIONID", baseUri.Host, "/");
				cookie.Value = roamingSettings.Values["JSESSIONID"].ToString();
				filter.CookieManager.SetCookie(cookie, false);

				GoHome();
			}
			else
			{
				if (result.Error == NPAPI.RequestResult.ErrorType.Unauthorized)
				{
					//Send GA Event
					App.Current.GATracker.SendEvent("Session", "Session Expired", null, 0);

					//Try background login
					var loginResult = await NPAPI.BackgroundLogin();
					if (loginResult.Success)
						await Login();
					else
						Frame.Navigate(typeof(LoginPage));
				}
				else
				{
					//Show message
				}
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
