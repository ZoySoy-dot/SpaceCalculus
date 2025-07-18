using UnityEngine;
using System.Collections.Generic;

public class PlayerSaveLocation : MonoBehaviour
{
    [Tooltip("Auto-finds the active GameObject tagged 'Player' in the scene if left empty.")]
    public Transform playerTransform;

    [Header("Auto-Save Settings")]
    public float autoSaveInterval = 2f; // Save every 2 seconds
    public float minMovementToSave = 0.5f; // Minimum distance moved to trigger save
    
    public Vector3 spawnPoint;
    private Vector3 lastSavedPosition;
    private List<Vector3> saveHistory = new List<Vector3>();
    private float lastSaveTime = 0f;

    void Start()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("⛔ Player transform not yet assigned! Waiting for Spawner to assign...");
            // Try to find player in scene
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                Debug.Log("✅ Found player automatically!");
            }
        }
        
        if (playerTransform != null)
        {
            InitializePlayerSave();
        }
    }

    void Update()
    {
        // If player wasn't found at start, keep trying
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                Debug.Log("✅ Found player automatically!");
                InitializePlayerSave();
            }
            return;
        }

        // Auto-save disabled - only save when AddNewFunction is called
    }

    private void InitializePlayerSave()
    {
        if (playerTransform == null) return;
        
        spawnPoint = playerTransform.position;
        lastSavedPosition = spawnPoint;
        saveHistory.Add(spawnPoint);
        lastSaveTime = Time.time;
        Debug.Log($"✅ Player save system initialized at {spawnPoint}");
    }

    private void CheckAndSaveLocation()
    {
        if (playerTransform == null) return;
        
        Vector3 currentPos = playerTransform.position;
        float distanceMoved = Vector3.Distance(currentPos, lastSavedPosition);
        
        // Only save if player has moved significantly
        if (distanceMoved > minMovementToSave)
        {
            SaveLocation();
        }
        
        lastSaveTime = Time.time;
    }


    public void SaveLocation()
    {
        if (playerTransform == null)
        {
            Debug.LogError("SaveLocation(): playerTransform is null!");
            return;
        }

        lastSavedPosition = playerTransform.position;
        saveHistory.Add(lastSavedPosition);
        Debug.Log($"💾 Auto-saved player position: {lastSavedPosition}");
    }

    public void ForceSaveLocation()
    {
        if (playerTransform == null)
        {
            Debug.LogError("ForceSaveLocation(): playerTransform is null!");
            return;
        }

        lastSavedPosition = playerTransform.position;
        saveHistory.Add(lastSavedPosition);
        Debug.Log($"✅ Manually saved location: {lastSavedPosition}");
    }

    public void LoadLastSavedLocation()
    {
        if (playerTransform == null)
        {
            Debug.LogError("LoadLastSavedLocation(): playerTransform is null!");
            return;
        }
        if (saveHistory.Count == 0)
        {
            Debug.LogWarning("⚠️ No saved locations found. Using spawn point instead.");
            playerTransform.position = spawnPoint;
            return;
        }
        playerTransform.position = lastSavedPosition;
        Debug.Log($"🔁 Loaded Last Saved Location: {lastSavedPosition}");
        
    }

    public void ResetToSpawn()
    {
        if (playerTransform != null)
        {
            playerTransform.position = spawnPoint;
            Debug.Log($"🔄 Player reset to spawn point: {spawnPoint}");
        }
        else
        {
            Debug.LogError("ResetToSpawn(): playerTransform is null!");
        }
    }

    public void PrintSaveHistory()
    {
        Debug.Log("📜 Save History:");
        for (int i = 0; i < saveHistory.Count; i++)
            Debug.Log($"  [{i}] {saveHistory[i]}");
    }
}
