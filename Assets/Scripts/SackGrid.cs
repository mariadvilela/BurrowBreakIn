using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the invisible 4x6 grid inside the sack.
/// Works in world space with SpriteRenderers.
/// Attach this to the Sack GameObject (which should have a SpriteRenderer).
/// </summary>
public class SackGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    public int columns = 6;
    public int rows = 4;

    [Header("Grid Area")]
    [Tooltip("Offset from the sack's center in world units.")]
    public Vector2 gridOffset = Vector2.zero;
    [Tooltip("Total size of the grid area in world units. Adjust to fit inside the sack.")]
    public Vector2 gridWorldSize = new Vector2(6f, 4f);

    [Header("Debug")]
    public bool showDebugGrid = true;
    public Color debugColor = Color.green;
    public Color debugOccupiedColor = Color.red;

    private GameObject[,] cellOccupants;
    private Vector2[,] cellCenters;
    private Vector2 cellSize;

    void Awake()
    {
        BuildGrid();
    }

    public void BuildGrid()
    {
        cellOccupants = new GameObject[columns, rows];
        cellCenters = new Vector2[columns, rows];

        Vector2 sackCenter = (Vector2)transform.position + gridOffset;

        cellSize = new Vector2(
            gridWorldSize.x / columns,
            gridWorldSize.y / rows
        );

        Vector2 gridBottomLeft = sackCenter - gridWorldSize * 0.5f;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                cellCenters[x, y] = new Vector2(
                    gridBottomLeft.x + (x + 0.5f) * cellSize.x,
                    gridBottomLeft.y + (y + 0.5f) * cellSize.y
                );
                cellOccupants[x, y] = null;
            }
        }
    }

    public Vector2 GetCellSize() => cellSize;

    public Vector2 GetCellCenter(int x, int y) => cellCenters[x, y];

    /// <summary>
    /// Check if a shape can be placed at the given anchor position.
    /// </summary>
    public bool CanPlace(int anchorX, int anchorY, Vector2Int[] shapeOffsets)
    {
        foreach (var off in shapeOffsets)
        {
            int cx = anchorX + off.x;
            int cy = anchorY + off.y;

            if (cx < 0 || cx >= columns || cy < 0 || cy >= rows)
                return false;
            if (cellOccupants[cx, cy] != null)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Find the best anchor position for a shape near a world position.
    /// </summary>
    public bool TryGetBestPlacement(Vector2 worldPos, Vector2Int[] shapeOffsets,
        out Vector2 snapPos, out int bestAnchorX, out int bestAnchorY)
    {
        snapPos = Vector2.zero;
        bestAnchorX = -1;
        bestAnchorY = -1;
        float bestDist = float.MaxValue;

        int minOx = int.MaxValue, maxOx = int.MinValue;
        int minOy = int.MaxValue, maxOy = int.MinValue;
        foreach (var off in shapeOffsets)
        {
            if (off.x < minOx) minOx = off.x;
            if (off.x > maxOx) maxOx = off.x;
            if (off.y < minOy) minOy = off.y;
            if (off.y > maxOy) maxOy = off.y;
        }

        for (int ax = -minOx; ax <= columns - 1 - maxOx; ax++)
        {
            for (int ay = -minOy; ay <= rows - 1 - maxOy; ay++)
            {
                if (!CanPlace(ax, ay, shapeOffsets)) continue;

                Vector2 center = GetShapeWorldCenter(ax, ay, shapeOffsets);
                float dist = Vector2.Distance(worldPos, center);

                if (dist < bestDist)
                {
                    bestDist = dist;
                    snapPos = center;
                    bestAnchorX = ax;
                    bestAnchorY = ay;
                }
            }
        }

        return bestAnchorX >= 0;
    }

    /// <summary>
    /// Get the world-space center (average) of all cells in a placed shape.
    /// </summary>
    public Vector2 GetShapeWorldCenter(int anchorX, int anchorY, Vector2Int[] shapeOffsets)
    {
        Vector2 sum = Vector2.zero;
        foreach (var off in shapeOffsets)
            sum += cellCenters[anchorX + off.x, anchorY + off.y];
        return sum / shapeOffsets.Length;
    }

    /// <summary>
    /// Get the world-space bounding box of a placed shape.
    /// </summary>
    public void GetShapeWorldBounds(int anchorX, int anchorY, Vector2Int[] shapeOffsets,
        out Vector2 boundsCenter, out Vector2 boundsSize)
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var off in shapeOffsets)
        {
            Vector2 c = cellCenters[anchorX + off.x, anchorY + off.y];
            float halfW = cellSize.x * 0.5f;
            float halfH = cellSize.y * 0.5f;

            if (c.x - halfW < minX) minX = c.x - halfW;
            if (c.x + halfW > maxX) maxX = c.x + halfW;
            if (c.y - halfH < minY) minY = c.y - halfH;
            if (c.y + halfH > maxY) maxY = c.y + halfH;
        }

        boundsSize = new Vector2(maxX - minX, maxY - minY);
        boundsCenter = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
    }

    /// <summary>
    /// Mark all cells of a shape as occupied.
    /// </summary>
    public void OccupyShape(int anchorX, int anchorY, Vector2Int[] shapeOffsets, GameObject item)
    {
        foreach (var off in shapeOffsets)
            cellOccupants[anchorX + off.x, anchorY + off.y] = item;
    }

    /// <summary>
    /// Free all cells occupied by a specific item.
    /// </summary>
    public void FreeItem(GameObject item)
    {
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
                if (cellOccupants[x, y] == item)
                    cellOccupants[x, y] = null;
    }

    public bool IsInsideGrid(Vector2 worldPos)
    {
        if (cellCenters == null) return false;

        Vector2 min = cellCenters[0, 0] - cellSize * 0.5f;
        Vector2 max = cellCenters[columns - 1, rows - 1] + cellSize * 0.5f;

        return worldPos.x >= min.x && worldPos.x <= max.x &&
               worldPos.y >= min.y && worldPos.y <= max.y;
    }

    public int OccupiedCellCount()
    {
        int count = 0;
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
                if (cellOccupants[x, y] != null) count++;
        return count;
    }

    public int OccupiedItemCount()
    {
        HashSet<GameObject> unique = new HashSet<GameObject>();
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
                if (cellOccupants[x, y] != null)
                    unique.Add(cellOccupants[x, y]);
        return unique.Count;
    }

    public int TotalCells() => columns * rows;

    public bool IsGridFull() => OccupiedCellCount() >= TotalCells();

    void OnDrawGizmos()
    {
        // Draw in edit mode too
        if (!showDebugGrid) return;

        if (cellCenters == null)
        {
            // Preview grid in editor without playing
            Vector2 sackCenter = (Vector2)transform.position + gridOffset;
            Vector2 previewCellSize = new Vector2(gridWorldSize.x / columns, gridWorldSize.y / rows);
            Vector2 bl = sackCenter - gridWorldSize * 0.5f;

            Gizmos.color = debugColor;
            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    Vector2 c = new Vector2(
                        bl.x + (x + 0.5f) * previewCellSize.x,
                        bl.y + (y + 0.5f) * previewCellSize.y
                    );
                    Gizmos.DrawWireCube(c, previewCellSize * 0.95f);
                }
            }
            return;
        }

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                bool occupied = cellOccupants != null && cellOccupants[x, y] != null;
                Gizmos.color = occupied ? debugOccupiedColor : debugColor;
                Gizmos.DrawWireCube(cellCenters[x, y], cellSize * 0.95f);
            }
        }
    }
}