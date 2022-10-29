using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace Picker
{
    internal class Program
    {
        static void Main(string[] args)
        {
            DotNetEnv.Env.Load("config.env");
            var hotkeyString = DotNetEnv.Env.GetString("HOTKEY");
            var c = new KeyGestureConverter();
            KeyGesture aKeyGesture = (KeyGesture)c.ConvertFrom(hotkeyString);
            HotKeyManager.RegisterHotKey((uint)aKeyGesture.Key, (uint)aKeyGesture.Modifiers);
            HotKeyManager.HotKeyPressed += new EventHandler<HotKeyEventArgs>(HotKeyManager_HotKeyPressed);
            //HotKeyManager.Run();
        }

        static void HotKeyManager_HotKeyPressed(object sender, HotKeyEventArgs e)
        {
            Debug.WriteLine("Hit me!");
        }
    }
}
