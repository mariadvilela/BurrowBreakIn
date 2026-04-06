using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to each LVLOne item (SpriteRenderer in world space).
/// Requires a Collider2D on the same GameObject for mouse detection.
/// Uses Unity's new Input System.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DraggableItem : MonoBehaviour
{
    public enum ItemShape
    {
        /// <summary> Image 1 - Small egg cluster: X (1 cell) </summary>
        SingleCell,

        /// <summary> Image 2 - Chicken legs: X . / X X (3 cells, L-shape) </summary>
        LShape,

        /// <summary> Image 3 - Wing pieces: X X X / . X . (4 cells, T-shape) </summary>
        TShape,

        /// <summary> Image 4 - Whole chicken: X X / X X (4 cells, square) </summary>
        Square,

        /// <summary> Image 5 - Spread wings: . . X / X X X (4 cells, J-shape) </summary>
        JShape,

        /// <summary> Image 6 - Large egg cluster: . X / X X / . X (4 cells, plus-shape) </summary>
        PlusShape,

        /// <summary> Image 7 - Thigh pieces: . X X / X X . (4 cells, S-shape) </summary>
        SShape,

        /// <summary> ZShape: X . / X X / . X (4 cells) </summary>
        ZShape,

        /// <summary> Horizontal3: X X X (3 cells) </summary>
        Horizontal3,

        /// <summary> Horizontal4: X X X X (4 cells) </summary>
        Horizontal4,

        /// <summary> BigLShape: X . . / X X X (4 cells) </summary>
        BigLShape,

        /// <summary> Vertical2: X / X (2 cells) </summary>
        Vertical2,

        /// <summary> Horizontal2: X X (2 cells) </summary>
        Horizontal2,

        /// <summary> TallRect: X X / X X / X X (6 cells) </summary>
        TallRect,

        /// <summary> WideRect: X X X / X X X (6 cells) </summary>
        WideRect,

        /// <summary> Vertical4: X / X / X / X (4 cells) </summary>
        Vertical4
    }

    [Header("References")]
    public SackGrid sackGrid;

    [Header("Item Shape")]
    public ItemShape shapeType = ItemShape.SingleCell;

    [Header("Snap Settings")]
    public bool scaleToFitShape = false;

    [Range(0f, 0.4f)]
    public float cellPadding = 0.05f;

    [Header("Animation")]
    public float snapAnimSpeed = 12f;
    public float scaleAnimSpeed = 10f;

    [Header("Sorting")]
    public int dragSortingBoost = 100;

    // Cached shape offsets
    private Vector2Int[] shapeOffsets;

    // State
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private int originalSortingOrder;
    private SpriteRenderer spriteRenderer;

    private bool isDragging = false;
    private Vector3 dragOffset;
    private Camera mainCam;

    private bool isSnapped = false;
    private int snappedAnchorX = -1;
    private int snappedAnchorY = -1;

    private bool animating = false;
    private Vector3 animTargetPos;
    private Vector3 animTargetScale;

    // Input
    private Mouse mouse;

    public static Vector2Int[] GetShapeOffsets(ItemShape shape)
    {
        switch (shape)
        {
            case ItemShape.SingleCell:
                return new Vector2Int[] {
                    new Vector2Int(0, 0)
                };

            case ItemShape.LShape:
                // X .
                // X X
                return new Vector2Int[] {
                    new Vector2Int(0, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 0)
                };

            case ItemShape.TShape:
                // X X X
                // . X .
                return new Vector2Int[] {
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1),
                    new Vector2Int(2, 1),
                    new Vector2Int(1, 0)
                };

            case ItemShape.Square:
                // X X
                // X X
                return new Vector2Int[] {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1)
                };

            case ItemShape.JShape:
                // . . X
                // X X X
                return new Vector2Int[] {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(2, 0),
                    new Vector2Int(2, 1)
                };

            case ItemShape.PlusShape:
                // . X
                // X X
                // . X
                return new Vector2Int[] {
                    new Vector2Int(1, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1),
                    new Vector2Int(1, 2)
                };

            case ItemShape.SShape:
                // . X X
                // X X .
                return new Vector2Int[] {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(1, 1),
                    new Vector2Int(2, 1)
                };

            case ItemShape.ZShape:
                // X .
                // X X
                // . X
                return new Vector2Int[] {
                    new Vector2Int(0, 1),
                    new Vector2Int(0, 2),
                    new Vector2Int(1, 0),
                    new Vector2Int(1, 1)
                };

            case ItemShape.Horizontal3:
                // X X X
                return new Vector2Int[] {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(2, 0)
                };

            case ItemShape.Horizontal4:
                // X X X X
                return new Vector2Int[] {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(2, 0),
                    new Vector2Int(3, 0)
                };

            case ItemShape.BigLShape:
                // X . .
                // X X X
                return new Vector2Int[] {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(2, 0),
                    new Vector2Int(0, 1)
                };

            case ItemShape.Vertical2:
                // X
                // X
                return new Vector2Int[] {
                    new Vector2Int(0, 0),
                    new Vector2Int(0, 1)
                };

            case ItemShape.Horizontal2:
                // X X
                return new Vector2Int[] {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0)
                };

            case ItemShape.TallRect:
                // X X
                // X X
                // X X
                return new Vector2Int[] {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1),
                    new Vector2Int(0, 2),
                    new Vector2Int(1, 2)
                };

            case ItemShape.WideRect:
                // X X X
                // X X X
                return new Vector2Int[] {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(2, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1),
                    new Vector2Int(2, 1)
                };

            case ItemShape.Vertical4:
                // X
                // X
                // X
                // X
                return new Vector2Int[] {
                    new Vector2Int(0, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(0, 2),
                    new Vector2Int(0, 3)
                };

            default:
                return new Vector2Int[] { new Vector2Int(0, 0) };
        }
    }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCam = Camera.main;
        mouse = Mouse.current;

        if (sackGrid == null)
            sackGrid = FindFirstObjectByType<SackGrid>();

        originalPosition = transform.position;
        originalScale = transform.localScale;
        if (spriteRenderer != null)
            originalSortingOrder = spriteRenderer.sortingOrder;

        shapeOffsets = GetShapeOffsets(shapeType);
    }

    public Vector2Int[] GetOffsets() => shapeOffsets;

    public int GetCellCount() => shapeOffsets.Length;

    private Vector2 GetMouseWorldPos()
    {
        Vector3 mousePos = mouse.position.ReadValue();
        mousePos.z = Mathf.Abs(mainCam.transform.position.z);
        return mainCam.ScreenToWorldPoint(mousePos);
    }

    void Update()
    {
        if (mouse == null) mouse = Mouse.current;
        if (mouse == null) return;

        // Handle click down
        if (mouse.leftButton.wasPressedThisFrame && !isDragging)
        {
            Vector2 mouseWorld = GetMouseWorldPos();
            RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);

            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                Vector3 worldPos3 = new Vector3(mouseWorld.x, mouseWorld.y, transform.position.z);
                dragOffset = transform.position - worldPos3;

                isDragging = true;
                animating = false;

                if (spriteRenderer != null)
                    spriteRenderer.sortingOrder = originalSortingOrder + dragSortingBoost;

                if (isSnapped)
                {
                    sackGrid.FreeItem(gameObject);
                    isSnapped = false;
                    snappedAnchorX = -1;
                    snappedAnchorY = -1;
                    transform.localScale = originalScale;
                }
            }
        }

        // Handle dragging
        if (isDragging && mouse.leftButton.isPressed)
        {
            Vector2 mouseWorld = GetMouseWorldPos();
            transform.position = new Vector3(
                mouseWorld.x + dragOffset.x,
                mouseWorld.y + dragOffset.y,
                transform.position.z
            );
        }

        // Handle release
        if (isDragging && mouse.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;

            if (spriteRenderer != null)
                spriteRenderer.sortingOrder = originalSortingOrder;

            Vector2 worldPos = transform.position;
            Vector2 snapPos;
            int ax, ay;

            if (sackGrid.IsInsideGrid(worldPos) &&
                sackGrid.TryGetBestPlacement(worldPos, shapeOffsets, out snapPos, out ax, out ay))
            {
                sackGrid.OccupyShape(ax, ay, shapeOffsets, gameObject);
                isSnapped = true;
                snappedAnchorX = ax;
                snappedAnchorY = ay;

                Vector3 targetScale;
                Vector2 targetPos;

                if (scaleToFitShape)
                {
                    Vector2 boundsCenter, boundsSize;
                    sackGrid.GetShapeWorldBounds(ax, ay, shapeOffsets,
                        out boundsCenter, out boundsSize);

                    Vector2 usable = boundsSize * (1f - cellPadding * 2f);

                    Vector2 spriteWorldSize = Vector2.one;
                    if (spriteRenderer != null && spriteRenderer.sprite != null)
                    {
                        Bounds spriteBounds = spriteRenderer.sprite.bounds;
                        spriteWorldSize = new Vector2(
                            spriteBounds.size.x * originalScale.x,
                            spriteBounds.size.y * originalScale.y
                        );
                    }

                    float sx = usable.x / spriteWorldSize.x;
                    float sy = usable.y / spriteWorldSize.y;
                    float uniform = Mathf.Min(sx, sy);

                    targetScale = originalScale * uniform;
                    targetPos = new Vector3(boundsCenter.x, boundsCenter.y, transform.position.z);
                }
                else
                {
                    targetScale = originalScale;
                    targetPos = new Vector3(snapPos.x, snapPos.y, transform.position.z);
                }

                animTargetPos = targetPos;
                animTargetScale = targetScale;
                animating = true;
            }
            else
            {
                animTargetPos = originalPosition;
                animTargetScale = originalScale;
                animating = true;
            }
        }

        // Handle animation
        if (animating)
        {
            transform.position = Vector3.Lerp(transform.position, animTargetPos, Time.deltaTime * snapAnimSpeed);
            transform.localScale = Vector3.Lerp(transform.localScale, animTargetScale, Time.deltaTime * scaleAnimSpeed);

            float posDist = Vector3.Distance(transform.position, animTargetPos);
            float scaleDist = Vector3.Distance(transform.localScale, animTargetScale);

            if (posDist < 0.01f && scaleDist < 0.001f)
            {
                transform.position = animTargetPos;
                transform.localScale = animTargetScale;
                animating = false;
            }
        }
    }

    public void ResetItem()
    {
        if (isSnapped)
        {
            sackGrid.FreeItem(gameObject);
            isSnapped = false;
            snappedAnchorX = -1;
            snappedAnchorY = -1;
        }

        animating = false;
        isDragging = false;
        transform.position = originalPosition;
        transform.localScale = originalScale;
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = originalSortingOrder;
    }
}