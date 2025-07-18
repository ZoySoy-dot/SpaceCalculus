using UnityEngine;
using System.Linq;

public class Blackholes : MonoBehaviour
{
    // Remove the cached field entirely

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log($"Blackhole triggered by {other.name} (tag={other.tag})");

        // Find all SpaceshipPlay components in the scene
        var ships = FindObjectsByType<SpaceshipPlay>(FindObjectsSortMode.None);
        if (ships.Length >= 2)
        {
            Debug.Log("Calling Restart() on the SECOND SpaceshipPlay instance.");
            ships[1].Restart();
        }
        else if (ships.Length == 1)
        {
            Debug.Log("Only one SpaceshipPlay found; calling Restart() on it.");
            ships[0].Restart();
        }
        else
        {
            Debug.LogError("No SpaceshipPlay found to restart!");
        }
    }
}
