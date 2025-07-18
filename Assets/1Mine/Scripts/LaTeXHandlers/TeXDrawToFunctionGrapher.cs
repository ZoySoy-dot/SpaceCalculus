using UnityEngine;
using TexDrawLib; // Required for TEXDraw
using UnityEngine.UI;

public class TeXDrawToFunctionGrapher : MonoBehaviour
{
    [Header("TeXDraw and Grapher")]
    public TEXDraw texDraw;
    public GraphingLine grapher;

    // Called from a UI Button
    public void GraphCurrentExpression()
    {
        Debug.Log("✅ Button Clicked");

        if (texDraw == null)
        {
            Debug.LogWarning("❌ TeXDraw is NULL!");
            return;
        }

        if (grapher == null)
        {
            Debug.LogWarning("❌ Grapher is NULL!");
            return;
        }

        string latex = texDraw.text;
        latex = latex.Replace("|", ""); // Remove any | characters
        Debug.Log($"📄 Received LaTeX: {latex}");

        grapher.SetLatexExpression(latex);
    }
}
