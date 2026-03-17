using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach this to your Main Camera.
/// Click anywhere in Play mode and check the Console.
/// Delete this script after testing.
/// </summary>
public class ClickTest : MonoBehaviour
{
    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            Vector3 mousePos = mouse.position.ReadValue();
            mousePos.z = Mathf.Abs(Camera.main.transform.position.z);
            Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(mousePos);

            Debug.Log($"CLICK at world position: {mouseWorld}");

            RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);

            if (hit.collider != null)
            {
                Debug.Log($"HIT: {hit.collider.gameObject.name} at {hit.point}");
            }
            else
            {
                Debug.Log("HIT NOTHING - no collider at click position");
            }
        }
    }
}