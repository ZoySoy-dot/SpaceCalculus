using UnityEngine;
using TMPro;

public class CalculatorButton : MonoBehaviour
{
    [Header("Assign the TextMeshProUGUI field here")]
    public TextMeshProUGUI displayText;

    [Header("Value to insert when this button is pressed")]
    public string valueToInsert;

    // Optional shared reference to current input (if you want to sync across buttons)
    private static string currentInput = "";

    public void InsertValue()
    {
        currentInput += valueToInsert;
        if (displayText != null)
        {
            displayText.text = currentInput;
        }
    }

    public void ClearDisplay()
    {
        currentInput = "";
        if (displayText != null)
        {
            displayText.text = "";
        }
    }
}
