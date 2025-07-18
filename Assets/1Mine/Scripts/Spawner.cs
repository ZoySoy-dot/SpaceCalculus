using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Prefabs to spawn")]
    public GameObject blackHolePrefab;
    public GameObject playerPrefab;
    public GameObject goalPrefab;

    [Header("Spawn Settings")]
    public int blackHoleCount = 5;
    [Range(0f, 0.5f)] public float viewportMargin = 0.1f;
    public float minSpawnDistance = 2f;
    public float minGoalDistance = 5f;
    public float pathWidth = 4f; // How wide the area between player and goal should be
    
    [Header("References")]
    [SerializeField] public GameObject mapObject;
    [SerializeField] public GameObject playerObject;
    
    void Start()
    {
        var cam = Camera.main;
        float zOffset = Mathf.Abs(cam.transform.position.z);
        var spawnPositions = new List<Vector3>();

        // 1) Move existing player to the left side
        Vector3 playerPos = GetPlayerPosition(cam, zOffset, viewportMargin);
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = playerPos;
            Debug.Log($"Moved existing player to {playerPos}");
        }
        else
        {
            // Fallback: spawn new player if none exists
            player = Instantiate(playerPrefab, playerPos, Quaternion.identity);
            player.name = "Player";
            player.tag = "Player";
            Debug.Log($"No existing player found, spawned new one at {playerPos}");
        }
        spawnPositions.Add(playerPos);

        // Set player in SaveManager and save spawn point
        PlayerSaveLocation saveManager = FindFirstObjectByType<PlayerSaveLocation>();
        if (saveManager != null)
        {
            saveManager.playerTransform = player.transform;
            saveManager.spawnPoint = playerPos; // Set spawn point to new position
            saveManager.SaveLocation(); // Save the new spawn point
        }

        // 2) Spawn goal on the right side
        Vector3 goalPos = GetGoalPosition(cam, zOffset, viewportMargin, playerPos);
        var goal = Instantiate(goalPrefab, goalPos, Quaternion.identity, transform);
        goal.name = "Goal";
        goal.tag = "Goal";
        spawnPositions.Add(goalPos);

        // 3) Spawn black holes evenly distributed between player and goal
        SpawnBlackHolesBetween(playerPos, goalPos, spawnPositions);

        if (mapObject != null)
            mapObject.SetActive(false);
        if (playerObject != null)
            Destroy(playerObject);
    }

    private Vector3 GetPlayerPosition(Camera cam, float zOffset, float margin)
    {
        // Place player randomly in left portion of screen
        float vx = Random.Range(margin, 0.3f);
        float vy = Random.Range(margin, 1f - margin);
        Vector3 worldPos = cam.ViewportToWorldPoint(new Vector3(vx, vy, zOffset));
        worldPos.z = 0f;
        return worldPos;
    }

    private Vector3 GetGoalPosition(Camera cam, float zOffset, float margin, Vector3 playerPos)
    {
        // Place goal in right portion of screen, ensuring minimum distance from player
        Vector3 goalPos;
        int tries = 0;
        
        do
        {
            float vx = Random.Range(0.7f, 1f - margin);
            float vy = Random.Range(margin, 1f - margin);
            goalPos = cam.ViewportToWorldPoint(new Vector3(vx, vy, zOffset));
            goalPos.z = 0f;
            tries++;
            
            if (tries > 100)
            {
                Debug.LogWarning("Spawner: Couldn't find valid goal position after 100 tries.");
                break;
            }
        }
        while (Vector3.Distance(playerPos, goalPos) < minGoalDistance);
        
        return goalPos;
    }

    private void SpawnBlackHolesBetween(Vector3 playerPos, Vector3 goalPos, List<Vector3> spawnPositions)
    {
        // Calculate the rectangular area between player and goal
        Vector3 direction = (goalPos - playerPos).normalized;
        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f);
        
        // Create evenly spaced positions along the path
        for (int i = 0; i < blackHoleCount; i++)
        {
            // Progress along the path from player to goal (skip very beginning and end)
            float progress = (float)(i + 1) / (blackHoleCount + 1);
            Vector3 basePos = Vector3.Lerp(playerPos, goalPos, progress);
            
            // Add random offset perpendicular to the path
            float perpendicularOffset = Random.Range(-pathWidth * 0.5f, pathWidth * 0.5f);
            Vector3 finalPos = basePos + perpendicular * perpendicularOffset;
            
            // Add some random variation along the path direction too
            float pathVariation = Random.Range(-1f, 1f);
            finalPos += direction * pathVariation;
            
            // Make sure it's not too close to existing objects
            if (IsTooCloseToExisting(finalPos, spawnPositions, minSpawnDistance))
            {
                // Try a few more positions if the first one is too close
                for (int retry = 0; retry < 5; retry++)
                {
                    perpendicularOffset = Random.Range(-pathWidth * 0.5f, pathWidth * 0.5f);
                    pathVariation = Random.Range(-1f, 1f);
                    finalPos = basePos + perpendicular * perpendicularOffset + direction * pathVariation;
                    
                    if (!IsTooCloseToExisting(finalPos, spawnPositions, minSpawnDistance))
                        break;
                }
            }
            
            // Spawn the black hole
            var blackHole = Instantiate(blackHolePrefab, finalPos, Quaternion.identity, transform);
            blackHole.name = $"BlackHole_{i}";
            spawnPositions.Add(finalPos);
        }
    }

    private bool IsTooCloseToExisting(Vector3 position, List<Vector3> existing, float minDistance)
    {
        return existing.Any(p => Vector3.Distance(p, position) < minDistance);
    }
}