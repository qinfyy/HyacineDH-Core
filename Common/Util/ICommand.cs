namespace HyacineCore.Server.Util
{
    public static class IConsole
    {
        public const string PrefixContent = "HyacineCore> "; // "HyacineCore> "; Before execute command
        private const string PinkColor = "\e[38;2;255;192;203m";
        private const string RedColor = "\e[38;2;255;0;0m";
        private const string ResetColor = "\e[0m";

        // coloured prefix
        public static string Prefix => $"{(IsCommandValid ? ResetColor : RedColor)}{PrefixContent}{ResetColor}";

        public static bool IsCommandValid { get; private set; } = true;
        private const int HistoryMaxCount = 10;
        
        public static readonly object ConsoleLock = new();

        public static List<char> Input { get; set; } = [];
        private static int CursorIndex { get; set; }
        private static readonly List<string> InputHistory = [];
        private static int HistoryIndex = -1;

        public static event Action<string>? OnConsoleExcuteCommand;

        public static bool ForceDisable { get; set; } = false;

        private static bool? _isConsoleAvailable;
        public static bool IsConsoleAvailable
        {
            get
            {
                if (ForceDisable) return false;
                if (_isConsoleAvailable.HasValue) return _isConsoleAvailable.Value;

                try
                {
                    // Check TERM environment variable
                    var term = Environment.GetEnvironmentVariable("TERM");
                    if (string.IsNullOrEmpty(term) || term == "dumb")
                    {
                        _isConsoleAvailable = false;
                        return false;
                    }

                    _ = Environment.UserInteractive;
                    if (Console.IsInputRedirected || Console.IsOutputRedirected || Console.IsErrorRedirected)
                    {
                        _isConsoleAvailable = false;
                        return false;
                    }

                    // On Linux/Unix, WindowWidth might be 0 or throw if no TTY
                    if (Console.WindowWidth <= 0 || Console.BufferWidth <= 0)
                    {
                         _isConsoleAvailable = false;
                         return false;
                    }

                    // Verify cursor position check removed to prevent blocking
                    // _ = Console.CursorTop;
                    
                    _isConsoleAvailable = true;
                    return true;
                }
                catch
                {
                    _isConsoleAvailable = false;
                    return false;
                }
            }
        }

        public static void InitConsole()
        {
            if (!IsConsoleAvailable) return;
            Console.Title = "HyacineCore Console";
        }

        public static int GetWidth(string str)
            => str.ToCharArray().Sum(EastAsianWidth.GetLength);

        public static void RedrawInput(List<char> input, bool hasPrefix = true)
            => RedrawInput(new string([.. input]), hasPrefix);

        public static void RedrawInput(string input, bool hasPrefix = true)
        {
            if (!IsConsoleAvailable) return;

            lock (ConsoleLock)
            {
                // check validity
                UpdateCommandValidity(input);

                var inputStr = input;
                if (hasPrefix)
                {
                    inputStr = Prefix + input;
                }
                
                var totalWidth = GetWidth(inputStr);
                var cursorEffectiveIndex = CursorIndex + (hasPrefix ? GetWidth(PrefixContent) : 0);

                // Use Carriage Return to reset, print line, clear trailing, reset again, then move right
                // 1. Carriage Return
                Console.Write('\r');
                // 2. Write Input
                Console.Write(inputStr);
                // 3. Clear remaining (Safety buffer)
                var clearLen = Console.BufferWidth - totalWidth - 1;
                if (clearLen > 0) Console.Write(new string(' ', clearLen));
                
                // 4. Move Cursor to correct position
                // Since we are maybe at the end or somewhere else, let's reset to \r and move forward
                Console.Write('\r');
                if (cursorEffectiveIndex > 0) 
                {
                    // Use ANSI move right if possible, or just re-print the substring up to cursor? 
                    // Re-printing is safest cross-platform without ANSI acting weird if Term is dumb
                    // But we used colors so ANSI is likely supported.
                    // \x1b[{n}C is Move Right n
                    Console.Write($"\x1b[{cursorEffectiveIndex}C");
                }
            }
        }

        // check validity and update
        private static void UpdateCommandValidity(string input)
        {
            IsCommandValid = CheckCommandValid(input);
        }

        #region Handlers

        public static void HandleEnter()
        {
            string cmdToRun = null;
            lock (ConsoleLock)
            {
                var input = new string([.. Input]);
                if (string.IsNullOrWhiteSpace(input)) return;

                // New line
                Console.WriteLine();
                Input = [];
                CursorIndex = 0;
                if (InputHistory.Count >= HistoryMaxCount)
                    InputHistory.RemoveAt(0);
                InputHistory.Add(input);
                HistoryIndex = InputHistory.Count;

                // Handle command
                if (input.StartsWith('/')) input = input[1..].Trim();
                cmdToRun = input;

                // reset
                IsCommandValid = true;
            }
            
            if (cmdToRun != null)
                OnConsoleExcuteCommand?.Invoke(cmdToRun);
        }

        public static void HandleBackspace()
        {
            lock (ConsoleLock)
            {
                if (CursorIndex <= 0) return;
                
                // Safety check
                if (CursorIndex > Input.Count) CursorIndex = Input.Count;
                
                CursorIndex--;
                Input.RemoveAt(CursorIndex);

                // Full redraw is safer than partial differential updates that require cursor reads
                RedrawInput(Input);
            }
        }

        public static void HandleUpArrow()
        {
            lock (ConsoleLock)
            {
                if (InputHistory.Count == 0) return;
                if (HistoryIndex <= 0) return;

                HistoryIndex--;
                var history = InputHistory[HistoryIndex];
                Input = [.. history];
                CursorIndex = Input.Count;

                // update
                UpdateCommandValidity(history);
                RedrawInput(Input);
            }
        }

        public static void HandleDownArrow()
        {
            lock (ConsoleLock)
            {
                if (HistoryIndex >= InputHistory.Count) return;

                HistoryIndex++;
                if (HistoryIndex >= InputHistory.Count)
                {
                    HistoryIndex = InputHistory.Count;
                    Input = [];
                    CursorIndex = 0;
                    IsCommandValid = true;
                }
                else
                {
                    var history = InputHistory[HistoryIndex];
                    Input = [.. history];
                    CursorIndex = Input.Count;
                    // update
                    UpdateCommandValidity(history);
                }
                RedrawInput(Input);
            }
        }

        public static void HandleLeftArrow()
        {
            lock (ConsoleLock)
            {
                if (CursorIndex <= 0) return;
                CursorIndex--;
                RedrawInput(Input);
            }
        }

        public static void HandleRightArrow()
        {
            lock (ConsoleLock)
            {
                if (CursorIndex >= Input.Count) return;
                CursorIndex++;
                RedrawInput(Input);
            }
        }

        public static void HandleInput(ConsoleKeyInfo keyInfo)
        {
            lock (ConsoleLock)
            {
                 if (char.IsControl(keyInfo.KeyChar)) return;
                 var newWidth = GetWidth(new string([.. Input])) + GetWidth(keyInfo.KeyChar.ToString());
                 if (newWidth >= (Console.BufferWidth - GetWidth(PrefixContent))) return;
                 HandleInput(keyInfo.KeyChar);
            }
        }

        public static void HandleInput(char keyChar)
        {
            lock (ConsoleLock)
            {
                // Crash fix: Bounds check
                if (CursorIndex < 0) CursorIndex = 0;
                if (CursorIndex > Input.Count) CursorIndex = Input.Count;
                
                Input.Insert(CursorIndex, keyChar);
                CursorIndex++;

                RedrawInput(Input);
            }
        }

        #endregion

        public static string ListenConsole()
        {
            if (!IsConsoleAvailable) return string.Empty;

            while (true)
            {
                ConsoleKeyInfo keyInfo;
                try { keyInfo = Console.ReadKey(true); }
                catch (InvalidOperationException) { continue; }

                switch (keyInfo.Key)
                {
                    case ConsoleKey.Enter:
                        HandleEnter();
                        break;
                    case ConsoleKey.Backspace:
                        HandleBackspace();
                        break;
                    case ConsoleKey.LeftArrow:
                        HandleLeftArrow();
                        break;
                    case ConsoleKey.RightArrow:
                        HandleRightArrow();
                        break;
                    case ConsoleKey.UpArrow:
                        HandleUpArrow();
                        break;
                    case ConsoleKey.DownArrow:
                        HandleDownArrow();
                        break;
                    default:
                        HandleInput(keyInfo);
                        break;
                }
            }
        }

        private static bool CheckCommandValid(string input)
        {
            if (string.IsNullOrEmpty(input))
                return true;

            var invalidChars = new[] { '@', '#', '$', '%', '&', '*' };
            return !invalidChars.Any(c => input.Contains(c));
        }
    }

    internal static class EastAsianWidth
    {
        public static int GetLength(char c)
        {
            return c <= 0x7F ? 1 : 2;
        }
    }
}

