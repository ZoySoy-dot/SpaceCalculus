using UnityEngine;
using System.Data;
using TexDrawLib;
using UnityEngine.InputSystem;
using System.Text;
using System;
using TexDrawLib;
using UnityEngine.UI;
using System.Collections.Generic;

public class SimpleCalculator : MonoBehaviour
{
    [Header("Where the math is rendered (TEXDraw UI)")]
    public TEXDraw displayTex;
    
    private StringBuilder inputBuffer = new StringBuilder();
    private int cursorIndex = 0;
    [SerializeField] private GraphingLine graph;
    [SerializeField] private PlayerSaveLocation saveSystem;
    
    // Cursor characters - safe for LaTeX
    private const string normalCursor = "|";  // Standard cursor
    private const string smallCursor = "|";   // Same cursor for inside functions
    
    // Maximum nesting depth for functions
    private const int MAX_NESTING_DEPTH = 3;
    
    private void TeleportToLastSave()
    {
        Debug.Log("🔁 Teleporting to last save location..."); 
        saveSystem.LoadLastSavedLocation();
        Debug.Log("✅ Teleported to last save location.");
    }

    private void Start()
    {
        graph = FindFirstObjectByType<GraphingLine>();
        if (graph == null)
            Debug.LogWarning("⚠️ No GraphingLine found in the scene!");

        if (saveSystem == null)
            saveSystem = FindFirstObjectByType<PlayerSaveLocation>();

        if (saveSystem == null)
            Debug.LogWarning("⚠️ No PlayerSaveLocation found in the scene!");
    }

    // === PUBLIC BUTTON METHODS ===
    
    public void OnButtonPressed(string s)
    {
        TeleportToLastSave();
        foreach (char c in s)
            InsertAtCursor(c);
        RenderDisplay();
    }

    public void OnClearPressed()
    {
        inputBuffer.Clear();
        cursorIndex = 0;
        RenderDisplay();
        graph.ClearGraph();
    }

    public void OnLeftArrow() => MoveCursor(-1);
    public void OnRightArrow() => MoveCursor(1);

    public void InsertLatexWithBraces(string latex)
    {
        TeleportToLastSave();
        
        if (!CanInsertFunction(latex))
        {
            Debug.LogWarning($"Cannot insert {latex} - would create infinite nesting");
            return;
        }
        
        inputBuffer.Insert(cursorIndex, latex);
        cursorIndex += latex.Length;
        
        // Find the first empty brace pair {} and position cursor inside
        string currentText = inputBuffer.ToString();
        int emptyBraceIndex = currentText.IndexOf("{}", cursorIndex - latex.Length);
        if (emptyBraceIndex >= 0)
        {
            cursorIndex = emptyBraceIndex + 1;
        }
        
        RenderDisplay();
    }

    public void InsertOperator(string op)
    {
        TeleportToLastSave();
        
        if (op == "^")
        {
            if (GetExponentNestingDepth() >= MAX_NESTING_DEPTH)
            {
                Debug.LogWarning("Cannot add more exponents - maximum nesting reached");
                return;
            }
            
            string exponentWithBraces = "^{}";
            inputBuffer.Insert(cursorIndex, exponentWithBraces);
            cursorIndex += 2;
            RenderDisplay();
            return;
        }
        
        string operatorWithSpaces = op;
        if (op == "+" || op == "-" || op == "*" || op == "/" || op == "=")
        {
            operatorWithSpaces = " " + op + " ";
        }
        
        foreach (char c in operatorWithSpaces)
            InsertAtCursor(c);
        RenderDisplay();
    }
    
    public void InsertParentheses(string paren)
    {
        TeleportToLastSave();
        InsertAtCursor(paren[0]);
        RenderDisplay();
    }

    // === CORE BACKSPACE LOGIC ===
    
    public void Backspace()
    {
        TeleportToLastSave();
        if (inputBuffer.Length == 0 || cursorIndex == 0)
            return;

        string expression = inputBuffer.ToString();
        
        // Scientific Calculator Backspace Logic
        // Priority: Stay inside brackets, character-by-character INSIDE functions, function-by-function OUTSIDE
        
        // 1. Special case: Don't delete opening braces when inside empty function arguments
        if (IsInsideEmptyFunctionArgument(expression, cursorIndex))
        {
            // Do nothing - don't delete opening braces when inside empty function fields
            return;
        }
        
        // 2. Check if we're inside a function argument with content
        if (IsInsideFunctionWithContent(expression, cursorIndex))
        {
            // Delete character by character when editing inside functions
            inputBuffer.Remove(cursorIndex - 1, 1);
            cursorIndex--;
            RenderDisplay();
            return;
        }
        
        // 3. Check if we're at the end of a complete function
        int functionStart = FindFunctionToDelete(expression, cursorIndex);
        if (functionStart >= 0)
        {
            // Delete the entire function as one unit
            inputBuffer.Remove(functionStart, cursorIndex - functionStart);
            cursorIndex = functionStart;
            RenderDisplay();
            return;
        }
        
        // 4. Default: delete single character
        inputBuffer.Remove(cursorIndex - 1, 1);
        cursorIndex--;
        RenderDisplay();
    }

    // === BACKSPACE HELPER METHODS ===
    
    private bool IsInsideEmptyFunctionArgument(string expression, int cursorPos)
    {
        if (cursorPos == 0) return false;
        
        // Find the current brace context
        int braceDepth = 0;
        int lastOpenBrace = -1;
        
        for (int i = 0; i < cursorPos; i++)
        {
            if (expression[i] == '{')
            {
                braceDepth++;
                lastOpenBrace = i;
            }
            else if (expression[i] == '}')
            {
                braceDepth--;
                if (braceDepth == 0)
                    lastOpenBrace = -1;
            }
        }
        
        // We're inside braces if depth > 0
        if (braceDepth > 0 && lastOpenBrace >= 0)
        {
            // Check if we're directly after an opening brace (empty position)
            if (cursorPos == lastOpenBrace + 1)
            {
                // We're right after '{' in an empty function argument - don't allow backspace
                return true;
            }
            
            // Check if we're about to delete an opening brace
            if (cursorPos > 0 && expression[cursorPos - 1] == '{')
            {
                // Don't delete opening braces when inside function arguments
                return true;
            }
        }
        
        return false;
    }
    
    private bool IsInsideFunctionWithContent(string expression, int cursorPos)
    {
        if (cursorPos == 0) return false;
        
        // Find the current brace context
        int braceDepth = 0;
        int lastOpenBrace = -1;
        
        for (int i = 0; i < cursorPos; i++)
        {
            if (expression[i] == '{')
            {
                braceDepth++;
                lastOpenBrace = i;
            }
            else if (expression[i] == '}')
            {
                braceDepth--;
                if (braceDepth == 0)
                    lastOpenBrace = -1;
            }
        }
        
        // We're inside braces if depth > 0
        if (braceDepth > 0 && lastOpenBrace >= 0)
        {
            // Check if there's content between the opening brace and cursor
            for (int i = lastOpenBrace + 1; i < cursorPos; i++)
            {
                if (!char.IsWhiteSpace(expression[i]))
                {
                    // We found content, allow character-by-character deletion
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private int FindFunctionToDelete(string expression, int cursorPos)
    {
        if (cursorPos == 0) return -1;
        
        // Check for complete function patterns ending at cursor
        
        // Pattern: \sin{...}| 
        if (CheckFunctionPattern(expression, cursorPos, "\\sin{", "}"))
            return FindFunctionStart(expression, cursorPos, "\\sin{");
        
        // Pattern: \cos{...}|
        if (CheckFunctionPattern(expression, cursorPos, "\\cos{", "}"))
            return FindFunctionStart(expression, cursorPos, "\\cos{");
        
        // Pattern: \tan{...}|
        if (CheckFunctionPattern(expression, cursorPos, "\\tan{", "}"))
            return FindFunctionStart(expression, cursorPos, "\\tan{");
        
        // Pattern: \ln{...}|
        if (CheckFunctionPattern(expression, cursorPos, "\\ln{", "}"))
            return FindFunctionStart(expression, cursorPos, "\\ln{");
        
        // Pattern: \sqrt{...}|
        if (CheckFunctionPattern(expression, cursorPos, "\\sqrt{", "}"))
            return FindFunctionStart(expression, cursorPos, "\\sqrt{");
        
        // Pattern: \log_{...}{...}|
        if (CheckLogPattern(expression, cursorPos))
            return FindLogStart(expression, cursorPos);
        
        // Pattern: \frac{...}{...}|
        if (CheckFractionPattern(expression, cursorPos))
            return FindFractionStart(expression, cursorPos);
        
        // Pattern: \int{...}dx|
        if (CheckIntegralPattern(expression, cursorPos))
            return FindIntegralStart(expression, cursorPos);
        
        // Pattern: exponent ^{...}|
        if (CheckExponentPattern(expression, cursorPos))
            return FindExponentStart(expression, cursorPos);
        
        // Check for empty function templates
        if (cursorPos > 0 && expression[cursorPos - 1] == '{')
        {
            return FindEmptyFunctionStart(expression, cursorPos);
        }
        
        return -1;
    }
    
    private bool CheckFunctionPattern(string expression, int cursorPos, string startPattern, string endPattern)
    {
        if (cursorPos < startPattern.Length + endPattern.Length)
            return false;
        
        // Must end with }
        if (!expression.Substring(cursorPos - endPattern.Length, endPattern.Length).Equals(endPattern))
            return false;
        
        // Find matching opening brace
        int braceDepth = 1;
        int searchPos = cursorPos - endPattern.Length - 1;
        
        while (searchPos >= 0 && braceDepth > 0)
        {
            if (expression[searchPos] == '}') braceDepth++;
            else if (expression[searchPos] == '{') braceDepth--;
            searchPos--;
        }
        
        if (braceDepth == 0)
        {
            searchPos++; // Move to opening brace
            int patternStart = searchPos - (startPattern.Length - 1);
            if (patternStart >= 0)
            {
                string pattern = expression.Substring(patternStart, startPattern.Length);
                return pattern.Equals(startPattern);
            }
        }
        
        return false;
    }
    
    private int FindFunctionStart(string expression, int cursorPos, string startPattern)
    {
        int braceDepth = 1;
        int searchPos = cursorPos - 2; // Skip closing }
        
        while (searchPos >= 0 && braceDepth > 0)
        {
            if (expression[searchPos] == '}') braceDepth++;
            else if (expression[searchPos] == '{') braceDepth--;
            searchPos--;
        }
        
        if (braceDepth == 0)
        {
            searchPos++; // Move to opening brace
            int patternStart = searchPos - (startPattern.Length - 1);
            if (patternStart >= 0)
            {
                string pattern = expression.Substring(patternStart, startPattern.Length);
                if (pattern.Equals(startPattern))
                    return patternStart;
            }
        }
        
        return -1;
    }
    
    private bool CheckLogPattern(string expression, int cursorPos)
    {
        if (cursorPos < 6) return false;
        if (expression[cursorPos - 1] != '}') return false;
        
        // Find argument brace
        int braceDepth = 1;
        int argStart = cursorPos - 2;
        
        while (argStart >= 0 && braceDepth > 0)
        {
            if (expression[argStart] == '}') braceDepth++;
            else if (expression[argStart] == '{') braceDepth--;
            argStart--;
        }
        
        if (braceDepth == 0 && argStart >= 0)
        {
            argStart++; // Move to opening brace
            if (argStart > 0 && expression[argStart - 1] == '}')
            {
                // Find base brace
                int baseEnd = argStart - 1;
                braceDepth = 1;
                int baseStart = baseEnd - 1;
                
                while (baseStart >= 0 && braceDepth > 0)
                {
                    if (expression[baseStart] == '}') braceDepth++;
                    else if (expression[baseStart] == '{') braceDepth--;
                    baseStart--;
                }
                
                if (braceDepth == 0 && baseStart >= 4)
                {
                    baseStart++; // Move to opening brace
                    // Check for complete \log_{...}{...} pattern
                    if (baseStart >= 5 && expression.Substring(baseStart - 5, 5) == "\\log_")
                    {
                        // Verify this is a complete, well-formed log function
                        // Must have pattern: \log_{base}{argument}
                        string logPattern = expression.Substring(baseStart - 5, cursorPos - (baseStart - 5));
                        if (IsWellFormedLogFunction(logPattern))
                            return true;
                    }
                }
            }
        }
        
        return false;
    }
    
    private int FindLogStart(string expression, int cursorPos)
    {
        int braceDepth = 1;
        int argStart = cursorPos - 2;
        
        while (argStart >= 0 && braceDepth > 0)
        {
            if (expression[argStart] == '}') braceDepth++;
            else if (expression[argStart] == '{') braceDepth--;
            argStart--;
        }
        
        if (braceDepth == 0 && argStart >= 0)
        {
            argStart++;
            if (argStart > 0 && expression[argStart - 1] == '}')
            {
                int baseEnd = argStart - 1;
                braceDepth = 1;
                int baseStart = baseEnd - 1;
                
                while (baseStart >= 0 && braceDepth > 0)
                {
                    if (expression[baseStart] == '}') braceDepth++;
                    else if (expression[baseStart] == '{') braceDepth--;
                    baseStart--;
                }
                
                if (braceDepth == 0 && baseStart >= 4)
                {
                    baseStart++;
                    if (baseStart >= 5 && expression.Substring(baseStart - 5, 5) == "\\log_")
                        return baseStart - 5;
                }
            }
        }
        
        return -1;
    }
    
    private bool CheckFractionPattern(string expression, int cursorPos)
    {
        if (cursorPos < 6) return false;
        if (expression[cursorPos - 1] != '}') return false;
        
        // Find denominator brace
        int braceDepth = 1;
        int denomStart = cursorPos - 2;
        
        while (denomStart >= 0 && braceDepth > 0)
        {
            if (expression[denomStart] == '}') braceDepth++;
            else if (expression[denomStart] == '{') braceDepth--;
            denomStart--;
        }
        
        if (braceDepth == 0 && denomStart >= 0)
        {
            denomStart++;
            if (denomStart > 0 && expression[denomStart - 1] == '}')
            {
                // Find numerator brace
                int numerEnd = denomStart - 1;
                braceDepth = 1;
                int numerStart = numerEnd - 1;
                
                while (numerStart >= 0 && braceDepth > 0)
                {
                    if (expression[numerStart] == '}') braceDepth++;
                    else if (expression[numerStart] == '{') braceDepth--;
                    numerStart--;
                }
                
                if (braceDepth == 0 && numerStart >= 4)
                {
                    numerStart++;
                    if (numerStart >= 5 && expression.Substring(numerStart - 5, 5) == "\\frac")
                        return true;
                }
            }
        }
        
        return false;
    }
    
    private int FindFractionStart(string expression, int cursorPos)
    {
        int braceDepth = 1;
        int denomStart = cursorPos - 2;
        
        while (denomStart >= 0 && braceDepth > 0)
        {
            if (expression[denomStart] == '}') braceDepth++;
            else if (expression[denomStart] == '{') braceDepth--;
            denomStart--;
        }
        
        if (braceDepth == 0 && denomStart >= 0)
        {
            denomStart++;
            if (denomStart > 0 && expression[denomStart - 1] == '}')
            {
                int numerEnd = denomStart - 1;
                braceDepth = 1;
                int numerStart = numerEnd - 1;
                
                while (numerStart >= 0 && braceDepth > 0)
                {
                    if (expression[numerStart] == '}') braceDepth++;
                    else if (expression[numerStart] == '{') braceDepth--;
                    numerStart--;
                }
                
                if (braceDepth == 0 && numerStart >= 4)
                {
                    numerStart++;
                    if (numerStart >= 5 && expression.Substring(numerStart - 5, 5) == "\\frac")
                        return numerStart - 5;
                }
            }
        }
        
        return -1;
    }
    
    private bool CheckIntegralPattern(string expression, int cursorPos)
    {
        if (cursorPos < 6) return false;
        if (cursorPos < 2 || expression.Substring(cursorPos - 2, 2) != "dx") return false;
        
        int dxStart = cursorPos - 2;
        if (dxStart > 0 && expression[dxStart - 1] == '}')
        {
            int braceDepth = 1;
            int intStart = dxStart - 2;
            
            while (intStart >= 0 && braceDepth > 0)
            {
                if (expression[intStart] == '}') braceDepth++;
                else if (expression[intStart] == '{') braceDepth--;
                intStart--;
            }
            
            if (braceDepth == 0 && intStart >= 3)
            {
                intStart++;
                if (intStart >= 4 && expression.Substring(intStart - 4, 4) == "\\int")
                    return true;
            }
        }
        
        return false;
    }
    
    private int FindIntegralStart(string expression, int cursorPos)
    {
        int dxStart = cursorPos - 2;
        if (dxStart > 0 && expression[dxStart - 1] == '}')
        {
            int braceDepth = 1;
            int intStart = dxStart - 2;
            
            while (intStart >= 0 && braceDepth > 0)
            {
                if (expression[intStart] == '}') braceDepth++;
                else if (expression[intStart] == '{') braceDepth--;
                intStart--;
            }
            
            if (braceDepth == 0 && intStart >= 3)
            {
                intStart++;
                if (intStart >= 4 && expression.Substring(intStart - 4, 4) == "\\int")
                    return intStart - 4;
            }
        }
        
        return -1;
    }
    
    private bool CheckExponentPattern(string expression, int cursorPos)
    {
        if (cursorPos < 3) return false;
        if (expression[cursorPos - 1] != '}') return false;
        
        int braceDepth = 1;
        int expStart = cursorPos - 2;
        
        while (expStart >= 0 && braceDepth > 0)
        {
            if (expression[expStart] == '}') braceDepth++;
            else if (expression[expStart] == '{') braceDepth--;
            expStart--;
        }
        
        if (braceDepth == 0 && expStart >= 0)
        {
            expStart++;
            if (expStart > 0 && expression[expStart - 1] == '^')
                return true;
        }
        
        return false;
    }
    
    private int FindExponentStart(string expression, int cursorPos)
    {
        int braceDepth = 1;
        int expStart = cursorPos - 2;
        
        while (expStart >= 0 && braceDepth > 0)
        {
            if (expression[expStart] == '}') braceDepth++;
            else if (expression[expStart] == '{') braceDepth--;
            expStart--;
        }
        
        if (braceDepth == 0 && expStart >= 0)
        {
            expStart++;
            if (expStart > 0 && expression[expStart - 1] == '^')
                return expStart - 1;
        }
        
        return -1;
    }
    
    private int FindEmptyFunctionStart(string expression, int cursorPos)
    {
        if (cursorPos == 0 || expression[cursorPos - 1] != '{') return -1;
        if (cursorPos >= expression.Length || expression[cursorPos] != '}') return -1;
        
        // Check for empty function patterns
        if (cursorPos >= 5 && expression.Substring(cursorPos - 5, 5) == "\\sin{")
            return cursorPos - 5;
        if (cursorPos >= 5 && expression.Substring(cursorPos - 5, 5) == "\\cos{")
            return cursorPos - 5;
        if (cursorPos >= 5 && expression.Substring(cursorPos - 5, 5) == "\\tan{")
            return cursorPos - 5;
        if (cursorPos >= 4 && expression.Substring(cursorPos - 4, 4) == "\\ln{")
            return cursorPos - 4;
        if (cursorPos >= 6 && expression.Substring(cursorPos - 6, 6) == "\\sqrt{")
            return cursorPos - 6;
        if (cursorPos >= 5 && expression.Substring(cursorPos - 5, 5) == "\\int{")
            return cursorPos - 5;
        if (cursorPos >= 6 && expression.Substring(cursorPos - 6, 6) == "\\frac{")
            return cursorPos - 6;
        if (cursorPos >= 6 && expression.Substring(cursorPos - 6, 6) == "\\log_{")
            return cursorPos - 6;
        
        return -1;
    }
    
    private bool IsWellFormedLogFunction(string logPattern)
    {
        // Check if the pattern is a complete, well-formed \log_{base}{argument} function
        // Must start with \log_ and have exactly 2 brace pairs
        if (!logPattern.StartsWith("\\log_"))
            return false;
        
        int braceCount = 0;
        int bracePairs = 0;
        bool inBracePair = false;
        
        for (int i = 5; i < logPattern.Length; i++) // Skip "\log_"
        {
            if (logPattern[i] == '{')
            {
                braceCount++;
                if (braceCount == 1)
                    inBracePair = true;
            }
            else if (logPattern[i] == '}')
            {
                braceCount--;
                if (braceCount == 0 && inBracePair)
                {
                    bracePairs++;
                    inBracePair = false;
                }
            }
        }
        
        // Must have exactly 2 complete brace pairs and end with balanced braces
        return bracePairs == 2 && braceCount == 0;
    }

    // === CURSOR MOVEMENT ===
    
    private void MoveCursor(int delta)
    {
        string expression = inputBuffer.ToString();
        int len = expression.Length;
        
        if (delta > 0)
        {
            // Moving right
            if (cursorIndex >= len)
            {
                RenderDisplay();
                return;
            }
            
            // Smart navigation for LaTeX functions
            if (expression[cursorIndex] == '\\')
            {
                // Jump to first brace of function
                int funcEnd = cursorIndex + 1;
                while (funcEnd < len && char.IsLetter(expression[funcEnd]))
                    funcEnd++;
                
                // Skip subscripts
                if (funcEnd < len && expression[funcEnd] == '_')
                {
                    funcEnd++;
                    if (funcEnd < len && expression[funcEnd] == '{')
                    {
                        funcEnd++;
                        int braceCount = 1;
                        while (funcEnd < len && braceCount > 0)
                        {
                            if (expression[funcEnd] == '{') braceCount++;
                            else if (expression[funcEnd] == '}') braceCount--;
                            funcEnd++;
                        }
                    }
                }
                
                // Find first main argument brace
                int firstBrace = expression.IndexOf('{', funcEnd);
                if (firstBrace >= 0)
                {
                    cursorIndex = firstBrace + 1;
                    RenderDisplay();
                    return;
                }
            }
            
            // Jump into exponent
            if (expression[cursorIndex] == '^')
            {
                int brace = expression.IndexOf('{', cursorIndex + 1);
                if (brace >= 0)
                {
                    cursorIndex = brace + 1;
                    RenderDisplay();
                    return;
                }
            }
            
            // Jump from } to next {
            if (expression[cursorIndex] == '}' && cursorIndex + 1 < len)
            {
                if (expression[cursorIndex + 1] == '^')
                {
                    int brace = expression.IndexOf('{', cursorIndex + 2);
                    if (brace >= 0)
                    {
                        cursorIndex = brace + 1;
                        RenderDisplay();
                        return;
                    }
                }
                else if (expression[cursorIndex + 1] == '{')
                {
                    cursorIndex = cursorIndex + 2;
                    RenderDisplay();
                    return;
                }
            }
            
            cursorIndex++;
        }
        else if (delta < 0)
        {
            // Moving left
            if (cursorIndex <= 0)
            {
                RenderDisplay();
                return;
            }
            
            // Smart navigation for LaTeX functions
            if (cursorIndex >= 2 && expression[cursorIndex - 2] == '^' && expression[cursorIndex - 1] == '{')
            {
                cursorIndex = cursorIndex - 2;
                RenderDisplay();
                return;
            }
            
            // Jump from inside function to before function
            if (cursorIndex > 0 && expression[cursorIndex - 1] == '{')
            {
                int bracePos = cursorIndex - 1;
                int checkPos = bracePos - 1;
                
                // Skip subscripts
                while (checkPos >= 0 && (expression[checkPos] == '_' || expression[checkPos] == '^'))
                    checkPos--;
                
                if (checkPos >= 0 && char.IsLetter(expression[checkPos]))
                {
                    while (checkPos > 0 && char.IsLetter(expression[checkPos - 1]))
                        checkPos--;
                    
                    if (checkPos > 0 && expression[checkPos - 1] == '\\')
                    {
                        cursorIndex = checkPos - 1;
                        RenderDisplay();
                        return;
                    }
                }
            }
            
            cursorIndex--;
        }
        
        cursorIndex = Mathf.Clamp(cursorIndex, 0, len);
        RenderDisplay();
    }

    // === UTILITY METHODS ===
    
    private void InsertAtCursor(char c)
    {
        cursorIndex = Mathf.Clamp(cursorIndex, 0, inputBuffer.Length);
        inputBuffer.Insert(cursorIndex, c);
        cursorIndex++;
        cursorIndex = Mathf.Clamp(cursorIndex, 0, inputBuffer.Length);
    }
    
    private bool CanInsertFunction(string latex)
    {
        if (latex.StartsWith("\\frac"))
        {
            return GetNestingDepth("\\frac") < MAX_NESTING_DEPTH;
        }
        
        if (latex.Contains("^{}"))
        {
            return GetExponentNestingDepth() < MAX_NESTING_DEPTH;
        }
        
        return true;
    }
    
    private int GetNestingDepth(string functionName)
    {
        string text = inputBuffer.ToString();
        int depth = 0;
        int currentPos = cursorIndex;
        
        int openBraces = 0;
        for (int i = 0; i < currentPos; i++)
        {
            if (text[i] == '{')
            {
                openBraces++;
                if (i >= functionName.Length)
                {
                    string before = text.Substring(i - functionName.Length, functionName.Length);
                    if (before == functionName)
                    {
                        depth++;
                    }
                }
            }
            else if (text[i] == '}')
            {
                openBraces--;
                if (openBraces < 0) openBraces = 0;
            }
        }
        
        return depth;
    }
    
    private int GetExponentNestingDepth()
    {
        string text = inputBuffer.ToString();
        int depth = 0;
        int currentPos = cursorIndex;
        
        for (int i = 0; i < currentPos - 1; i++)
        {
            if (text[i] == '^' && i + 1 < text.Length && text[i + 1] == '{')
            {
                int exponentStart = i + 2;
                int braceCount = 1;
                int j = exponentStart;
                while (j < text.Length && braceCount > 0)
                {
                    if (text[j] == '{') braceCount++;
                    else if (text[j] == '}') braceCount--;
                    j++;
                }
                
                if (currentPos >= exponentStart && currentPos < j)
                {
                    depth++;
                }
            }
        }
        
        return depth;
    }
    
    private bool IsInsideFunction(string expression, int cursorPos)
    {
        if (cursorPos == 0) return false;
        
        // Find the current brace context
        int braceDepth = 0;
        
        for (int i = 0; i < cursorPos; i++)
        {
            if (expression[i] == '{')
            {
                braceDepth++;
            }
            else if (expression[i] == '}')
            {
                braceDepth--;
            }
        }
        
        // We're inside a function if we're inside any braces
        return braceDepth > 0;
    }
    
    private void RenderDisplay()
    {
        string displayText = inputBuffer.ToString();
        
        if (cursorIndex <= displayText.Length)
        {
            // Choose cursor based on context
            string currentCursor = IsInsideFunction(displayText, cursorIndex) ? smallCursor : normalCursor;
            displayText = displayText.Insert(cursorIndex, currentCursor);
        }
        
        displayTex.text = displayText;
        displayTex.Rebuild(CanvasUpdate.PreRender);
        
        graph.SetLatexExpression(inputBuffer.ToString());
    }
}