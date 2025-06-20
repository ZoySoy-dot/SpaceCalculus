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
    public void OnLeftArrow()  => MoveCursor(-1);
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
        cursorIndex = Mathf.Clamp(cursorIndex + delta, 0, inputBuffer.Length);
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
