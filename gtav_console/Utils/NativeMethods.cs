using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace DeveloperConsole {
    internal static class NativeMethods {
        public delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>
        ///     Gets a string from the data from a key press
        /// </summary>
        /// <param name="keys">The pressed Keys</param>
        /// <param name="shift">Whether or not shift was held</param>
        /// <param name="altGr">Whether or not alt was held</param>
        /// <returns>The string produced from the keys</returns>
        internal static string GetCharsFromKeys(Keys keys, bool shift, bool altGr) {
            var buf = new StringBuilder(256);
            var keyboardState = new byte[256];

            if (shift) keyboardState[(int) Keys.ShiftKey] = 0xff;
            if (altGr) {
                keyboardState[(int) Keys.ControlKey] = 0xff;
                keyboardState[(int) Keys.Menu] = 0xff;
            }

            if (ToUnicode((uint) keys, 0, keyboardState, buf, 256, 0) == 1) return buf.ToString();

            return null;
        }

        /// <summary>
        ///     Check if GTA is running and in the foreground
        /// </summary>
        /// <returns>Whether or not GTA is in the foreground</returns>
        internal static bool ApplicationIsActivated() {
            var activatedHandle = GetForegroundWindow();
            if (activatedHandle == IntPtr.Zero) {
                return false;
            }

            var procId = Process.GetCurrentProcess().Id;
            int activeProcId;
            GetWindowThreadProcessId(activatedHandle, out activeProcId);

            return activeProcId == procId;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn,
            IntPtr hInstance, int threadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        internal static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        internal static extern int ToUnicode(uint virtualKeyCode, uint scanCode, byte[] keyboardState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 64)] StringBuilder receivingBuffer, int bufferSize,
            uint flags);

        [StructLayout(LayoutKind.Sequential)]
        public class Point {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class MouseHookStruct {
            public Point pt;
            public int hwnd;
            public int wHitTestCode;
            public int dwExtraInfo;
        }
    }
}