using UnityEngine;

public sealed class RecolorPickedObject : MonoBehaviour
{
    /// <summary>
    /// The picking provider used to pick objects to recolor.
    /// </summary>
    [SerializeField, Tooltip("The picking provider to use for picking objects on screen.")]
    private PickingProvider picker;

    private Pickable lastPicked;
    private Color originalColor;

    private void Update()
    {
        if (picker == null)
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        Vector3 screenPosition = Input.mousePosition;
        Pickable picked = picker.GetPickableAt(screenPosition);

        if (picked == lastPicked)
            return;

        if (lastPicked != null)
        {
            ColorProperty colorProperty = lastPicked.GetComponent<ColorProperty>();
            if (colorProperty != null)
            {
                colorProperty.Color = originalColor;
            }
        }

        lastPicked = picked;

        if (lastPicked != null)
        {
            ColorProperty colorProperty = lastPicked.GetComponent<ColorProperty>();
            if (colorProperty != null)
            {
                originalColor = colorProperty.Color;
                colorProperty.Color = Color.green;
            }
        }
    }
}