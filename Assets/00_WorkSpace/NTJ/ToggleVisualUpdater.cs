using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleVisualUpdater : MonoBehaviour
{
    public Toggle toggle;
    public Image targetImage;
    public Color onColor = Color.white;
    public Color offColor = Color.grey;

    void Start()
    {
        if (toggle == null || targetImage == null)
        {
            Debug.LogError("Toggle or Target Image not assigned!");
            return;
        }
        // Add a listener to handle color changes whenever the toggle state changes
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
        // Set the initial color
        OnToggleValueChanged(toggle.isOn);
    }

    private void OnToggleValueChanged(bool isOn)
    {
        targetImage.color = isOn ? onColor : offColor;
    }
}

