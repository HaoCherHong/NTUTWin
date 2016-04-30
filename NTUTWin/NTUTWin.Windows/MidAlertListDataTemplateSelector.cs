using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace NTUTWin
{
    class MidAlertListDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SubmittedTemplate { get; set; }
        public DataTemplate UnSubmittedTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            var element = container as ListViewItem;

            if (item != null && item is MidAlerts.MidAlert)
            {
                MidAlerts.MidAlert alertItem = item as MidAlerts.MidAlert;

                if (alertItem.AlertSubmitted)
                {
                    if (alertItem.Alerted)
                    {
                        element.Background = new SolidColorBrush(Color.FromArgb(255, 209, 52, 56));
                        return SubmittedTemplate;
                    }

                    else
                    {
                        element.Background = new SolidColorBrush(Color.FromArgb(255, 52, 125, 56));
                        return SubmittedTemplate;
                    }
                }
                else
                    return UnSubmittedTemplate;
            }

            return base.SelectTemplateCore(item, container);
        }
    }
}
