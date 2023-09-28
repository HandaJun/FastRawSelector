using FastRawSelector.MODEL;
using MaterialDesignThemes.Wpf;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace FastRawSelector.LOGIC
{
    public class Alert
    {
        public static async Task<object> Info(
            object messageBoxText,
            string caption = "알림",
            Action afterAct = null
            )
        {
            var rtn = await Show(messageBoxText, caption, MessageBoxButton.OK, AlertStateEnum.INFO);
            afterAct?.Invoke();
            return rtn;
        }

        //public static MessageBoxResult Confirm(
        //    object messageBoxText,
        //    string caption = "확인"
        //    )
        //{
        //    return Show(messageBoxText, caption, MessageBoxButton.YesNo, AlertStateEnum.OK).Result;
        //}

        public static Task<object> Show(
            object messageBoxText,
            string caption = "알림",
            MessageBoxButton button = MessageBoxButton.OK,
            AlertStateEnum state = AlertStateEnum.INFO
            )
        {
            NotificationMessage msg = new NotificationMessage();
            if (button == MessageBoxButton.OK)
            {
                msg = new InfoNotificationMessage()
                {
                    Title = caption,
                    Message = messageBoxText.ToString()
                };
            }
            else if (button == MessageBoxButton.YesNo)
            {
                msg = new ConfirmNotificationMessage()
                {
                    Title = caption,
                    Message = messageBoxText.ToString()
                };
            }

            return DialogHost.Show(msg, "RootDialog");
        }




    }
}
