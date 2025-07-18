using UnityEngine;

[System.Serializable]
public class Hole : MonoBehaviour {
    public float strength = 0f; // Positive = black hole (pull), Negative = white hole (push)
    public float influenceRadius = 3f;
}