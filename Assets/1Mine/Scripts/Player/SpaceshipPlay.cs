using TexDrawLib;
using TMPro;
using UnityEngine;

public class SpaceshipPlay : MonoBehaviour
{
    public LineRenderer trajectoryLine;
    public Transform playerTransform;
    public float speed = 5f;
    public bool isPlaying = false;
    public TextMeshProUGUI statusText;
    public TEXDraw latex;                // Current TEXDraw, assigned via Inspector
    private PlayerSaveLocation saveLocation; // ✅ will be auto-found in Start()
    private SimpleCalculator calculator;  // ✅ will be auto-found in Start()
    private int currentPointIndex = 0;
    private Vector3[] points;
    public Transform historyParent; // Assign in Inspector to "History" object
    public TextMeshProUGUI functionCountText;
    private int functionCount = 0;
    private GraphingLine graphingLine; // Reference to the GraphingLine component
    void Start()
    {
        calculator = FindObjectOfType<SimpleCalculator>();
        saveLocation = FindObjectOfType<PlayerSaveLocation>();
        RefreshLinePoints();

        if (playerTransform == null)
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            if (players.Length >= 2)
            {
                playerTransform = players[1].transform;
                Debug.Log("🎮 Using second Player in scene: " + playerTransform.name);
            }
            else if (players.Length == 1)
            {
                playerTransform = players[0].transform;
                Debug.LogWarning("⚠️ Only one Player found. Using first.");
            }
            else
            {
                Debug.LogError("❌ No Player object with tag 'Player' found.");
                return;
            }
        }
        if (points != null && points.Length > 0)
            playerTransform.position = points[0];


        if (functionCountText != null)
            functionCountText.text = "0";
            if (points != null && points.Length > 0)
        playerTransform.position = points[0];

    // **NEW**: record that as the “spawn” point
    if (saveLocation != null)
        saveLocation.SaveLocation();

}

void RefreshLinePoints()
{
    if (trajectoryLine == null)
    {
        Debug.LogWarning("Trajectory LineRenderer is not assigned or missing!");
        points = new Vector3[0];
        isPlaying = false;
        return;
    }

    int count = trajectoryLine.positionCount;
    if (count == 0)
    {
        // no curve → stop the ship
        points = new Vector3[0];
        isPlaying = false;
        return;
    }

    // otherwise pull in the new trajectory
    points = new Vector3[count];
    trajectoryLine.GetPositions(points);
    currentPointIndex = 0;

    if (playerTransform != null)
        playerTransform.position = points[0];
}

    void Update()
    {
        if (!isPlaying || points == null || points.Length == 0)
                return;

        Vector3 target = points[currentPointIndex];
        float step = speed * Time.deltaTime;
        playerTransform.position = Vector3.MoveTowards(playerTransform.position, target, step);

        if (Vector3.Distance(playerTransform.position, target) < 0.01f)
        {
            currentPointIndex++;
        }
    }

public void TogglePlayPause()
{
    // disallow playing if there's no trajectory
    if (!isPlaying && (trajectoryLine == null || trajectoryLine.positionCount == 0))
        return;

    isPlaying = !isPlaying;
    statusText.text = isPlaying ? "||" : "▶";
    if (isPlaying) RefreshLinePoints();
}


    public void AddNewFunction()
    {
        Debug.Log("AddNewFunction");

        if (latex == null || calculator == null || historyParent == null || functionCountText == null)
        {
            Debug.LogWarning("Missing references for latex, calculator, historyParent, or functionCountText!");
            return;
        }

        // ✅ Clone current TEXDraw
        GameObject oldLatex = Instantiate(latex.gameObject, historyParent);
        oldLatex.name = "OldFunction";

        oldLatex.transform.localScale = Vector3.one;
        oldLatex.transform.localPosition = Vector3.zero;

        oldLatex.transform.localPosition += new Vector3(0, -50f * historyParent.childCount, 0);

        // ✅ Clear input
        calculator.OnClearPressed();
        saveLocation.SaveLocation();

        // ✅ Increment counter and update UI
        functionCount++;
        functionCountText.text = functionCount.ToString();
    }
public void Restart()
{
    Debug.Log("Restarting spaceship play...");

    isPlaying = false;
    currentPointIndex = 0;
    
    // ✅ Stop any movement by pausing the spaceship
    var rb = GetComponent<Rigidbody2D>();
    if (rb != null) rb.linearVelocity = Vector2.zero;
    

    // ✅ Move back to saved spawn
    if (saveLocation != null)
    {
        saveLocation.ResetToSpawn();
        // Ensure player is exactly at spawn point
        playerTransform.position = saveLocation.spawnPoint;
    }

    // ✅ Reset function counter and UI
    functionCount = 0;
    if (functionCountText != null)
        functionCountText.text = "0";

    // ✅ Clear graph line and data
    points = new Vector3[0]; // Clear internal array
        if (trajectoryLine != null)
        {
            trajectoryLine.positionCount = 0;
            trajectoryLine.SetPositions(new Vector3[0]); // Clear visually
            
    }

    // ✅ Clear previous function history
    if (historyParent != null)
    {
        foreach (Transform child in historyParent)
        {
            Destroy(child.gameObject);
        }
    }

    calculator.OnClearPressed();
    
    // ✅ Clear the trajectory completely - don't refresh from graphing line
    points = new Vector3[0];

    Debug.Log("Reset complete.");
}

public void Reset()
{
    // ✅ Pause the spaceship and clear momentum
    isPlaying = false;
    var rb = GetComponent<Rigidbody2D>();
    if (rb != null) rb.linearVelocity = Vector2.zero;
    
    Debug.Log("Reset: Paused spaceship and cleared momentum");
}




}
