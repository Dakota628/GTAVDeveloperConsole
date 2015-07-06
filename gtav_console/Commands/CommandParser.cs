using System;
using System.CodeDom.Compiler;
using Microsoft.CSharp;

namespace DeveloperConsole {
    internal class CommandParser {
        private const char EOF = (char) 0;
        private readonly DeveloperConsole _console;
        private readonly string _data;
        private int _column;
        private int _line;
        private int _pos;
        private int _saveCol;
        private int _saveLine;
        private int _savePos;

        public CommandParser(string data, DeveloperConsole console) {
            if (data == null) throw new ArgumentNullException("data");
            _data = data;
            _console = console;
            Reset();
        }

        public char[] SymbolChars { get; set; }
        public bool IgnoreWhiteSpace { get; set; }

        private void Reset() {
            IgnoreWhiteSpace = false;
            SymbolChars = new[] {
                '=', '+', '-', '/', ',', '.', '*', '~', '!', '@', '#', '$', '%', '^', '&', '(', ')', '[', ']', ':',
                ';',
                '<', '>', '?', '|', '\\'
            };

            _line = 1;
            _column = 1;
            _pos = 0;
        }

        protected char LA(int count) {
            if (_pos + count >= _data.Length) return EOF;
            return _data[_pos + count];
        }

        protected char Consume() {
            var ret = _data[_pos];
            _pos++;
            _column++;

            return ret;
        }

        protected CommandToken CreateToken(CommandTokenKind kind, string value) {
            return new CommandToken(kind, value, _line, _column, _console);
        }

        protected CommandToken CreateToken(CommandTokenKind kind) {
            var tokenData = _data.Substring(_savePos, _pos - _savePos);

            if (kind == CommandTokenKind.QuotedString) {
                if (tokenData[0] == '"') tokenData = tokenData.Remove(0, 1);
                if (tokenData[tokenData.Length - 1] == '"') tokenData = tokenData.Remove(tokenData.Length - 1, 1);
            }

            if (kind == CommandTokenKind.CodeBlock) {
                if (tokenData[0] == '{') tokenData = tokenData.Remove(0, 1);
                if (tokenData[tokenData.Length - 1] == '}') tokenData = tokenData.Remove(tokenData.Length - 1, 1);
            }

            return new CommandToken(kind, tokenData, _saveLine, _saveCol, _console);
        }

        public CommandToken Next() {
            ReadToken:

            var ch = LA(0);
            switch (ch) {
                case EOF:
                    return CreateToken(CommandTokenKind.EOF, string.Empty);

                case ' ':
                case '\t': {
                    if (IgnoreWhiteSpace) {
                        Consume();
                        goto ReadToken;
                    }
                    return ReadWhitespace();
                }
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    return ReadNumber();

                case '\r': {
                    StartRead();
                    Consume();
                    if (LA(0) == '\n') Consume();

                    _line++;
                    _column = 1;

                    return CreateToken(CommandTokenKind.EOL);
                }
                case '\n': {
                    StartRead();
                    Consume();
                    _line++;
                    _column = 1;

                    return CreateToken(CommandTokenKind.EOL);
                }

                case '"': {
                    return ReadString();
                }

                case '{': {
                    return ReadCodeBlock();
                }

                default: {
                    if (char.IsLetter(ch) || ch == '_' || ch == '-') return ReadWord();
                    if (IsSymbol(ch)) {
                        StartRead();
                        Consume();
                        return CreateToken(CommandTokenKind.Symbol);
                    }
                    StartRead();
                    Consume();
                    return CreateToken(CommandTokenKind.Unknown);
                }
            }
        }

        private void StartRead() {
            _saveLine = _line;
            _saveCol = _column;
            _savePos = _pos;
        }

        protected CommandToken ReadWhitespace() {
            StartRead();

            Consume();

            while (true) {
                var ch = LA(0);
                if (ch == '\t' || ch == ' ') Consume();
                else break;
            }

            return CreateToken(CommandTokenKind.WhiteSpace);
        }

        protected CommandToken ReadNumber() {
            StartRead();

            var hadDot = false;

            Consume();

            while (true) {
                var ch = LA(0);
                if (char.IsDigit(ch)) Consume();
                else if (ch == '.' && !hadDot) {
                    hadDot = true;
                    Consume();
                }
                else break;
            }

            return CreateToken(CommandTokenKind.Number);
        }

        protected CommandToken ReadWord() {
            StartRead();

            Consume();

            while (true) {
                var ch = LA(0);
                if (char.IsLetter(ch) || ch == '_' || ch == '-') Consume();
                else break;
            }

            return CreateToken(CommandTokenKind.Word);
        }

        protected CommandToken ReadString() {
            StartRead();

            Consume();

            while (true) {
                var ch = LA(0);
                if (ch == EOF) break;
                if (ch == '\r') {
                    Consume();
                    if (LA(0) == '\n')
                        Consume();

                    _line++;
                    _column = 1;
                }
                else if (ch == '\n') {
                    Consume();

                    _line++;
                    _column = 1;
                }
                else if (ch == '"') {
                    Consume();
                    if (LA(0) != '"') break;
                    Consume();
                }
                else Consume();
            }

            return CreateToken(CommandTokenKind.QuotedString);
        }

        protected CommandToken ReadCodeBlock() {
            StartRead();

            Consume();

            while (true) {
                var ch = LA(0);
                if (ch == EOF) break;
                if (ch == '\r') {
                    Consume();
                    if (LA(0) == '\n') Consume();

                    _line++;
                    _column = 1;
                }
                else if (ch == '\n') {
                    Consume();

                    _line++;
                    _column = 1;
                }
                else if (ch == '}') {
                    Consume();
                    if (LA(0) != '}') break;
                    Consume();
                }
                else Consume();
            }

            return CreateToken(CommandTokenKind.CodeBlock);
        }

        protected bool IsSymbol(char c) {
            foreach (var t in SymbolChars) if (t == c) return true;
            return false;
        }
    }

    public enum CommandTokenKind {
        Unknown,
        Word,
        Number,
        QuotedString,
        CodeBlock,
        WhiteSpace,
        Symbol,
        EOL,
        EOF
    }

    public class CommandToken {
        private readonly DeveloperConsole _console;

        public CommandToken(CommandTokenKind kind, string @string, int line, int column,
            DeveloperConsole console) {
            Kind = kind;
            String = @string;
            Line = line;
            Column = column;
            _console = console;
        }

        public int Column { get; private set; }
        public CommandTokenKind Kind { get; private set; }
        public int Line { get; private set; }
        public string String { get; private set; }

        public object Eval {
            get {
                switch (Kind) {
                    case CommandTokenKind.Word:
                        bool res;
                        if (bool.TryParse(String, out res)) return res;
                        break;
                    case CommandTokenKind.CodeBlock:
                        return CSharpEval(String);
                    case CommandTokenKind.Number:
                        return Convert.ToDouble(String);
                }
                return String;
            }
        }

        public Type Type {
            get { return Eval.GetType(); }
        }

        private object CSharpEval(string cs) {
            var c = new CSharpCodeProvider();
            var cp = new CompilerParameters();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                try {
                    var location = assembly.Location;
                    if (!string.IsNullOrEmpty(location)) cp.ReferencedAssemblies.Add(location);
                }
                catch (NotSupportedException) {
                }
            }

            cp.ReferencedAssemblies.Add(typeof (DeveloperConsole).Assembly.Location);

            foreach (var s in DeveloperConsole.RegisteredScripts)
                cp.ReferencedAssemblies.Add(s.Key.GetType().Assembly.Location);

            cp.CompilerOptions = "/t:library";
            cp.GenerateInMemory = true;

            var code = "using System;\n";
            code += "using System.Drawing;\n";
            code += "using System.Windows.Forms;\n";
            code += "using GTA;\n";
            code += "using GTA.Math;\n";
            code += "using GTA.Native;\n";
            code += "using DeveloperConsole;\n";

            code += "namespace DeveloperConsoleCodeEval{ \n";
            code += "public class DeveloperConsoleCodeEval{ \n";
            code += "public object EvalCode(){\n";
            code += "Player PLAYER = Game.Player; \n";
            code += "Ped PED = PLAYER.Character;\n";
            code += "int MP_ID = Function.Call<int>(Hash.NETWORK_GET_PLAYER_INDEX, PED.Handle);\n";

            code += cs + ";\n";
            code += "return null; \n";
            code += "} \n";
            code += "} \n";
            code += "}\n";

            var cr = c.CompileAssemblyFromSource(cp, code);
            if (cr.Errors.Count > 0) {
                _console.PrintError("C# ERROR: " + cr.Errors[0].ErrorText + " at " + cr.Errors[0].Line + ":" +
                                    cr.Errors[0].Column);
                return null;
            }

            var a = cr.CompiledAssembly;
            var o = a.CreateInstance("DeveloperConsoleCodeEval.DeveloperConsoleCodeEval");

            if (o != null) {
                var t = o.GetType();
                var mi = t.GetMethod("EvalCode");
                var s = mi.Invoke(o, null);
                return s;
            }
            return null;
        }
    }
}