using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Diagnostics;

namespace Ticker
{
    public partial class Form1 : Form
    {
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // turn on WS_EX_TOOLWINDOW style bit
                cp.ExStyle |= 0x80;
                return cp;
            }
        }

        public Form1()
        {
            InitializeComponent();
            if (!CheckValid())
                Process.GetCurrentProcess().Kill();
            DotNetEnv.Env.Load("config.env");
            {
                var hotkeyString = DotNetEnv.Env.GetString("HOTKEY_PICK");
                var c = new KeyGestureConverter();
                KeyGesture aKeyGesture = (KeyGesture)c.ConvertFrom(hotkeyString);
                var aVirtualKeyCode = (Keys)KeyInterop.VirtualKeyFromKey(aKeyGesture.Key);
                if (!RegisterHotKey(this.Handle, 1, (uint)aKeyGesture.Modifiers | MOD_NOREPEAT, (uint)aVirtualKeyCode))
                    MessageBox.Show($"Set hotkey failed: {hotkeyString}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            {
                var hotkeyString = DotNetEnv.Env.GetString("HOTKEY_TOGGLE");
                var c = new KeyGestureConverter();
                KeyGesture aKeyGesture = (KeyGesture)c.ConvertFrom(hotkeyString);
                var aVirtualKeyCode = (Keys)KeyInterop.VirtualKeyFromKey(aKeyGesture.Key);
                if (!RegisterHotKey(this.Handle, 2, (uint)aKeyGesture.Modifiers | MOD_NOREPEAT, (uint)aVirtualKeyCode))
                    MessageBox.Show($"Set hotkey failed: {hotkeyString}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [DllImport("User32")]
        public static extern bool RegisterHotKey(
            IntPtr hWnd,
            int id,
            uint fsModifiers,
            uint vk
        );
        [DllImport("User32")]
        public static extern bool UnregisterHotKey(
            IntPtr hWnd,
            int id
        );

        public const int MOD_WIN = 0x8;
        public const int MOD_SHIFT = 0x4;
        public const int MOD_CONTROL = 0x2;
        public const int MOD_ALT = 0x1;
        public const int WM_HOTKEY = 0x312;
        public const int WM_DESTROY = 0x0002;
        private static uint MOD_NOREPEAT = 0x4000;

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_HOTKEY:
                    switch (m.WParam.ToInt32())
                    {
                        case 1:
                            Pick();
                            break;
                        case 2:
                            Toggle();
                            break;
                    }
                    break;
                case WM_DESTROY:
                    UnregisterHotKey(this.Handle, 1);
                    UnregisterHotKey(this.Handle, 2);
                    break;
            }
            base.WndProc(ref m);
        }

        private bool formVisible = false;
        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(formVisible ? value : formVisible);
        }

        public enum GWL
        {
            ExStyle = -20
        }

        public enum WS_EX
        {
            Transparent = 0x20,
            Layered = 0x80000
        }

        public enum LWA
        {
            ColorKey = 0x1,
            Alpha = 0x2
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        public static extern int GetWindowLong(IntPtr hWnd, GWL nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        public static extern int SetWindowLong(IntPtr hWnd, GWL nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetLayeredWindowAttributes")]
        public static extern bool SetLayeredWindowAttributes(IntPtr hWnd, int crKey, byte alpha, LWA dwFlags);


        private void Form1_Load(object sender, EventArgs e)
        {
            if (DotNetEnv.Env.GetInt("CLICK_THROUGH") > 0)
            {
                int wl = GetWindowLong(this.Handle, GWL.ExStyle);
                wl = wl | (int)WS_EX.Layered | (int)WS_EX.Transparent;
                SetWindowLong(this.Handle, GWL.ExStyle, wl);
                SetLayeredWindowAttributes(this.Handle, 0, 255, LWA.Alpha);
            }
        }

        void Pick()
        {
            if (formVisible) return;
            try
            {
                DotNetEnv.Env.Load("config.env");
                var sizeString = DotNetEnv.Env.GetString("SIZE");
                var array = sizeString.Split(',');
                this.Left = int.Parse(array[0].Trim());
                this.Top = int.Parse(array[1].Trim());
                this.Width = int.Parse(array[2].Trim());
                this.Height = int.Parse(array[3].Trim());
                Bitmap bitmap = new Bitmap(this.Width, this.Height);
                Graphics graphics = Graphics.FromImage(bitmap);
                graphics.CopyFromScreen(this.Left, this.Top, 0, 0, new Size(this.Width, this.Height));
                graphics.Flush();
                this.BackgroundImage = bitmap;
                //bitmap.Dispose();
                //graphics.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "SIZE ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void Toggle()
        {
            formVisible = !formVisible;
            this.Visible = formVisible;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterHotKey(this.Handle, 1);
            UnregisterHotKey(this.Handle, 2);
        }

        public static string GetCode()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return null;
        }

        public static bool CheckValid()
        {
            //127    -560733196
            //128    -133766156
            //129     1822548980
            //130    -170502001
            //131     1785813135
            //132    -552839025
            //133     1403476111
            var code = GetCode();
            if (code == null) return false;
            int codeValue = code.GetHashCode();
            var array = new int[] { -560733196, -133766156, 1822548980, -170502001, 1785813135, -552839025, 1403476111 };
            if (!array.Contains(codeValue)) return false;
            var now = DateTime.UtcNow;
            if (now.Year != 2023 || now.Month > 2) return false;
            return true;
        }
    }
}
