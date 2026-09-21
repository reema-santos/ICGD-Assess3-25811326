using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] private GameObject emptyPrefab;
    [SerializeField] private GameObject outsideCornerPrefab;
    [SerializeField] private GameObject outsideWallPrefab;
    [SerializeField] private GameObject insideCornerPrefab;
    [SerializeField] private GameObject insideWallPrefab;
    [SerializeField] private GameObject standardPelletPrefab;
    [SerializeField] private GameObject powerPelletPrefab;
    [SerializeField] private GameObject tJunctionPrefab;
    [SerializeField] private GameObject ghostExitPrefab;

    [SerializeField] private string manualLevelName = "manualLevel01";
    [SerializeField] private float tileSize = 1.0f;

    private int[,] levelMap =
    {
        {1,2,2,2,2,2,2,2,2,2,2,2,2,7},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,4},
        {2,6,4,0,0,4,5,4,0,0,0,4,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,3},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,5},
        {2,5,3,4,4,3,5,3,3,5,3,4,4,4},
        {2,5,3,4,4,3,5,4,4,5,3,4,4,3},
        {2,5,5,5,5,5,5,4,4,5,5,5,5,4},
        {1,2,2,2,2,1,5,4,3,4,4,3,0,4},
        {0,0,0,0,0,2,5,4,3,4,4,3,0,3},
        {0,0,0,0,0,2,5,4,4,0,0,0,0,0},
        {0,0,0,0,0,2,5,4,4,0,3,4,4,8},
        {2,2,2,2,2,1,5,3,3,0,4,0,0,0},
        {0,0,0,0,0,0,5,0,0,0,4,0,0,0},
    };

    private int[,] fullMap;
    private int quadRows;
    private int quadCols;
    private int fullRows;
    private int fullCols;

    private void Start()
    {
        GameObject manualLevel = GameObject.Find(manualLevelName);
        if (manualLevel != null)
        {
            Destroy(manualLevel);
        }

        GenerateFullMapMatrix();
        CreateLevelLayout();
        AdjustCameraViewport();
    }

    private void GenerateFullMapMatrix()
    {
        quadRows = levelMap.GetLength(0);
        quadCols = levelMap.GetLength(1);

        int effectiveQuadRows = quadRows;

        fullRows = effectiveQuadRows * 2;
        fullCols = quadCols * 2;
        fullMap = new int[fullRows, fullCols];

        for (int r = 0; r < fullRows; r++)
        {
            for (int c = 0; c < fullCols; c++)
            {
                int sourceR = (r < effectiveQuadRows)
                    ? r
                    : (fullRows - 1 - r);

                int sourceC = (c < quadCols)
                    ? c
                    : (fullCols - 1 - c);

                fullMap[r, c] = levelMap[sourceR, sourceC];
            }
        }
    }

    private void CreateLevelLayout()
    {
        GameObject levelRoot = new GameObject("Procedural Level");

        for (int r = 0; r < fullRows; r++)
        {
            for (int c = 0; c < fullCols; c++)
            {
                int tileType = fullMap[r, c];

                GameObject prefab = GetPrefabForType(tileType);
                if (prefab == null) continue;

                Vector3 position = new Vector3(
                    c * tileSize,
                    -r * tileSize,
                    0
                );

                GameObject spawnedTile = Instantiate(
                    prefab,
                    position,
                    Quaternion.identity,
                    levelRoot.transform
                );

                spawnedTile.name = $"Tile_{r}_{c}_Type_{tileType}";

                float rotationAngle = CalculateRotation(
                    r,
                    c,
                    tileType
                );

                spawnedTile.transform.rotation =
                    Quaternion.Euler(0, 0, rotationAngle);
            }
        }
    }

    private float CalculateRotation(int r, int c, int type)
{
    bool u = IsConnected(r - 1, c, type);
    bool d = IsConnected(r + 1, c, type);
    bool l = IsConnected(r, c - 1, type);
    bool rNeighbor = IsConnected(r, c + 1, type);

    if (type == 2 || type == 4)
    {

        if (type == 4)
        {
            if (l && rNeighbor)
                return 0f;

            if (u && d)
                return 90f;

            if (l || rNeighbor)
                return 0f;

            if (u || d)
                return 90f;

            return 0f;
        }

        if (u && d && !l && !rNeighbor)
            return 90f;

        if (l && rNeighbor && !u && !d)
            return 0f;

        if (u || d)
            return 90f;

        if (l || rNeighbor)
            return 0f;
    }

    if (type == 7)
    {
        if (!u)
            return 0f;

        if (!rNeighbor)
            return 90f;

        if (!d)
            return 180f;

        if (!l)
            return 270f;

        return 0f;
    }

    if (type == 1 || type == 3)
    {

        if (type == 3 && u && d && l && rNeighbor)
        {
            bool type3Above =
                r > 0 && fullMap[r - 1, c] == 3;

            bool type3Below =
                r < fullRows - 1 && fullMap[r + 1, c] == 3;

            bool type3Left =
                c > 0 && fullMap[r, c - 1] == 3;

            bool type3Right =
                c < fullCols - 1 && fullMap[r, c + 1] == 3;

            bool topHalf = r < fullRows / 2;
            bool leftHalf = c < fullCols / 2;

            if (type3Right)
            {
                if (topHalf)
                    return 0f;
                else
                    return 90f;
            }

            if (type3Left)
            {
                if (topHalf)
                    return 270f;
                else
                    return 180f;
            }

            if (type3Below)
            {
                if (leftHalf)
                    return 0f;
                else
                    return 270f;
            }

            if (type3Above)
            {
                if (leftHalf)
                    return 90f;
                else
                    return 180f;
            }
        }

        if (rNeighbor && d && !u && !l)
            return 0f;

        if (d && l && !u && !rNeighbor)
            return 270f;

        if (l && u && !d && !rNeighbor)
            return 180f;

        if (u && rNeighbor && !d && !l)
            return 90f;

        Debug.LogWarning(
            $"Corner at ({r},{c}) type {type}: " +
            $"ambiguous pattern u={u} d={d} l={l} r={rNeighbor}"
        );

        if (rNeighbor && d)
            return 0f;

        if (d && l)
            return 270f;

        if (l && u)
            return 180f;

        if (u && rNeighbor)
            return 90f;
    }

    return 0f;
}

private bool IsConnected(int r, int c, int sourceLayerType)
{
    if (r < 0 || r >= fullRows ||
        c < 0 || c >= fullCols)
    {
        return false;
    }

    int neighborType = fullMap[r, c];

    if (sourceLayerType == 3 || sourceLayerType == 4)
    {
        return neighborType == 3 ||
               neighborType == 4;
    }

    if (sourceLayerType == 1 || sourceLayerType == 2)
    {
        return neighborType == 1 ||
               neighborType == 2 ||
               neighborType == 7 ||
               neighborType == 8;
    }

    if (sourceLayerType == 7)
    {
        return neighborType == 1 ||
               neighborType == 2 ||
               neighborType == 3 ||
               neighborType == 4 ||
               neighborType == 7 ||
               neighborType == 8;
    }

    if (sourceLayerType == 8)
    {
        return neighborType == 1 ||
               neighborType == 2 ||
               neighborType == 7 ||
               neighborType == 8;
    }

    return neighborType == sourceLayerType;
}

    private bool IsWallOrSame(int r, int c, int sourceLayerType)
    {
        if (r < 0 || r >= fullRows || c < 0 || c >= fullCols)
            return true;

        int neighborType = fullMap[r, c];

        if (neighborType == 1 ||
            neighborType == 2 ||
            neighborType == 3 ||
            neighborType == 4 ||
            neighborType == 7 ||
            neighborType == 8)
        {
            return true;
        }

        return neighborType == sourceLayerType;
    }

    private GameObject GetPrefabForType(int type)
    {
        return type switch
        {
            0 => emptyPrefab,
            1 => outsideCornerPrefab,
            2 => outsideWallPrefab,
            3 => insideCornerPrefab,
            4 => insideWallPrefab,
            5 => standardPelletPrefab,
            6 => powerPelletPrefab,
            7 => tJunctionPrefab,
            8 => ghostExitPrefab,
            _ => null
        };
    }

    private void AdjustCameraViewport()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        float mapWidth = fullCols * tileSize;
        float mapHeight = fullRows * tileSize;

        float centerX = (mapWidth - tileSize) / 2f;
        float centerY = -(mapHeight - tileSize) / 2f;

        mainCam.transform.position =
            new Vector3(centerX, centerY, -10f);

        float screenAspect =
            (float)Screen.width / Screen.height;

        float targetSizeByHeight =
            mapHeight / 2f;

        float targetSizeByWidth =
            (mapWidth / 2f) / screenAspect;

        mainCam.orthographicSize =
            Mathf.Max(
                targetSizeByHeight,
                targetSizeByWidth
            );
    }
}