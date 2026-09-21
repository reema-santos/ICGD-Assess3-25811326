using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    // tile prefabs 0-8
    [SerializeField] private GameObject emptyPrefab;
    [SerializeField] private GameObject outsideCornerPrefab;
    [SerializeField] private GameObject outsideWallPrefab;
    [SerializeField] private GameObject insideCornerPrefab;
    [SerializeField] private GameObject insideWallPrefab;
    [SerializeField] private GameObject standardPelletPrefab;
    [SerializeField] private GameObject powerPelletPrefab;
    [SerializeField] private GameObject tJunctionPrefab;
    [SerializeField] private GameObject ghostExitPrefab;

    [SerializeField] private GameObject powerPellet = null;

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

    private int[,] fullMap; // mirrored grid
    private int[,] wallAxis; // full map (as above) but stores only straight walls

    // named consts to be inserted into wallAxis, tells u if a line is horizontal or vertical (or neither)
    private const int AXIS_NONE = 0;
    private const int AXIS_HORIZONTAL = 1;
    private const int AXIS_VERTICAL = 2;

    private int quadRows, quadCols, fullRows, fullCols;

    private void Start()
    {
        GameObject manualLevel = GameObject.Find(manualLevelName);
        if (manualLevel != null) Destroy(manualLevel);

        GenerateFullMapMatrix();
        ComputeWallAxes();
        CreateLevelLayout();
        AdjustCameraViewport();
    }

    private void GenerateFullMapMatrix()
    {
        quadRows = levelMap.GetLength(0);
        quadCols = levelMap.GetLength(1);

        fullRows = quadRows * 2;
        fullCols = quadCols * 2;
        fullMap = new int[fullRows, fullCols];

        for (int r = 0; r < fullRows; r++)
        {
            for (int c = 0; c < fullCols; c++)
            {
                int sourceR = (r < quadRows) ? r : (fullRows - 1 - r);
                int sourceC = (c < quadCols) ? c : (fullCols - 1 - c);
                fullMap[r, c] = levelMap[sourceR, sourceC];
            }
        }
    }

    private void ComputeWallAxes()
    {
        wallAxis = new int[fullRows, fullCols];

        for (int r = 0; r < fullRows; r++)
        {
            for (int c = 0; c < fullCols; c++)
            {
                int type = fullMap[r, c];
                if (type != 2 && type != 4) continue;

                // checks if adjacend tiles are structures (i.e. NOT pellets or empty)
                bool up = IsStructure(r - 1, c);
                bool down = IsStructure(r + 1, c);
                bool left = IsStructure(r, c - 1);
                bool right = IsStructure(r, c + 1);

                bool vertical = up && down;
                bool horizontal = left && right;

                if (vertical && !horizontal)
                {
                    wallAxis[r, c] = AXIS_VERTICAL;
                }
                else if (horizontal && !vertical)
                {
                    wallAxis[r, c] = AXIS_HORIZONTAL;
                }

                // is adjacent to both vertical and horizontal lines
                else if (vertical && horizontal)
                {
                    bool sameVertical =
                        SameLayer(type, fullMap[r - 1, c]) &&
                        SameLayer(type, fullMap[r + 1, c]);

                    bool sameHorizontal =
                        SameLayer(type, fullMap[r, c - 1]) &&
                        SameLayer(type, fullMap[r, c + 1]);

                    wallAxis[r, c] = (sameVertical && !sameHorizontal) ? AXIS_VERTICAL : AXIS_HORIZONTAL;
                }

                // only has one neighbour
                else
                {
                    wallAxis[r, c] = (up || down) ? AXIS_VERTICAL : AXIS_HORIZONTAL;
                }
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

                Vector3 position = new Vector3(c * tileSize, -r * tileSize, 0);
                GameObject spawnedTile = Instantiate(prefab, position, Quaternion.identity, levelRoot.transform);
                spawnedTile.name = $"Tile_{r}_{c}_Type_{tileType}";

                if (prefab == powerPelletPrefab && powerPellet != null)
                {
                    powerPellet = Instantiate(powerPellet, position, Quaternion.identity);
                    powerPellet.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                    Animator animator = powerPellet.GetComponent<Animator>();
                    animator.Play("PowerPellet_Flash");
                }

                float rotationAngle = CalculateRotation(r, c, tileType);
                spawnedTile.transform.rotation = Quaternion.Euler(0, 0, rotationAngle);
            }
        }
    }
    private float CalculateRotation(int r, int c, int type)
    {
        if (type == 2 || type == 4)
            return (wallAxis[r, c] == AXIS_VERTICAL) ? 90f : 0f;

        if (type == 7)
        {
            if (!IsStructure(r - 1, c)) return 0f;
            if (!IsStructure(r, c + 1)) return 90f;
            if (!IsStructure(r + 1, c)) return 180f;
            if (!IsStructure(r, c - 1)) return 270f;
            return 0f;
        }

        if (type == 1 || type == 3)
        {
            bool up = CornerCanConnect(r - 1, c, true,  type);
            bool down = CornerCanConnect(r + 1, c, true,  type);
            bool left = CornerCanConnect(r, c - 1, false, type);
            bool right = CornerCanConnect(r, c + 1, false, type);

            bool useDown = ChooseSecond(up, down, r - 1, c, r + 1, c);
            bool useRight = ChooseSecond(left, right, r, c - 1, r, c + 1);

            bool hasVertical = up || down;
            bool hasHorizontal = left || right;

            if (hasVertical && hasHorizontal)
            {
                if (useDown && useRight) return 0f;    // down + right
                if (useDown && !useRight) return 270f;  // down + left
                if (!useDown && !useRight) return 180f;  // up + left
                return 90f;                              // up + right
            }

            Debug.LogWarning($"Corner at ({r},{c}) type {type}: u={up} d={down} l={left} r={right}");
            return 0f;
        }

        return 0f;
    }

    private bool ChooseSecond(bool firstOk, bool secondOk,
                              int r1, int c1, int r2, int c2)
    {
        if (firstOk && secondOk)
        {
            bool firstIsWall  = IsStraightWall(r1, c1);
            bool secondIsWall = IsStraightWall(r2, c2);

            if (firstIsWall && !secondIsWall) return false;
            if (secondIsWall && !firstIsWall) return true;
            return false;
        }

        return secondOk;
    }

    private bool CornerCanConnect(int r, int c, bool vertical, int sourceType)
    {
        if (r < 0 || r >= fullRows || c < 0 || c >= fullCols) return false;

        int neighbourType = fullMap[r, c];
        if (!IsStructureType(neighbourType)) return false;
        if (!SameLayer(sourceType, neighbourType)) return false;

        if (neighbourType == 2 || neighbourType == 4)
        {
            int required = vertical ? AXIS_VERTICAL : AXIS_HORIZONTAL;
            return wallAxis[r, c] == required;
        }

        return true;
    }

    private bool IsStraightWall(int r, int c)
    {
        if (r < 0 || r >= fullRows || c < 0 || c >= fullCols) return false;
        int t = fullMap[r, c];
        return t == 2 || t == 4;
    }

    // returns true if at (r, c) there is a solid piece. not including empty or pellet types
    private bool IsStructure(int r, int c)
    {
        if (r < 0 || r >= fullRows || c < 0 || c >= fullCols) return false;
        return IsStructureType(fullMap[r, c]);
    }

    private bool IsStructureType(int t)
    {
        return t == 1 || t == 2 || t == 3 || t == 4 || t == 7 || t == 8;
    }

    private bool SameLayer(int sourceType, int neighbourType)
    {
        // checks if two pieces are the same kind (inside vs outside piece)
        if (sourceType == 3 || sourceType == 4)
            return neighbourType == 3 || neighbourType == 4;

        return neighbourType == 1 || neighbourType == 2 ||
               neighbourType == 7 || neighbourType == 8;
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

        mainCam.transform.position = new Vector3(centerX, centerY, -10f);

        float screenAspect = (float)Screen.width / Screen.height;
        float targetSizeByHeight = mapHeight / 2f;
        float targetSizeByWidth = (mapWidth / 2f) / screenAspect;

        mainCam.orthographicSize = Mathf.Max(targetSizeByHeight, targetSizeByWidth);
    }
}