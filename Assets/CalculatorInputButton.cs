using UnityEngine;
using System.Data;
using TexDrawLib;
using UnityEngine.InputSystem;
using System.Text;

public class SimpleCalculator : MonoBehaviour
{
    [Header("Where the math is rendered (TEXDraw UI)")]
    public TEXDraw displayTex;

    private StringBuilder inputBuffer = new StringBuilder();
    private int cursorIndex = 0;

    // Always show the cursor, no blinking
    private const string cursorChar = "|";

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Digits 0-9 (main row and numpad)
        if (kb.digit0Key.wasPressedThisFrame || kb.numpad0Key.wasPressedThisFrame) InsertAtCursor('0');
        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) InsertAtCursor('1');
        if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) InsertAtCursor('2');
        if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) InsertAtCursor('3');
        if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) InsertAtCursor('4');
        if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) InsertAtCursor('5');
        if (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame) InsertAtCursor('6');
        if (kb.digit7Key.wasPressedThisFrame || kb.numpad7Key.wasPressedThisFrame) InsertAtCursor('7');
        if (kb.digit8Key.wasPressedThisFrame || kb.numpad8Key.wasPressedThisFrame) InsertAtCursor('8');
        if (kb.digit9Key.wasPressedThisFrame || kb.numpad9Key.wasPressedThisFrame) InsertAtCursor('9');

        // Operators (main row and numpad)
        if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame) InsertAtCursor('-');
        if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame) InsertAtCursor('+');
        if (kb.slashKey.wasPressedThisFrame || kb.numpadDivideKey.wasPressedThisFrame) InsertAtCursor('/');
        if (kb.numpadMultiplyKey.wasPressedThisFrame) InsertAtCursor('*');
        if (kb.periodKey.wasPressedThisFrame || kb.numpadPeriodKey.wasPressedThisFrame) InsertAtCursor('.');

        // Backspace at cursor
        if (kb.backspaceKey.wasPressedThisFrame) Backspace();
        // Enter = evaluate
        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) Evaluate();
        // Cursor left/right
        if (kb.leftArrowKey.wasPressedThisFrame) MoveCursor(-1);
        if (kb.rightArrowKey.wasPressedThisFrame) MoveCursor(1);
    }

    // --- GUI BUTTONS ---
    public void OnButtonPressed(string s)
    {
        foreach (char c in s)
            InsertAtCursor(c);
    }

    public void OnEqualsPressed() => Evaluate();
    public void OnClearPressed()
    {
        inputBuffer.Clear();
        cursorIndex = 0;
        RenderDisplay();
    }
    public void OnLeftArrow() => MoveCursor(-1);
    public void OnRightArrow() => MoveCursor(1);

    public void InsertLatexWithBraces(string latex)
    {
        inputBuffer.Insert(cursorIndex, latex);
        cursorIndex = Mathf.Clamp(cursorIndex, 0, inputBuffer.Length);
        int nextBrace = inputBuffer.ToString().IndexOf("{}", cursorIndex);
        cursorIndex = nextBrace >= 0 ? nextBrace + 1 : inputBuffer.Length;
        RenderDisplay();
    }

    // --- CORE LOGIC ---
    private void InsertAtCursor(char c)
    {
        cursorIndex = Mathf.Clamp(cursorIndex, 0, inputBuffer.Length);
        inputBuffer.Insert(cursorIndex, c);
        cursorIndex++;
        cursorIndex = Mathf.Clamp(cursorIndex, 0, inputBuffer.Length);
        RenderDisplay();
    }

    private void Backspace()
    {
        cursorIndex = Mathf.Clamp(cursorIndex, 0, inputBuffer.Length);
        if (cursorIndex > 0)
        {
            inputBuffer.Remove(cursorIndex - 1, 1);
            cursorIndex--;
        }
        cursorIndex = Mathf.Clamp(cursorIndex, 0, inputBuffer.Length);
        RenderDisplay();
    }

private void MoveCursor(int delta)
{
    int oldIndex = cursorIndex;
    string s     = inputBuffer.ToString();
    int len      = s.Length;

    // 1) Naive one‐step clamp
    cursorIndex = Mathf.Clamp(oldIndex + delta, 0, len);

    if (delta > 0)
    {
        // 2) If on '\' (LaTeX function), jump inside its first '{'
        if (oldIndex < len && s[oldIndex] == '\\')
        {
            int funcEnd = oldIndex + 1;
            while (funcEnd < len && char.IsLetter(s[funcEnd]))
                funcEnd++;
            int firstBrace = s.IndexOf('{', funcEnd);
            if (firstBrace >= 0)
            {
                cursorIndex = firstBrace + 1;
                RenderDisplay();
                return;
            }
        }

        // 3) If on '^', jump inside its '{…}' block
        if (oldIndex < len && s[oldIndex] == '^')
        {
            int brace = s.IndexOf('{', oldIndex + 1);
            if (brace >= 0)
            {
                cursorIndex = brace + 1;
                RenderDisplay();
                return;
            }
        }

        // 4) If on '}' and next char is '^', leap into that exponent‐brace
        if (oldIndex < len 
            && s[oldIndex] == '}' 
            && oldIndex + 1 < len 
            && s[oldIndex + 1] == '^')
        {
            int brace = s.IndexOf('{', oldIndex + 2);
            if (brace >= 0)
            {
                cursorIndex = brace + 1;
                RenderDisplay();
                return;
            }
        }

        // 5) NEW: If on '}' and next char is '{', jump into that brace
        if (oldIndex < len 
            && s[oldIndex] == '}' 
            && oldIndex + 1 < len 
            && s[oldIndex + 1] == '{')
        {
            cursorIndex = oldIndex + 2;  // skip the '{' and land inside
            RenderDisplay();
            return;
        }

        // 6) If we’re on a closing '}', just accept the one‐step move
        if (oldIndex < len && s[oldIndex] == '}')
        {
            RenderDisplay();
            return;
        }

        // 7) If we’re not on an opening '{', we’re done
        if (oldIndex < len && s[oldIndex] != '{')
        {
            RenderDisplay();
            return;
        }

        // 8) Otherwise (we were on '{'), scan forward to the next brace
        for (int i = oldIndex + 1; i < len; i++)
        {
            if (s[i] == '{' || s[i] == '}')
            {
                cursorIndex = i + 1;
                break;
            }
        }
    }
    else if (delta < 0)
    {
        // 9) Skip backslash (function)
        if (oldIndex > 0 && s[oldIndex - 1] == '\\')
        {
            cursorIndex = Mathf.Max(0, oldIndex - 2);
            RenderDisplay();
            return;
        }

        // 10) Skip '^' one step
        if (oldIndex > 0 && s[oldIndex - 1] == '^')
        {
            cursorIndex = Mathf.Max(0, oldIndex - 1);
            RenderDisplay();
            return;
        }

        // 11) If stepping off '}', land inside it
        if (oldIndex > 0 && s[oldIndex - 1] == '}')
        {
            cursorIndex = oldIndex - 1;
            RenderDisplay();
            return;
        }

        // 12) If we just stepped off the first '{' of a function, jump to before '\'
        if (oldIndex > 0 && s[oldIndex - 1] == '{')
        {
            int bracePos  = oldIndex - 1;
            int funcStart = s.LastIndexOf('\\', bracePos);
            if (funcStart >= 0 && s.IndexOf('{', funcStart) == bracePos)
            {
                cursorIndex = funcStart;
                RenderDisplay();
                return;
            }
        }

        // 13) One‐step back if left char isn’t a brace
        if (oldIndex > 0 && s[oldIndex - 1] != '{' && s[oldIndex - 1] != '}')
        {
            cursorIndex = oldIndex - 1;
            RenderDisplay();
            return;
        }

        // 14) Otherwise scan backward to the previous brace
        for (int i = oldIndex - 2; i >= 0; i--)
        {
            if (s[i] == '}')
            {
                cursorIndex = i;
                break;
            }
            else if (s[i] == '{')
            {
                cursorIndex = i + 1;
                break;
            }
        }
    }

    // 15) Final clamp & redraw
    cursorIndex = Mathf.Clamp(cursorIndex, 0, len);
    RenderDisplay();
}








    private void Evaluate()
    {
        try
        {
            string result = new DataTable().Compute(inputBuffer.ToString(), null).ToString();
            inputBuffer.Clear();
            inputBuffer.Append(result);
            cursorIndex = inputBuffer.Length;
        }
        catch
        {
            inputBuffer.Clear();
            inputBuffer.Append("Error");
            cursorIndex = inputBuffer.Length;
        }
        RenderDisplay();
    }

    // --- Always called, ONLY ever updates the display, never changes buffer ---
    private void RenderDisplay()
    {
        cursorIndex = Mathf.Clamp(cursorIndex, 0, inputBuffer.Length);
        if (displayTex == null) return;
        string output = inputBuffer.ToString();
        // Always insert the visible | cursor, never blinking
        if (cursorIndex <= output.Length)
            output = output.Insert(cursorIndex, cursorChar);
        displayTex.text = output;
    }

}
