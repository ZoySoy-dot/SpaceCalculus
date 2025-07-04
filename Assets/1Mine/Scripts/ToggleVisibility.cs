using UnityEngine;

public class ToggleVisibility : MonoBehaviour
{
    // Assign this in the Inspector or leave null to use this object's parent
    public GameObject targetObject;

    void Start()
    {
        // If no target is assigned, default to the parent
        if (targetObject == null && transform.parent != null)
        {
            targetObject = transform.parent.gameObject;
        }
    }

    // Call this to hide the object
    public void Hide()
    {
        if (targetObject != null)
        {
            targetObject.SetActive(false);
        }
    }

    // Call this to unhide the object
    public void Show()
    {
        if (targetObject != null)
        {
            targetObject.SetActive(true);
        }
    }

    // Example toggle function
    public void Toggle()
    {
        if (targetObject != null)
        {
            targetObject.SetActive(!targetObject.activeSelf);
        }
    }
}
