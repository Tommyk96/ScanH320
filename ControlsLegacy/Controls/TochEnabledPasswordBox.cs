using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace PharmaLegacy.Controls
{
   /* 
    public class TouchEnabledPasswordBox : PasswordBox
    {
        //inside the class definition
        private Process _touchKeyboardProcess = null;

        public TouchEnabledPasswordBox()
        {
            this.GotTouchCapture += TouchEnabledTextBox_GotTouchCapture;

            //add this at the end of TouchEnabledTextBox's constructor
            this.LostFocus += TouchEnabledTextBox_LostFocus;
        }

        private void TouchEnabledTextBox_GotTouchCapture(object sender, System.Windows.Input.TouchEventArgs e)
        {
            string touchKeyboardPath = @"C:\Program Files\Common Files\Microsoft Shared\Ink\TabTip.exe";
            _touchKeyboardProcess = Process.Start(touchKeyboardPath);
        }

        //add this method as a member method of the class
        private void TouchEnabledTextBox_LostFocus(object sender, RoutedEventArgs eventArgs)
        {
            if (_touchKeyboardProcess != null)
            {
                _touchKeyboardProcess.Kill();
                //nullify the instance pointing to the now-invalid process
                _touchKeyboardProcess = null;
            }
        }
    }/**/
}