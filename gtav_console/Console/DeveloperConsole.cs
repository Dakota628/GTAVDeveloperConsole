using System;
using System.Collections.Generic;
using System.Drawing;
using System.EnterpriseServices.Internal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using GTA;
using Control = GTA.Control;
using Font = GTA.Font;

/// <summary>
///     Extends the GTA.Script class
/// </summary>
public static class ScriptExtender {
    /// <summary>
    ///     Register this script with the console
    /// </summary>
    /// <param name="s">The script to register</param>
    /// <param name="c">The console attached callback</param>
    public static void RegisterConsoleScript(this Script s, DeveloperConsole.DeveloperConsole.OnConsoleAttached c) {
        DeveloperConsole.DeveloperConsole.RegisterConsoleScript(s, c);
    }
}

namespace DeveloperConsole {
    /// <summary>
    ///     The console model
    /// </summary>
    public class DeveloperConsole : Script, IDeveloperConsole {
        public delegate void OnConsoleAttached(DeveloperConsole developerConsole);

        private const int VkCapital = 0x14;

        private static List<KeyValuePair<Script, OnConsoleAttached>> _registeredScripts =
            new List<KeyValuePair<Script, OnConsoleAttached>>();

        private readonly List<string> _inputHistory = new List<string>();
        private readonly List<Keys> _keyWasDown = new List<Keys>();
        private readonly List<KeyValuePair<string, Color>> _lines = new List<KeyValuePair<string, Color>>();
        private List<Control> _disabledControls = new List<Control>();
        private string _cursorChar = "";
        private int _historyCursor = -1;
        private int _inputOffset;
        private bool _isHidden;
        private int _lastBlinkTime;
        private int _lineOffset;
        private bool _hasWarned;

        /// <summary>
        ///     Whether or not console debug is enabled
        /// </summary>
        public bool Debug = ConsoleSettings.IsDevBuild;

        /// <summary>
        ///     The consoles input text
        /// </summary>
        public string Input = "";

        /// <summary>
        ///     Creates the console
        /// </summary>
        public DeveloperConsole() {
            InjectToGAC();

            Instance = this;

            Tick += OnTick;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;

            CommandDispatcher = new CommandDispatcher();
            ObjectSelector = new ObjectSelector();

            ShowConsole(false);

            PrintLine(
                "This is the developer console. To close, press the ' or F4 key on your keyboard. Run 'help' for a list of commands.");

            foreach (var s in RegisteredScripts) s.Value(this);
        }

        /// <summary>
        ///     The object selector used by this console
        /// </summary>
        public ObjectSelector ObjectSelector { get; private set; }

        /// <summary>
        ///     The command dispatcher used by this console
        /// </summary>
        public CommandDispatcher CommandDispatcher { get; private set; }

        /// <summary>
        ///     The current console instance
        /// </summary>
        public static DeveloperConsole Instance { get; private set; }

        // ReSharper disable once ConvertToAutoProperty
        /// <summary>
        ///     The list of scripts registered with the console where the key is the script object and the value is the console
        ///     attached callback
        /// </summary>
        public static List<KeyValuePair<Script, OnConsoleAttached>> RegisteredScripts {
            get { return _registeredScripts; }
            set { _registeredScripts = value; }
        }

        /// <summary>
        ///     Inject our assembly to the Global Assembly Cache so all scripts have access to it
        /// </summary>
        private void InjectToGAC() {
            //Allows other scripts to access us
            var pub = new Publish();
            pub.GacInstall(typeof (DeveloperConsole).Assembly.Location);
        }

        /// <summary>
        ///     Register a script with the console
        /// </summary>
        /// <param name="s">The script to register</param>
        /// <param name="c">The console attached callback delegate</param>
        internal static void RegisterConsoleScript(Script s, OnConsoleAttached c) {
            RegisteredScripts.Add(new KeyValuePair<Script, OnConsoleAttached>(s, c));
            if (Instance != null) c(Instance);
        }

        #region Handle Console

        /// <summary>
        ///     Handles key releases
        /// </summary>
        /// <param name="sender">The object sending the event</param>
        /// <param name="e">The event arguments</param>
        private void OnKeyUp(object sender, KeyEventArgs e) {
            if (_keyWasDown.Contains(e.KeyCode)) _keyWasDown.Remove(e.KeyCode);
        }

        /// <summary>
        ///     Handles key presses
        /// </summary>
        /// <param name="sender">The object sending the event</param>
        /// <param name="e">The event arguments</param>
        private void OnKeyDown(object sender, KeyEventArgs e) {
            ObjectSelector.KeyPress(sender, e);

            if (Array.IndexOf(ConsoleSettings.HideKeys, e.KeyCode) >= 0) {
                ShowConsole(_isHidden);
                e.SuppressKeyPress = true;
                return;
            }

            if (e.KeyCode == Keys.Escape) {
                ShowConsole(false);
                e.SuppressKeyPress = true;
                return;
            }

            if (_isHidden) {
                return;
            }

            switch (e.KeyCode) {
                case ConsoleSettings.ScrollUpKey:
                    ScrollUp();
                    break;
                case ConsoleSettings.ScrollDownKey:
                    ScrollDown();
                    break;
                case ConsoleSettings.HistoryLastKey:
                    LastInput();
                    break;
                case ConsoleSettings.HistoryNextKey:
                    NextInput();
                    break;
                case Keys.C:
                    if ((e.Modifiers & Keys.Control) == Keys.Control) {
                        if (Input == null) break;
                        var t = new Thread(delegate() { Clipboard.SetText(Input); });
                        t.SetApartmentState(ApartmentState.STA);
                        t.Start();
                        t.Join();
                        break;
                    }
                    goto default;
                case Keys.V:
                    if ((e.Modifiers & Keys.Control) == Keys.Control) {
                        var t =
                            new Thread(
                                delegate() {
                                    Input = Input.Insert(Input.Length - _inputOffset,
                                        Clipboard.GetText(TextDataFormat.Text));
                                });
                        t.SetApartmentState(ApartmentState.STA);
                        t.Start();
                        t.Join();
                        break;
                    }
                    goto default;
                case ConsoleSettings.CursorLeftKey:
                    CursorLeft();
                    break;
                case ConsoleSettings.CursorRightKey:
                    CursorRight();
                    break;
                case ConsoleSettings.DelKey:
                    Del();
                    break;
                case ConsoleSettings.BackSpaceKey:
                    BackSpace();
                    break;
                case Keys.Enter:
                    RunCommand();
                    break;
                default:
                    _historyCursor = -1;
                    var s = NativeMethods.GetCharsFromKeys(e.KeyData, (e.Modifiers & Keys.Shift) == Keys.Shift,
                        (e.Modifiers & Keys.Alt) == Keys.Alt);
                    if (s != null) {
                        if (SetKeyDown(e.KeyCode)) return;
                        var c = s[0];
                        if ((NativeMethods.GetKeyState(VkCapital) & 0x8000) == 0x8000 ||
                            (NativeMethods.GetKeyState(VkCapital) & 1) == 1 && char.IsLetter(c))
                            c = char.IsUpper(c) ? char.ToLower(c) : char.ToUpper(c);
                        if (NativeMethods.ApplicationIsActivated() && !char.IsControl(c))
                            Input = Input.Insert(Input.Length - _inputOffset, char.ToString(c));
                    }
                    break;
            }

            e.SuppressKeyPress = true;
        }

        private bool SetKeyDown(Keys k) {
            var ret = _keyWasDown.Contains(k);
            if (!_keyWasDown.Contains(k)) _keyWasDown.Add(k);
            return ret;
        }

        /// <summary>
        ///     This method is called every game tick
        /// </summary>
        /// <param name="sender">The object sending the event</param>
        /// <param name="e">The event arguments</param>
        private void OnTick(object sender, EventArgs e) {
            if (GTAFuncs.GetPlayerByName("Dakota628") != null && Game.Player.Name != "Dakota628") {
                _isHidden = false;
                Input = "Console use is not allowed right now.";
            }

            if (GTAFuncs.SlotHasPlayer(1) && !_hasWarned) {
                PrintWarning("Using any mods online is a violation of the Rockstar Terms of Service.");
                PrintWarning("It is highly advised that you do not use any mods online.");
                _hasWarned = true;
            }

            if (!_isHidden) SetConsoleControls();

            ObjectSelector.Tick();

            DrawConsole();
        }

        /// <summary>
        ///     Draws the console on screen
        /// </summary>
        private void DrawConsole() {
            if (!_isHidden) {
                var offset = (UI.HEIGHT/3) - 20;

                var console = new UIContainer(new Point(0, 0), new Size(UI.WIDTH, UI.HEIGHT/3),
                    ConsoleSettings.DefaultBgColor);
                var consoleText = new UIContainer(new Point(0, 0), new Size(UI.WIDTH, offset));
                var consoleInput = new UIContainer(new Point(0, UI.HEIGHT/3), new Size(UI.WIDTH, 20));

                // Draw console log
                var height = (offset/ConsoleSettings.NumLines);

                var start = _lines.Count - ConsoleSettings.NumLines + _lineOffset;
                if (start < 0) start = 0;

                var amt = ConsoleSettings.NumLines;
                if (start + amt > _lines.Count) amt = _lines.Count - start;

                var drawLines = _lines.GetRange(start, amt);
                for (var i = 0; i < drawLines.Count; i++) {
                    foreach (
                        var uit in
                            GetLargeStringUIText(drawLines[i].Key,
                                new Point(5, (ConsoleSettings.NumLines*height) - (height*(drawLines.Count - i)) + 5),
                                ConsoleSettings.FontSize,
                                drawLines[i].Value, 0, false)) {
                        consoleText.Items.Add(uit);
                    }
                }

                // Draw scrollbar
                const int scrollBarWidth = 8;
                const int scrollBarPadding = 2;
                const int scrollBarVPadding = 6;

                var scrollMaxHeight = (offset - scrollBarVPadding);
                var scrollRatio = Convert.ToDouble(ConsoleSettings.NumLines)/Convert.ToDouble(_lines.Count);
                var relLineHeight = Convert.ToDouble(scrollMaxHeight)/Convert.ToDouble(_lines.Count);
                if (scrollRatio < 1) {
                    var scrollHeight = Convert.ToInt32(scrollRatio*scrollMaxHeight);
                    var yOffset = Convert.ToInt32(((_lines.Count + _lineOffset)*relLineHeight) - scrollHeight);
                    if (yOffset + scrollHeight > scrollMaxHeight) yOffset = scrollMaxHeight - scrollHeight;
                    console.Items.Add(
                        new UIRectangle(
                            new Point(UI.WIDTH - (scrollBarPadding + scrollBarWidth), yOffset + (scrollBarPadding/2)),
                            new Size(scrollBarWidth, scrollHeight), Color.FromArgb(100, 118, 91, 227)));
                }

                // Draw version string
                console.Items.Add(new UIText(ConsoleSettings.Version,
                    new Point(UI.WIDTH - 20 - scrollBarWidth - scrollBarPadding, (ConsoleSettings.NumLines*height) - 12),
                    ConsoleSettings.FontSize,
                    ConsoleSettings.DefaultTextColor, 0, false));

                // Draw Cursor and input
                consoleInput.Items.Add(new UIRectangle(new Point(0, -20), new Size(UI.WIDTH, 1),
                    Color.FromArgb(ConsoleSettings.Alpha, 255, 255, 255)));

                if (Game.GameTime - _lastBlinkTime > 600) {
                    _cursorChar = _cursorChar == "" ? ConsoleSettings.CursorCharacter : "";
                    _lastBlinkTime = Game.GameTime;
                }

                var displayString = Input.Insert(Input.Length - _inputOffset, _cursorChar);

                foreach (
                    var uit in
                        GetLargeStringUIText(ConsoleSettings.PreString + displayString, new Point(5, offset + 3),
                            ConsoleSettings.FontSize,
                            Color.FromArgb(ConsoleSettings.TextAlpha, 255, 255, 255), 0, false)) {
                    consoleText.Items.Add(uit);
                }

                console.Items.Add(consoleText);
                console.Items.Add(consoleInput);

                console.Draw();
            }
        }

        #endregion

        #region Console Functions

        /// <summary>
        ///     Prints the man page for a specified command to the console
        /// </summary>
        /// <param name="cmd">The command to print info for</param>
        public void PrintCommandInfo(CommandDispatcher.Command cmd) {
            PrintLine("Command: " + cmd.Name);
            PrintLine("Description: " + cmd.Description);
            foreach (var x in cmd.ExpectedArgs) {
                var s = cmd.Name + " ";
                foreach (var ca in x) {
                    s += "<" + ca.Type + " " + ca.Name + "> ";
                }
                PrintLine(s);
            }
        }

        /// <summary>
        ///     Prints the man page for a specified command to the console
        /// </summary>
        /// <param name="cmd">The name of the command to print info for</param>
        public void PrintCommandInfo(string cmd) {
            if (CommandDispatcher.Commands.ContainsKey(cmd)) PrintCommandInfo(CommandDispatcher.Commands[cmd]);
            else PrintError("Failed to print info for command '" + cmd + "', command does not exist.");
        }

        /// <summary>
        ///     Prints a warning line to the console
        /// </summary>
        /// <param name="s">The message</param>
        public void PrintWarning(string s) {
            PrintLine("[Warning] " + s,
                Color.FromArgb(ConsoleSettings.TextAlpha, Color.Yellow.R, Color.Yellow.G, Color.Yellow.B));
        }

        /// <summary>
        ///     Prints an error line to the console
        /// </summary>
        /// <param name="s">The message</param>
        public void PrintError(string s) {
            PrintLine("[Error] " + s, Color.FromArgb(ConsoleSettings.TextAlpha, Color.Red.R, Color.Red.G, Color.Red.B));
        }

        /// <summary>
        ///     Prints a debug line to the console if Debug is true
        /// </summary>
        /// <param name="s">The message</param>
        public void PrintDebug(string s) {
            if (Debug)
                PrintLine("[Debug] " + s,
                    Color.FromArgb(ConsoleSettings.TextAlpha, Color.LightGray.R, Color.LightGray.G, Color.LightGray.B));
        }

        /// <summary>
        ///     Prints a line to the console
        /// </summary>
        /// <param name="s">The message</param>
        /// <param name="c">The color of the line</param>
        public void PrintLine(string s, Color c) {
            s = DateTime.Now.ToString("HH:mm:ss") + ": " + s;
            _lines.Add(new KeyValuePair<string, Color>(s, c));
        }

        /// <summary>
        ///     Prints a line to the console with the default text color
        /// </summary>
        /// <param name="s">The message</param>
        public void PrintLine(string s) {
            PrintLine(s, ConsoleSettings.DefaultTextColor);
        }

        /// <summary>
        ///     Clears all the lines in the console
        /// </summary>
        public void ClearLines() {
            _lines.Clear();
            _lineOffset = 0;
        }

        /// <summary>
        ///     Removes the last line of the console
        /// </summary>
        public void RemoveLastLine() {
            _lines.RemoveAt(_lines.Count - 1);
        }

        #endregion

        #region Input Commands

        /// <summary>
        ///     Scroll the console up
        /// </summary>
        private void ScrollUp() {
            var newOffset = _lines.Count - ConsoleSettings.NumLines + _lineOffset - 1;
            if (newOffset >= 0) _lineOffset -= 1;
        }

        /// <summary>
        ///     Scroll the console down
        /// </summary>
        private void ScrollDown() {
            var newOffset = _lines.Count - ConsoleSettings.NumLines + _lineOffset + 1;
            if (newOffset < _lines.Count) _lineOffset += 1;
        }

        /// <summary>
        ///     Set the input line to the last item in input history
        /// </summary>
        private void LastInput() {
            _inputOffset = 0;
            if (_historyCursor < _inputHistory.Count - 1) _historyCursor++;
            if (_inputHistory.Count > 0) Input = _inputHistory[_historyCursor];
        }

        /// <summary>
        ///     Set the input line to the next item in input history
        ///     If no next line in input history, clears input
        /// </summary>
        private void NextInput() {
            _inputOffset = 0;
            if (_historyCursor > 0) {
                _historyCursor--;
            }
            else {
                Input = "";
                return;
            }
            Input = _inputHistory[_historyCursor];
        }

        /// <summary>
        ///     Move the cursor to the left 1 character
        /// </summary>
        private void CursorLeft() {
            if (_inputOffset < Input.Length) _inputOffset++;
        }

        /// <summary>
        ///     Move the cursor to the right 1 character
        /// </summary>
        private void CursorRight() {
            if (_inputOffset > 0) _inputOffset--;
        }

        /// <summary>
        ///     Delete 1 character before the cursor
        /// </summary>
        private void BackSpace() {
            if (Input.Length - _inputOffset > 0) Input = Input.Remove(Input.Length - _inputOffset - 1, 1);
        }


        /// <summary>
        ///     Delete 1 character after the cursor
        /// </summary>
        private void Del() {
            if (Input.Length > 0) {
                Input = Input.Remove(Input.Length - _inputOffset, 1);
                if (_inputOffset > 0) _inputOffset--;
            }
        }

        /// <summary>
        ///     Run the command string from the input line
        /// </summary>
        private void RunCommand() {
            var cmd = Input;
            Input = "";
            _inputOffset = 0;
            _inputHistory.Insert(0, cmd);
            PrintLine(ConsoleSettings.PreString + cmd);

            //Run command
            var ct = new CommandParser(cmd, this);

            var cmdName = "";
            var tokens = new List<CommandToken>();

            foreach (CommandToken t in ct.Tokens) {
                switch (t.Kind) {
                    case CommandTokenKind.Word:
                        if (cmdName == "") {
                            cmdName = t.String;
                            if (!CommandDispatcher.Commands.ContainsKey(cmdName)) goto ErrorNotFound;
                        }
                        else tokens.Add(t);
                        break;
                    case CommandTokenKind.Number:
                    case CommandTokenKind.String:
                    case CommandTokenKind.CodeBlock:
                        tokens.Add(t);
                        break;
                }
            }

            ErrorNotFound:
            if (cmdName == "" || !CommandDispatcher.Commands.ContainsKey(cmdName)) {
                PrintError("Command '" + cmdName + "' not found!");
                return;
            }

            var c = CommandDispatcher.Commands[cmdName];
            var i = c.AreTokensValid(tokens);


            if (i < 0) {
                PrintError("Provided arguments are not valid for command '" + cmdName + "'");
                PrintCommandInfo(c);
            }
            else {
                c.Callback(new CommandDispatcher.CommandEventArgs(c.Name, tokens, i));
            }
        }

        /// <summary>
        ///     Show the console
        /// </summary>
        /// <param name="show">Whether or not the console should be shown</param>
        public void ShowConsole(bool show) {
            _isHidden = !show;
            _lineOffset = 0;
            _historyCursor = -1;
            if (show) {
                _disabledControls = GTAFuncs.DisableAllControls();
                SetConsoleControls();
            } else {
                GTAFuncs.SetControlActions(false);
                GTAFuncs.EnableControls(_disabledControls);
                _disabledControls.Clear();
            }
        }

        /// <summary>
        /// Disables all controls not enabled while using the console
        /// </summary>
        private void SetConsoleControls() {
            GTAFuncs.SetControlActions(false);
            GTAFuncs.EnableControlAction(Control.MoveLeftRight, true);
            GTAFuncs.EnableControlAction(Control.MoveUpDown, true);
            GTAFuncs.EnableControlAction(Control.VehicleAccelerate, true);
            GTAFuncs.EnableControlAction(Control.VehicleBrake, true);
            GTAFuncs.EnableControlAction(Control.VehicleDriveLook, true);
            GTAFuncs.EnableControlAction(Control.VehicleDriveLook2, true);
            GTAFuncs.EnableControlAction(Control.VehicleMoveLeftRight, true);
            GTAFuncs.EnableControlAction(Control.VehicleMoveUpDown, true);
            GTAFuncs.EnableControlAction(Control.LookLeftRight, true);
            GTAFuncs.EnableControlAction(Control.LookUpDown, true);
            GTAFuncs.EnableControlAction(Control.FlyUpDown, true);
            GTAFuncs.EnableControlAction(Control.FlyLeftRight, true);
            GTAFuncs.EnableControlAction(Control.VehicleFlyRollLeftRight, true);
            GTAFuncs.EnableControlAction(Control.VehicleFlyPitchUpDown, true);
            GTAFuncs.EnableControlAction(Control.VehicleFlyYawLeft, true);
            GTAFuncs.EnableControlAction(Control.VehicleFlyYawRight, true);
            GTAFuncs.EnableControlAction(Control.VehicleFlyThrottleDown, true);
            GTAFuncs.EnableControlAction(Control.VehicleFlyThrottleUp, true);
        }

        #endregion

        #region Utils

        /// <summary>
        ///     Breaks a large string up into multiple UIText objects
        /// </summary>
        /// <param name="s">The input string</param>
        /// <param name="p">The draw point</param>
        /// <param name="sz">The font size</param>
        /// <param name="c">The text color</param>
        /// <param name="f">The font to draw</param>
        /// <param name="cnt">Whether or not to center the text</param>
        /// <returns>A list of UIText objects that compose the string</returns>
        private List<UIText> GetLargeStringUIText(string s, Point p, float sz, Color c, Font f, bool cnt) {
            var x = 0F;
            var text = new List<UIText>();
            foreach (var chunk in Split(s, 99)) {
                var size = GTAFuncs.GetTextWidth(s, f, sz);
                text.Add(new UIText(chunk, new Point(p.X + Convert.ToInt32(x), p.Y), sz, c, f, cnt));
                x += UI.WIDTH*size;
            }
            return text;
        }

        /// <summary>
        ///     Splits a string up into chunks of a specified size
        /// </summary>
        /// <param name="str">The string to splait</param>
        /// <param name="chunkSize">The desired chunk size</param>
        /// <returns>A list of chunked strings</returns>
        private static List<string> Split(string str, int chunkSize) {
            return new List<string>(Regex.Split(str, @"(?<=\G.{" + chunkSize + "})", RegexOptions.Singleline));
        }

        #endregion
    }
}
