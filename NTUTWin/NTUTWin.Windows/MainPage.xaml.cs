using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace NTUTWin
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Send GA View
            App.Current.GATracker.SendView("MainPage");

            frame.Navigate(typeof(CurriculumPage));
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            switch(e.ClickedItem.ToString())
            {
                case "查課表":
                    frame.Navigate(typeof(CurriculumPage));
                    break;
                case "行事曆":
                    frame.Navigate(typeof(SchedulePage));
                    break;
                case "期中預警":
                    frame.Navigate(typeof(MidAlertPage));
                    break;
            }
        }
    }
}
