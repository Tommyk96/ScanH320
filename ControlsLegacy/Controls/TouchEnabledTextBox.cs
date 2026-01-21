using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace ControlsLegacy.Controls
{
   
    public class TouchEnabledTextBox : TextBox
    {
        //inside the class definition
        private Process _touchKeyboardProcess = null;

        public TouchEnabledTextBox()
        {
            this.GotTouchCapture += TouchEnabledTextBox_GotTouchCapture;
            this.GotFocus += TouchEnabledTextBox_GotFocus;

            this.GotKeyboardFocus += TouchEnabledTextBox_GotKeyboardFocus;
            
            //add this at the end of TouchEnabledTextBox's constructor
            this.LostFocus += TouchEnabledTextBox_LostFocus;
        }

        private void TouchEnabledTextBox_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            //string touchKeyboardPath = @"C:\Program Files\Common Files\Microsoft Shared\Ink\TabTip.exe";
            ////C:\Program Files\Common Files\microsoft shared\ink
            ////string touchKeyboardPath = @"C:\Windows\System32\osk.exe";
            //try
            //{
            //    _touchKeyboardProcess = Process.Start(touchKeyboardPath);
            //}
            //catch (Exception ex)
            //{
            //    ex.ToString();
            //}
        }

        private void TouchEnabledTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            string touchKeyboardPath = @"C:\Program Files\Common Files\Microsoft Shared\Ink\TabTip.exe";
            //C:\Program Files\Common Files\microsoft shared\ink
            //string touchKeyboardPath = @"C:\Windows\System32\osk.exe";
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = touchKeyboardPath;
                startInfo.Verb = "runas"; // Запуск с повышенными правами
                startInfo.UseShellExecute = true;

              //  _touchKeyboardProcess = Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        private void TouchEnabledTextBox_GotTouchCapture(object sender, System.Windows.Input.TouchEventArgs e)
        {
            string touchKeyboardPath = @"C:\Program Files\Common Files\Microsoft Shared\Ink\TabTip.exe";
            try
            {
                _touchKeyboardProcess = Process.Start(touchKeyboardPath);
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        //add this method as a member method of the class
        private void TouchEnabledTextBox_LostFocus(object sender, RoutedEventArgs eventArgs)
        {
            try
            {
                if (_touchKeyboardProcess != null)
                {
                    _touchKeyboardProcess.Kill();
                    //nullify the instance pointing to the now-invalid process
                    _touchKeyboardProcess = null;
                }
            }
            catch (InvalidOperationException ex)
            {
                ex.Message.ToString();

            }
            catch (Exception ex)
            {
                ex.Message.ToString();
            }
        }
    }
}


