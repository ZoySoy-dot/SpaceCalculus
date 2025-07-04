using UnityEngine;
using System.Collections;

public class LineRendererCircle : MonoBehaviour
{
    public LineRenderer circleRenderer;
    public int steps = 100;
    public float radius = 1f;

    public float maxRadius = 5f;
    public float pulseSpeed = 1f;
    public bool isPulsing = true;
    public bool reversePulse = false; // If true, the pulse will shrink instead of grow
    private Coroutine pulseRoutine;

    void Start()
    {
        if (circleRenderer == null)
        {
            circleRenderer = GetComponent<LineRenderer>();
        }

        DrawCircle(steps, radius);

        if (isPulsing)
        {
            pulseRoutine = StartCoroutine(PulseCoroutine());
        }
    }

    void DrawCircle(int steps, float radius)
    {
        circleRenderer.positionCount = steps + 1; // +1 to close the loop

        for (int i = 0; i < steps; i++)
        {
            float angle = (float)i / steps * 2 * Mathf.PI;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;

            circleRenderer.SetPosition(i, new Vector3(x, y, 0));
        }

        // Close the loop
        circleRenderer.SetPosition(steps, circleRenderer.GetPosition(0));
    }

    IEnumerator PulseCoroutine()
    {
        float currentRadius = reversePulse ? maxRadius : radius;

        while (true)
        {
            if (reversePulse)
            {
                currentRadius -= pulseSpeed * Time.deltaTime;

                if (currentRadius <= radius)
                {
                    currentRadius = maxRadius; // Reset to max when it shrinks too small
                }
            }
            else
            {
                currentRadius += pulseSpeed * Time.deltaTime;

                if (currentRadius >= maxRadius)
                {
                    currentRadius = radius; // Reset to base radius when it grows too large
                }
            }

            DrawCircle(steps, currentRadius);
            yield return null;
        }
    }

}
