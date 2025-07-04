using EUtility.ValueEx;
using System.Text.Json;
using System.Text.RegularExpressions;
using TrueColorConsole;
using System.Drawing;
using GScript.Analyzer.InternalType;
using GScript.Analyzer.Util;

namespace GScript.Editor;

internal partial class Editor
{
    public int TextAreaPaddingLeft => 6;
    public int TabSize => 4;

    public StyleTable ConfigStyle { get; set; }
    public Dictionary<string, KeyUnit> Keys { get; private set; }
    public List<List<KeyUnit>> Units { get; private set; }
    public List<string> Raw { get; private set; }
    public Stack<ContentFrame> History { get; private set; }
    public Stack<ContentFrame> BackTemp { get; private set; }
    public string FileName { get; set; } = string.Empty;

    private string currentString = string.Empty;
    private int tabCount = 0;
    private bool isPreviewMode = false;
    private int col;
    private int row;
    private int previewpage;
    private int editpage;
    private int EditRow => row + editpage * Console.WindowHeight;

    public Editor(StyleTable configStyle)
    {
        ConfigStyle = configStyle ?? throw new ArgumentNullException(nameof(configStyle));
        Keys = new();
        var all = ConfigStyle.GetList();
        all.ForEach(x => Keys.Add(x.RawString, x));

        Units = new List<List<KeyUnit>> { new() };
        Raw = Enumerable.Repeat(string.Empty, 4000).ToList();
        History = new();
        BackTemp = new();
        col = TextAreaPaddingLeft;
        row = 0;
        previewpage = 0;
        editpage = 0;
    }

    public Editor(string configPath)
        : this(JsonSerializer.Deserialize<StyleTable>(File.ReadAllText(configPath), new JsonSerializerOptions { WriteIndented = true })!)
    {
    }

    public void Run()
    {
        while (true)
        {
            Update(isPreviewMode ? previewpage : editpage);
            VTConsole.CursorPosition(row + 1, col + 1);

            var key = Console.ReadKey();

            if (HandleMenu(key)) continue;
            if (HandleSave(key)) continue;
            if (HandleSaveAs(key)) continue;
            if (HandleUndo(key)) continue;
            if (HandleRedo(key)) continue;
            if (HandlePaste(key)) continue;
            if (HandleOtherKey(key)) continue;
            if (HandleNavigation(key)) continue;
            if (HandleEdit(key)) continue;

            // ∆’Õ®◊÷∑˚ ‰»Î
            isPreviewMode = false;
            if (col - TextAreaPaddingLeft == currentString.Length)
                currentString += key.KeyChar;
            else
                currentString = currentString.Insert(col - TextAreaPaddingLeft, key.KeyChar.ToString());
            col++;

            UpdateLine();
        }
    }

    private bool HandleMenu(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.F1)
        {
            ShowMenu(isPreviewMode ? previewpage : editpage);
            VTConsole.SetScrollingRegion(1, 1);
            return true;
        }
        return false;
    }

    private bool HandleSave(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.S && key.Modifiers == ConsoleModifiers.Control)
        {
            var content = GetContentArea();
            if (string.IsNullOrEmpty(FileName))
            {
                var file = ~FileList.FileDialog(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                if (file.IsVoid())
                    return true;
                File.WriteAllLines((file as FileInfo).FullName, content);
            }
            else
            {
                if (!File.Exists(FileName))
                    File.Create(FileName).Close();
                File.WriteAllLines(FileName, content);
            }
            return true;
        }
        return false;
    }

    private bool HandleSaveAs(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.S && key.Modifiers == (ConsoleModifiers.Control | ConsoleModifiers.Shift))
        {
            var content = GetContentArea();
            var file = ~FileList.FileDialog(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            if (file.IsVoid())
                return true;
            File.WriteAllLines((file as FileInfo).FullName, content);
            return true;
        }
        return false;
    }

    private bool HandleUndo(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Z && key.Modifiers == ConsoleModifiers.Control && History.Count != 0)
        {
            (var u, List<string> ru, int r, int c, int p) = History.Pop();
            BackTemp.Push((u, ru, r, c, p));
            Units = u;
            Raw = ru;
            row = r;
            col = c;
            editpage = p;
            currentString = Raw[EditRow];
            return true;
        }
        return false;
    }

    private bool HandleRedo(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Y && key.Modifiers == ConsoleModifiers.Control && BackTemp.Count != 0)
        {
            (var u, List<string> ru, int r, int c, int p) = BackTemp.Pop();
            History.Push((u, ru, r, c, p));
            Units = u;
            Raw = ru;
            row = r;
            col = c;
            editpage = p;
            currentString = Raw[EditRow];
            return true;
        }
        return false;
    }

    private bool HandlePaste(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.V && key.Modifiers == (ConsoleModifiers.Shift | ConsoleModifiers.Control))
        {
            string clipboardContent = Native.GetCurrentClipboardContent();
            if (clipboardContent == null)
                return true;

            const string NewLine = "\uABCD";
            string replacedContent = clipboardContent.Replace("\r\n", NewLine)
                                                     .Replace("\r", NewLine)
                                                     .Replace("\n", NewLine);

            string[] contentLines = replacedContent.Split(NewLine);

            string afterString = string.Empty;
            int realcol = col - TextAreaPaddingLeft - 1;

            if (realcol > 0 && Raw[EditRow].Length > realcol)
            {
                afterString = Raw[EditRow][realcol..];
                Raw[EditRow] = Raw[EditRow][..(realcol - 1)];
            }

            int insertLineCount = contentLines.Length - 2;
            if (insertLineCount < 0)
                insertLineCount = 0;

            Units[EditRow] = GetUnits(Raw[EditRow]);
            Raw[EditRow] += contentLines[0];

            if (contentLines.Length == 1)
                return true;

            string[] insertLines = contentLines[1..^1];

            if (insertLineCount != 0)
            {
                Raw.InsertRange(EditRow + 1, insertLines);

                if (Units.Count < insertLines.Length)
                    Units.AddRange(insertLines.Select(x => GetUnits(x)));
                else
                    Units.InsertRange(EditRow + 1, insertLines.Select(x => GetUnits(x)));
            }

            if (insertLineCount > -1)
            {
                string lastLine = contentLines[^1] + afterString;
                if (Units.Count < EditRow + 1)
                    Units.Add(GetUnits(lastLine));
                else
                    Units.Insert(EditRow + 1 + insertLineCount, GetUnits(lastLine));
                Raw[EditRow + 1 + insertLineCount] = lastLine;
            }

            currentString = Raw[EditRow];
            col += contentLines[0].Length;
            return true;
        }
        return false;
    }

    private bool HandleOtherKey(ConsoleKeyInfo key)
    {
        if (key.KeyChar is ' ' or '\n' || key.KeyChar >= '0')
        {
            if (BackTemp.Count != 0)
                BackTemp.Clear();
        }
        return false;
    }

    private bool HandleNavigation(ConsoleKeyInfo key)
    {
        if ((key.Key is ConsoleKey.DownArrow or ConsoleKey.UpArrow or ConsoleKey.LeftArrow or ConsoleKey.RightArrow)
            && Units.Count == 0)
            return true;
        if (key.Key == ConsoleKey.UpArrow && row == 0 && editpage == 0)
            return true;

        switch (key.Key)
        {
            case ConsoleKey.DownArrow:
                isPreviewMode = false;
                row++;
                if (row >= Console.WindowHeight - 1)
                {
                    editpage++;
                    row = 0;
                    return true;
                }
                else if (row < Console.WindowHeight)
                {
                    return true;
                }
                if (Raw[EditRow].Length != 0 && Raw[EditRow].Length + TextAreaPaddingLeft < col)
                {
                    VTConsole.CursorAbsoluteVertical(Raw[EditRow].Length - 2);
                    col = Raw[EditRow].Length + TextAreaPaddingLeft;
                }
                else if (Raw[EditRow].Length == 0)
                {
                    col = TextAreaPaddingLeft;
                }
                return true;
            case ConsoleKey.UpArrow:
                isPreviewMode = false;
                row--;
                if (row < 0 && editpage > 0)
                {
                    editpage--;
                    row = Console.WindowHeight - 1;
                    if (Raw[EditRow].Length == 0)
                    {
                        col = TextAreaPaddingLeft;
                    }
                    return true;
                }
                else if (row < 0)
                {
                    row++;
                    return true;
                }
                if (Raw[EditRow].Length != 0 && Raw[EditRow].Length + TextAreaPaddingLeft < col)
                {
                    VTConsole.CursorAbsoluteVertical(Raw[EditRow].Length + TextAreaPaddingLeft);
                    col = Raw[EditRow].Length + TextAreaPaddingLeft;
                }
                else if (Raw[EditRow].Length == 0)
                {
                    col = TextAreaPaddingLeft;
                }
                return true;
            case ConsoleKey.LeftArrow:
                isPreviewMode = false;
                col--;
                if (col < TextAreaPaddingLeft)
                {
                    col = TextAreaPaddingLeft;
                }
                return true;
            case ConsoleKey.RightArrow:
                isPreviewMode = false;
                col++;
                if (col >= Raw[EditRow].Length + TextAreaPaddingLeft)
                {
                    col = Raw[EditRow].Length + TextAreaPaddingLeft;
                }
                return true;
            case ConsoleKey.End:
                isPreviewMode = false;
                col = Raw[EditRow].Length + TextAreaPaddingLeft;
                return true;
            case ConsoleKey.Home:
                isPreviewMode = false;
                col = TextAreaPaddingLeft;
                return true;
            case ConsoleKey.PageUp:
                if (isPreviewMode && previewpage > 0)
                    previewpage--;
                else
                {
                    previewpage = editpage - 1;
                    isPreviewMode = true;
                }
                return true;
            case ConsoleKey.PageDown:
                if (isPreviewMode)
                    previewpage++;
                else
                {
                    previewpage = editpage + 1;
                    isPreviewMode = true;
                }
                return true;
        }
        return false;
    }

    private bool HandleEdit(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Enter:
                isPreviewMode = false;
                if (row == Console.WindowHeight - 1)
                {
                    Units.Add(new());
                    row = 0;
                    editpage++;
                    col = tabCount * TabSize + TextAreaPaddingLeft;
                    currentString = Raw[EditRow];
                    return true;
                }
                if (col < currentString.Length + TextAreaPaddingLeft)
                {
                    string sub = currentString.Substring(col - TextAreaPaddingLeft);
                    string front = currentString[..(col - TextAreaPaddingLeft)];
                    var l1 = GetUnits(sub);
                    var l2 = GetUnits(front);
                    Units[EditRow] = l2;
                    Raw[EditRow] = front;
                    Raw[row + 1] = sub;
                    if (Units.Count - 1 < row + 1)
                    {
                        Units.Add(Enumerable.Repeat(new KeyUnit() { RawString = "    ", Type = KeyType.Text }, tabCount).ToList());
                        Raw.Add(string.Join(string.Empty, Enumerable.Repeat("    ", tabCount)));
                        Units.Add(l1);
                    }
                    else
                    {
                        Raw.Insert(row + 1, string.Join(string.Empty, Enumerable.Repeat("    ", tabCount)));
                        Units.Insert(row + 1, Enumerable.Repeat(new KeyUnit() { RawString = "    ", Type = KeyType.Text }, tabCount).ToList());
                        Units[row + 1].AddRange(l1);
                    }
                    row++;
                    col = tabCount * TabSize + TextAreaPaddingLeft;
                    currentString = Raw[EditRow];
                    return true;
                }
                else if (col == TextAreaPaddingLeft)
                {
                    if (Units.Count <= row + Console.WindowHeight * editpage + 1)
                    {
                        Units.Add(Enumerable.Repeat(new KeyUnit() { RawString = "    ", Type = KeyType.Text }, tabCount).ToList());
                        Raw.Add(string.Join(string.Empty, Enumerable.Repeat("    ", tabCount)));
                    }
                    row++;
                    col = tabCount * TabSize + TextAreaPaddingLeft;
                    currentString = Raw[EditRow];
                    return true;
                }
                if (Units.Count < row + 1)
                {
                    Units.Add(new());
                    Raw.Insert(row + 1, string.Join(string.Empty, Enumerable.Repeat("    ", tabCount)));
                    Units.Insert(row + 1, Enumerable.Repeat(new KeyUnit() { RawString = "    ", Type = KeyType.Text }, tabCount).ToList());
                    row++;
                    col = tabCount * TabSize + TextAreaPaddingLeft;
                    currentString = Raw[EditRow];
                    return true;
                }
                else if (Units.Count >= row + 1)
                {
                    Raw.Insert(row + 1, string.Join(string.Empty, Enumerable.Repeat("    ", tabCount)));
                    Units.Insert(row + 1, Enumerable.Repeat(new KeyUnit() { RawString = "    ", Type = KeyType.Text }, tabCount).ToList());
                    row++;
                    col = tabCount * TabSize + TextAreaPaddingLeft;
                    currentString = Raw[EditRow];
                }
                col = tabCount * TabSize + TextAreaPaddingLeft;
                return true;
            case ConsoleKey.Tab:
                isPreviewMode = false;
                if (Units.Count <= row)
                {
                    Units.Add(new());
                    Raw.Insert(EditRow, string.Empty);
                }
                if (Units[EditRow].Count == 0)
                {
                    Units[EditRow].Add(MakeUnit("    ", KeyType.Text));
                    Raw[EditRow] += "    ";
                }
                else if ((col - TextAreaPaddingLeft) % TabSize == 0)
                {
                    Units[EditRow].Add(MakeUnit("    ", KeyType.Text));
                    Raw[EditRow] = Raw[EditRow].Insert(col - TextAreaPaddingLeft, "    ");
                }
                else if ((col - TextAreaPaddingLeft) % TabSize != 0)
                {
                    return true;
                }
                else if (IsOnlyType(Raw[EditRow][..(col - TextAreaPaddingLeft)], new(@"\s+")))
                {
                    Units[EditRow].Add(MakeUnit("    ", KeyType.Text));
                    Raw[EditRow] = Raw[EditRow].Insert(col - TextAreaPaddingLeft, "    ");
                }
                col += TabSize;
                tabCount++;
                return true;
            case ConsoleKey.Backspace:
            case ConsoleKey.Delete:
                return HandleBackspaceDelete(key);
        }
        return false;
    }

    private bool HandleBackspaceDelete(ConsoleKeyInfo key)
    {
        isPreviewMode = false;
        if (col == TextAreaPaddingLeft && row == 0 && editpage > 0)
        {
            if (Raw[EditRow].Length == 0)
            {
                Units.RemoveAt(EditRow);
                if (Raw[row + editpage * Console.WindowHeight - 1].Length > 0)
                {
                    col = Raw[row - 1].Length + TextAreaPaddingLeft;
                }
            }
            row = Console.WindowHeight - 1;
            editpage--;
            return true;
        }

        if (col != TextAreaPaddingLeft && (col - TextAreaPaddingLeft) % TabSize == 0 && Units[EditRow].Count >= (col == TextAreaPaddingLeft ? 0 : (col - TextAreaPaddingLeft) / TabSize) && Units[EditRow][(col - TextAreaPaddingLeft) / TabSize - 1].RawString == "    ")
        {
            if (tabCount > 0)
                tabCount--;
            Units[EditRow].RemoveAt((col - TextAreaPaddingLeft) / TabSize - 1);
            Raw[EditRow] = Raw[EditRow].Remove((col - TextAreaPaddingLeft) / TabSize - 1, TabSize);
            col -= TabSize;
            return true;
        }
        if (row == 0 && currentString.Length == 0)
        {
            if (Units.Count == 0)
                return true;
            Units.Remove(Units[EditRow]);
            return true;
        }
        else if (col == TextAreaPaddingLeft)
        {
            if (Raw[EditRow].Length == 0 && row > 0)
            {
                Units.RemoveAt(EditRow);
                row--;
                if (Raw[EditRow].Length > 0)
                {
                    col = Raw[EditRow].Length + TextAreaPaddingLeft;
                }
                return true;
            }
            string bf = Raw[row - 1];
            bf += Raw[EditRow];
            Raw[EditRow] = string.Empty;
            if (row < Units.Count)
                Units.RemoveAt(EditRow);
            Raw[row - 1] = bf;
            currentString = bf;
            row--;
            col = bf.Length + TextAreaPaddingLeft;
            var l = GetUnits(currentString);
            Units[EditRow] = l;
            return true;
        }
        if (currentString.Length == 0)
        {
            if (Units.Count - 1 >= row && Units[EditRow] != null)
            {
                Units[EditRow] = null;
                Units.Remove(Units[EditRow]);
            }
            col = Raw[row - 1].Length + TextAreaPaddingLeft;
            row--;
            return true;
        }
        else if (col < currentString.Length + TextAreaPaddingLeft)
        {
            currentString = currentString.Remove(col - 7, 1);
            col--;
        }
        else
        {
            currentString = currentString[..^1];
            col--;
        }
        UpdateLine();
        return true;
    }

    private void UpdateLine()
    {
        List<KeyUnit> ku = GetUnits(currentString);
        if (Units.Count - 1 < row)
            Units.Add(new());
        Units[EditRow] = ku;
        Raw[EditRow] = currentString;
        if (!ku.Exists((x) => x.Type == KeyType.Text))
            History.Push((Units, Raw, EditRow, col, editpage));
    }

    public KeyUnit MakeUnit(string rawString, KeyType keyType)
        => new() { Type = keyType, RawString = rawString };

    public bool IsOnlyType(string str, Regex r)
        => r.Match(str).Value.Length == str.Length;

    public List<KeyUnit> GetUnits(string currentString, bool hasCommand = true)
        => GetUnits(currentString, Keys, TabSize, hasCommand);

    public List<KeyUnit> GetUnits(string currentString, Dictionary<string, KeyUnit> keys, int tabSize, bool hasCommand = true)
    {
        List<KeyUnit> ku = new();
        int i = 0;

        while (currentString.Length > 0 && i < currentString.Length && currentString[i] == ' ')
        {
            if (i != 0 && (i + 1) % tabSize == 0)
            {
                ku.Add(MakeUnit("    ", KeyType.Text));
                currentString = currentString.Remove(0, tabSize);
                i = 0;
                continue;
            }
            i++;
        }
        if (i % tabSize != 0)
        {
            ku.Add(MakeUnit(new(' ', i % tabSize), KeyType.Text));
            currentString.Remove(0, i % tabSize);
        }

        StringSplit split = new(currentString, ' ');

        if (hasCommand)
        {
            if (split.SplitUnit.Length == 0)
                return new();
            if (keys.TryGetValue(split[0], out KeyUnit value))
                ku.Add(value);
            else
                ku.Add(MakeUnit(split[0], KeyType.Text));

            ku.Add(MakeUnit(" ", KeyType.Split));
        }

        int index = 0;

        foreach (string su in split[(hasCommand ? 1 : 0)..])
        {
            ScriptObject go = new ScriptObject();
            var part = StrParenthesis.GetStringParenthesisType(su);
            string parContent = su[1..(part.HasFlag(ParenthesisType.Half) ? ^0 : ^1)];
            switch (part.HasFlag(ParenthesisType.Half) ? part ^ ParenthesisType.Half : part)
            {
                case ParenthesisType.Unknown:
                    ku.Add(MakeUnit(su, KeyType.Text));
                    break;
                case ParenthesisType.Big:
                    ku.Add(new() { RawString = su, Type = KeyType.Type });
                    break;
                case ParenthesisType.Middle:
                    go = new ScriptObject();
                    ku.Add(MakeUnit("[", KeyType.Parenthesis));
                    if (su.Length == 1)
                        break;
                    var su2 = parContent;
                    if (!part.HasFlag(ParenthesisType.Half) && su2.Length == 0)
                        goto MiddleEnd;
                    if (!su2.Contains(":"))
                    {
                        ku.Add(MakeUnit(su2, KeyType.Text));
                        goto MiddleEnd;
                    }
                    var sp = new StringSplit(su2, ':');
                    if (su2.Length < 3) goto MiddleEnd;
                    if (keys.TryGetValue(sp[0], out KeyUnit type) && type.Type == KeyType.KnownType)
                        ku.Add(type);
                    else
                        ku.Add(MakeUnit(sp[0], KeyType.Type));
                    ku.Add(MakeUnit(":", KeyType.Symbol));
                    if (sp[0] == "flag")
                    {
                        List<KeyUnit> ku1 = new();
                        try
                        {
                            var sp2 = new StringSplitEx(string.Join(":", sp[1..]), ':', 2);
                            ku1.Add(MakeUnit(sp2[0], KeyType.Tag));
                            ku1.Add(MakeUnit(":", KeyType.Symbol));
                            if (sp2.SplitUnit.Count > 1)
                                ku1.Add(MakeUnit(sp2[1], KeyType.String));
                        }
                        catch
                        {
                            ku1.Add(MakeUnit(string.Join(string.Empty, sp[1..]), KeyType.Text));
                        }
                        ku.AddRange(ku1);
                    }
                    else if (!keys.ContainsKey(sp[0]))
                    {
                        ku.Add(MakeUnit(sp[1], KeyType.Text));
                    }
                    else if (type.Type == KeyType.KnownType &&
                            type.ConstantType.HasValue &&
                            type.ConstantType.Value != KeyType.Unknown &&
                            sp.SplitUnit.Length > 1)
                    {
                        var ct = type.ConstantType;
                        bool canparse = IsOnlyType(string.Concat(sp[1..]), CodeHighlightRuleSet.ValueVaildRegularExpressions[ct.Value]);
                        if (canparse)
                        {
                            ku.Add(MakeUnit(sp[1], ct.Value));
                        }
                        else
                        {
                            ku.Add(MakeUnit(sp[1], KeyType.Text));
                        }
                    }
                    else if (sp.SplitUnit.Length > 1)
                    {
                        ku.Add(MakeUnit(sp[1], KeyType.Text));
                    }
                MiddleEnd:
                    if (!part.HasFlag(ParenthesisType.Half))
                        ku.Add(MakeUnit("]", KeyType.Parenthesis));
                    break;
                case ParenthesisType.Small:
                    ku.Add(MakeUnit("(", KeyType.Parenthesis));
                    var su1 = parContent;
                    if (keys.TryGetValue(su1, out KeyUnit variable) && variable.Type == KeyType.CritialVariable)
                        ku.Add(MakeUnit(su1, KeyType.CritialVariable));
                    else
                        ku.Add(MakeUnit(su1, KeyType.Variable));
                    if (!part.HasFlag(ParenthesisType.Half))
                        ku.Add(MakeUnit(")", KeyType.Parenthesis));
                    break;
                case ParenthesisType.Sharp:
                    ku.Add(MakeUnit("<", KeyType.Parenthesis));

                    var aisp = new StringSplit(parContent, ',');

                    int aispi = 0;
                    foreach (string asu in aisp.SplitUnit)
                    {
                        if (asu.StartsWith(' '))
                        {
                            string mv = StartWhiteSpace().Match(asu).Value;
                            ku.Add(MakeUnit(mv, KeyType.Split));
                        }
                        List<KeyUnit> arrItem = GetUnits(asu.Trim(' '), keys, tabSize, false);
                        if (arrItem.Count > 0)
                            ku.AddRange(arrItem);
                        if (asu.EndsWith(' '))
                        {
                            string mv = EndWhiteSpace().Match(asu).Value;
                            ku.Add(MakeUnit(mv, KeyType.Split));
                        }
                        if (aispi < aisp.SplitUnit.Length - 1)
                            ku.Add(MakeUnit(",", KeyType.Symbol));

                        aispi++;
                    }

                    if (!part.HasFlag(ParenthesisType.Half))
                        ku.Add(MakeUnit(">", KeyType.Parenthesis));
                    break;
                default:
                    ku.Add(MakeUnit(su, KeyType.Text));
                    break;
            }
            if (index < split.SplitUnit.Length - 1)
                ku.Add(MakeUnit(" ", KeyType.Split));
        }

        return ku;
    }

    public List<string> GetContentArea()
    {
        List<string> line = new();
        int whitespacecount = 0;

        foreach (var l in Raw)
        {
            if (whitespacecount > 8)
            {
                line.RemoveRange(line.Count - 1 - 8, 8);
                break;
            }
            if (string.IsNullOrWhiteSpace(l))
            {
                whitespacecount++;
            }
            line.Add(l);
        }

        return line;
    }

    public void Update(int page)
    {
        Console.Clear();
        Console.Write("\x1b[3J");

        int start = page * Console.WindowHeight;
        int end = start + Console.WindowHeight;
        int line = 1 + start;
        if (start > Units.Count)
            return;

        var list = Units ?? new();
        foreach (var l in list.ToArray()[start..Math.Min(end, list.Count)])
        {
            Console.Write("      ");
            Console.CursorLeft = 0;
            Console.Write(line);
            Console.CursorLeft = TextAreaPaddingLeft;
            foreach (var u in l)
            {
                if (ConfigStyle.ColorStyle[u.Type].ForegroundColor.Equals(CodeHighlightRuleSet._none))
                {
                    VTConsole.SetColorForeground((Color)CodeHighlightRuleSet._fore);
                }
                else
                {
                    VTConsole.SetColorForeground((Color)ConfigStyle.ColorStyle[u.Type].ForegroundColor);
                }

                if (ConfigStyle.ColorStyle[u.Type].BackgroundColor.Equals(CodeHighlightRuleSet._none))
                {
                    VTConsole.SetColorBackground((Color)CodeHighlightRuleSet._back);
                }
                else
                {
                    VTConsole.SetColorBackground((Color)ConfigStyle.ColorStyle[u.Type].BackgroundColor);
                }
                VTConsole.Write(u.RawString);
            }
            if (line != end)
                VTConsole.WriteLine();
            line++;
        }
    }

    [GeneratedRegex("\\s+")]
    public static partial Regex StartWhiteSpace();

    [GeneratedRegex("$\\s+")]
    public static partial Regex EndWhiteSpace();
}
