using UnityEngine;
using org.mariuszgromada.math.mxparser;

[RequireComponent(typeof(LineRenderer))]
public class LatexFunctionGrapher : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public int steps = 100;
    public float xStart = -10f;
    public float xEnd = 10f;

    [TextArea]
    public string latexExpression = "x^2 + 3*x + 2";

    [System.NonSerialized]
    private Expression expression;

    [System.NonSerialized]
    private Argument xArg;

    void Start()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        string exprStr = PreprocessLatex(latexExpression);

        xArg = new Argument("x = 0");
        expression = new Expression(exprStr, xArg);

        DrawFunction(xStart, xEnd, steps);
    }

    public void DrawFunction(float xStart, float xEnd, int steps)
    {
        lineRenderer.positionCount = steps;

        float xRange = xEnd - xStart;

        for (int i = 0; i < steps; i++)
        {
            float t = (float)i / (steps - 1);
            double x = xStart + t * xRange;
            xArg.setArgumentValue(x);

            double y = expression.calculate();

            if (double.IsNaN(y) || double.IsInfinity(y))
                y = 0; // fallback

            lineRenderer.SetPosition(i, new Vector3((float)x, (float)y, 0));
        }
    }

    // Convert LaTeX-like to mXparser-friendly math
private string PreprocessLatex(string latex)
{
    string expr = latex;

    // Replace LaTeX function names
    expr = expr.Replace(@"\sin", "sin");
    expr = expr.Replace(@"\cos", "cos");
    expr = expr.Replace(@"\tan", "tan");
    expr = expr.Replace(@"\sqrt", "sqrt");

    // Replace \frac{a}{b} → (a)/(b)
    expr = System.Text.RegularExpressions.Regex.Replace(expr, @"\\frac\{(.*?)\}\{(.*?)\}", "($1)/($2)");

    // Replace function{arg} → function(arg)
    expr = System.Text.RegularExpressions.Regex.Replace(expr, @"(sin|cos|tan|sqrt)\{(.*?)\}", "$1($2)");

    // Remove any stray braces (optional clean-up)
    expr = expr.Replace("{", "").Replace("}", "");

    return expr;
}

}
