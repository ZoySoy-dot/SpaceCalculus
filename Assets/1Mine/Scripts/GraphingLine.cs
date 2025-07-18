using UnityEngine;
using org.mariuszgromada.math.mxparser;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(LineRenderer))]
public class GraphingLine : MonoBehaviour
{
    public enum FunctionMode { Original, FirstDerivative, SecondDerivative }

    public LineRenderer lineRenderer;
    public int steps = 100;
    public Transform player; // NEW: Reference to the player for starting X

    [TextArea]
    public string latexExpression = "x^2 + 3*x + 2";
    public FunctionMode mode = FunctionMode.Original;

    [System.NonSerialized]
    private Expression expression;
    [System.NonSerialized]
    private Argument xArg;
    [SerializeField] private List<Hole> holes = new List<Hole>();
    [ContextMenu("🔄 Refresh Graph")]
    public void RefreshGraph()
    {
        SetLatexExpression(latexExpression);
    }

    void Start()
{
    // Always refresh holes
    holes = new List<Hole>(FindObjectsOfType<Hole>());
    Debug.Log($"🕳️ Refreshed holes: Found {holes.Count} holes in scene.");

    if (lineRenderer == null)
        lineRenderer = GetComponent<LineRenderer>();

    // ✅ Always find the 2nd Player at START
    if (player == null)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length >= 2)
        {
            player = players[1].transform;
            Debug.Log("🎮 Using second Player in scene: " + player.name);
        }
        else if (players.Length == 1)
        {
            player = players[0].transform;
            Debug.LogWarning("⚠️ Only one Player found. Using first.");
        }
        else
        {
            Debug.LogError("❌ No Player object found in scene.");
            return;
        }
    }

    // Expression setup
    string exprStr = PreprocessLatex(latexExpression);
    xArg = new Argument("x = 0");

    expression = new Expression(exprStr, xArg);

    DrawFunction(steps);
}

public void DrawFunction(int steps)
{
    if (lineRenderer == null || expression == null || !expression.checkSyntax())
    {
        Debug.LogError("❌ Cannot draw function: Missing component or invalid expression.");
        if (lineRenderer != null) lineRenderer.positionCount = 0;
        return;
    }
    // Always find the 2nd Player if not set manually
if (player == null)
{
    GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
    if (players.Length >= 2)
    {
        player = players[1].transform;
        Debug.Log("🎮 Using second Player in scene: " + player.name);
    }
    else if (players.Length == 1)
    {
        player = players[0].transform;
        Debug.LogWarning("⚠️ Only one Player found. Using first.");
    }
    else
    {
        Debug.LogError("❌ No Player object found in scene.");
        return;
    }
}

    // Auto-find holes if none assigned
    if (holes == null || holes.Count == 0)
    {
        holes = new List<Hole>(FindObjectsByType<Hole>(FindObjectsSortMode.None));
        Debug.Log($"🕳️ Auto-found {holes.Count} holes in scene.");
    }

    // Camera bounds
    Camera cam = Camera.main;
    float camH    = cam.orthographicSize * 2f;
    float camW    = camH * cam.aspect;
    float camXmin = cam.transform.position.x - camW/2f;
    float camXmax = cam.transform.position.x + camW/2f;
    float camYmin = cam.transform.position.y - camH/2f;
    float camYmax = cam.transform.position.y + camH/2f;

    // Graph domain in world-space
    float xStart = 0f;
    float xEnd   = camXmax - player.position.x;
    string exprStr = expression.getExpressionString();
    if (exprStr.Contains("log(") || exprStr.Contains("ln(") || exprStr.Contains("sqrt("))
        xStart = Mathf.Max(xStart, 0.01f);

    // Compute y-offset so that at xStart, y = player.y
    xArg.setArgumentValue(xStart);
    double y0 = expression.calculate();
    float yOffset = float.IsFinite((float)y0)
        ? (float)y0 - player.position.y
        : 0f;

    // Physics params
    Vector3 velocity = Vector3.zero;
    float dt        = 1f / (steps - 1);
    float swingAmp  = 0.2f;
    float swingFreq = Mathf.PI * 2f;

    // Prepare renderer
    lineRenderer.positionCount = steps;
    int drawn = 0;

    for (int i = 0; i < steps; i++)
    {
        float t      = i * dt;
        float xGraph = Mathf.Lerp(xStart, xEnd, t);

        xArg.setArgumentValue(xGraph);
        double yRaw = expression.calculate();
        if (double.IsNaN(yRaw) || double.IsInfinity(yRaw))
            continue;

        // Base graph point (before physics)
        Vector3 point = new Vector3(
            player.position.x + xGraph,
            (float)yRaw - yOffset,
            0f
        );
        if (i == 0)
            point = player.position;  // anchor at the player

        // === accumulate gravity/repulsion from all holes ===
// === accumulate attraction (black hole) or repulsion (white hole) ===
Vector2 totalAccel = Vector2.zero;
Vector2 p2        = new Vector2(point.x, point.y);

foreach (Hole hole in holes)
{
    Vector2 h2     = (Vector2)hole.transform.position;
    Vector2 toHole = h2 - p2;                // direction *toward* the hole
    float   dist2  = toHole.sqrMagnitude;
    if (dist2 < 1e-4f) continue;

    // rawForce >0 pulls in, <0 pushes out
    float rawForce = hole.strength / dist2;

    // clamp so you don’t get crazy huge jolts
    float f = Mathf.Clamp(rawForce, -5f, +5f);

    // *this* vector already has the correct sign baked in
    Vector2 force = toHole.normalized * f;
    totalAccel   += force;

    // debug: red = pull, cyan = push
}



        // simple Euler integration
        velocity += (Vector3)totalAccel * dt;
        point    += velocity * dt;

        // add a gentle pendulum swing
        point.x += swingAmp * Mathf.Sin(swingFreq * t);

        // cull if offscreen
        if (point.x < camXmin || point.x > camXmax ||
            point.y < camYmin || point.y > camYmax)
        {
            lineRenderer.positionCount = drawn;
            return;
        }

        // stop if we “fall into” any hole
        bool fellIn = false;
        foreach (Hole hole in holes)
        {
            if (Vector2.Distance(point, hole.transform.position) < 0.5f)
            {
                fellIn = true;
                break;
            }
        }
        if (fellIn)
        {
            lineRenderer.positionCount = drawn;
            return;
        }

        // set the vertex
        lineRenderer.SetPosition(drawn++, point);
    }

    lineRenderer.positionCount = drawn;
    Debug.Log($"📈 Line drawn with gravity & swing: {drawn} points.");
}


    private string PreprocessLatex(string latex)
{
    string expr = latex;

    // --- Trig, Log, Roots ---
    expr = expr.Replace(@"\sin", "sin");
    expr = expr.Replace(@"\cos", "cos");
    expr = expr.Replace(@"\tan", "tan");
    expr = expr.Replace(@"\ln", "ln");
    expr = expr.Replace(@"\sqrt", "sqrt");

    // --- Log base: \log_{b}{x} → log(x, b)
    expr = Regex.Replace(expr, @"\\log_\{(.*?)\}\{(.*?)\}", "log($2, $1)");

    // --- Fallback: \log{x} → log(x)
    expr = Regex.Replace(expr, @"\\log\{(.*?)\}", "log($1)");
    expr = expr.Replace(@"\log", "log");

    // --- Fractions: \frac{a}{b} → (a)/(b)
    expr = Regex.Replace(expr, @"\\frac\{(.*?)\}\{(.*?)\}", "($1)/($2)");

    // --- Exponents: a^{b} → a^(b), a^b → a^(b)
    expr = Regex.Replace(expr, @"([a-zA-Z0-9]+)\^\{(.*?)\}", "$1^($2)");
    expr = Regex.Replace(expr, @"([a-zA-Z0-9]+)\^([a-zA-Z0-9])", "$1^($2)");

    // --- General functions: func{arg} → func(arg)
    expr = Regex.Replace(expr, @"(sin|cos|tan|ln|log|sqrt)\{(.*?)\}", "$1($2)");

    // --- 🆕 INTEGRAL: \int_{a}^{b}{f(x)}dx → integrate(f(x), x, a, b)
    expr = Regex.Replace(expr, @"\\int_\{(.*?)\}\^\{(.*?)\}\{(.*?)\}\\?d([a-zA-Z])", "integrate($3, $4, $1, $2)");

    // --- 🆕 INDEFINITE: \int{f(x)}dx → integrate(f(x), x)
    expr = Regex.Replace(expr, @"\\int\{(.*?)\}\\?d([a-zA-Z])", "integrate($1, $2)");

    // --- Operators and cleanup ---
    expr = expr.Replace("÷", "/").Replace("×", "*").Replace("−", "-");
    expr = expr.Replace(@"\left", "").Replace(@"\right", "");
    expr = expr.Replace("{", "(").Replace("}", ")");
    expr = expr.Replace(" ", "");
            // Convert indefinite integral: \int_ body → int(body, x)
        // Convert \int_ body → int(body, x, 0, x) to simulate indefinite integral
    expr = Regex.Replace(expr, @"\\int_\s*(.+)", "int($1, x, 0, x)");


    return expr;
}





   public void SetLatexExpression(string latex)
{
    if (string.IsNullOrWhiteSpace(latex))
    {
        ClearGraph();
        return;
    }

    latexExpression = latex;
    string exprStr = PreprocessLatex(latexExpression);

    // NEW: re-build the mxparser Expression for this new formula
    xArg       = new Argument("x = 0");
    expression = new Expression(exprStr, xArg);

    if (!expression.checkSyntax())
    {
        Debug.LogError("❌ Invalid syntax: " + expression.getErrorMessage());
        lineRenderer.positionCount = 0;
        return;
    }

    DrawFunction(steps);
}

public void ClearGraph()
{
    Debug.Log($"🧽 ClearGraph() called. Previous latexExpression: {latexExpression}");
    // Wipe your expression so no new draw happens
    latexExpression = "";
    expression     = null;
    xArg           = null;

    if (lineRenderer != null)
    {
        // 1) Zero the count so nothing renders
        lineRenderer.positionCount = 0;

        // 2) Also clear out the position array itself
        lineRenderer.SetPositions(Array.Empty<Vector3>());
    }
}


    internal void Render()
    {
        throw new NotImplementedException();
    }
}
