using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace DeveloperConsole {
    public class ConsoleSettings {
        public const Keys ScrollUpKey = Keys.PageUp;
        public const Keys ScrollDownKey = Keys.PageDown;
        public const Keys HistoryLastKey = Keys.Up;
        public const Keys HistoryNextKey = Keys.Down;
        public const Keys CursorLeftKey = Keys.Left;
        public const Keys CursorRightKey = Keys.Right;
        public const Keys DelKey = Keys.Delete;
        public const Keys BackSpaceKey = Keys.Back;

        public static string Version =
            FileVersionInfo.GetVersionInfo(typeof (DeveloperConsole).Assembly.Location).ProductVersion != "9.9.9.9"
                ? FileVersionInfo.GetVersionInfo(typeof (DeveloperConsole).Assembly.Location).ProductVersion
                : "DEV";

        public static int NumLines = 15;
        public static float FontSize = .3F;
        public static string PreString = "--> ";
        public static string CursorCharacter = "|";

        public static Keys[] HideKeys = {
            Keys.F4, Keys.Oemtilde
        };

        public static int Alpha = 175;
        public static int TextAlpha = 200;
        public static Color DefaultBgColor = Color.FromArgb(Alpha, 52, 35, 120);
        public static Color DefaultTextColor = Color.FromArgb(TextAlpha, 255, 255, 255);

        public static bool IsDevBuild {
            get { return Version == "DEV"; }
        }
    }
}