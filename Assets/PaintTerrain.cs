using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PaintTerrain : MonoBehaviour {

    public Terrain terrain;
    public TerrainData terrainData;

    [Space]
    [Range(0.00001F, 0.001F)] public float minHeight = 0.00025F;
    [Range(0.00001F, 0.001F)] public float maxHeight = 0.00075F;
    [Range(1, 100)] public int minRadius = 1;
    [Range(1, 100)] public int maxRadius = 10;
    public int hillAmount = 1000;

    float a = 0;

    float[,] heights;

    int heightMapWidth, heightmapHeight;

    void Start () {
        heightMapWidth = terrainData.heightmapWidth;
        heightmapHeight = terrainData.heightmapHeight;
        heights = terrainData.GetHeights(0, 0, heightMapWidth, heightmapHeight);

        CreateMultipleHills(hillAmount);
    }
	
	void Update () {
		if (Input.GetKeyDown(KeyCode.F))
        {
            FlattenTerrain();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            CreateMultipleHills(hillAmount);
        }
	}

    void CreateMultipleHills(int amount)
    {
        FlattenTerrain();

        for (int i = 0; i < amount; i++)
        {
            int radius = Random.Range(minRadius, maxRadius);
            int x = Mathf.RoundToInt(Random.Range(0, terrainData.heightmapWidth));
            int z = Mathf.RoundToInt(Random.Range(0, terrainData.heightmapHeight));

            if (x - radius < 0) x = radius;
            if (x + radius > terrainData.heightmapWidth) x = Mathf.RoundToInt(terrainData.heightmapWidth - radius);

            if (z - radius < 0) z = radius;
            if (z + radius > terrainData.heightmapHeight) z = Mathf.RoundToInt(terrainData.heightmapHeight - radius);

            CreateHill(x, z, Random.Range(minHeight, maxHeight), radius);
        }
    }

    void RaiseTerrain(Vector3 point, float height, float falloff, float radius)
    {
        int mouseX = (int)((point.x / terrainData.size.x) * heightMapWidth);
        int mouseZ = (int)((point.z / terrainData.size.z) * heightmapHeight);

        float[,] modefiedHeights = new float[1, 1];
        float y = heights[mouseX, mouseZ];
        y += height;

        if (y > terrainData.size.y)
        {
            y = terrainData.size.y;
        }

        for (int x = (int)-radius; x < (int)radius; x++)
        {
            for (int z = (int)-radius; z < (int)radius; z++)
            {
                float[,] mH = new float[1, 1];
                mH[0, 0] = 0.01F;
                terrainData.SetHeights(mouseX + x, mouseZ + z, mH);
            }
        }

        modefiedHeights[0, 0] = y;
        heights[mouseX, mouseZ] = y;
        terrainData.SetHeights(mouseX, mouseZ, modefiedHeights);
    }

    void FlattenTerrain()
    {
        int xCor = terrainData.heightmapWidth;
        int yCor = terrainData.heightmapHeight;
        var heights = terrain.terrainData.GetHeights(0, 0, xCor, yCor);

        for (int x = 0; x < xCor; x++)
        {
            for (int y = 0; y < yCor; y++)
            {
                heights[x, y] = 0;
            }
        }
        terrain.terrainData.SetHeights(0, 0, heights);
    }

    public void CreateHill(int x, int y, float height, int radius)
    {
        int diameter = radius * 2;
        int heightsCenterX = radius;
        int heightsCenterY = radius;
        int baseX = x - heightsCenterX;
        int baseY = y - heightsCenterY;
        heights = terrainData.GetHeights(baseX, baseY, diameter, diameter);

        Vector2 controlPoint1 = new Vector2(0.52f, 0.06f);
        Vector2 controlPoint2 = new Vector2(0.42f, 0.95f);

        for (int a = 0; a < diameter; a++)
        {
            for (int b = 0; b < diameter; b++)
            {
                float distanceFromCenter = Mathf.Sqrt(Mathf.Pow((heightsCenterY - b), 2) + Mathf.Pow((heightsCenterX - a), 2));
                float time = Mathf.Max(1 - (distanceFromCenter / radius), 0);
                Vector2 bezierPoint = BezierCurve(time, new Vector2(0, 0), controlPoint1, controlPoint2, new Vector2(1, 1));

                float pointHeight = bezierPoint.y * height;

                if (pointHeight < 0)
                    pointHeight = 0;

                heights[a, b] = pointHeight;
            }
        }

        terrainData.SetHeights(baseX, baseY, heights);

        Smooth();
    }

    private void Smooth()
    {

        float[,] height = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapWidth,
                                          terrain.terrainData.heightmapHeight);
        float k = 0.5f;
        /* Rows, left to right */
        for (int x = 1; x < terrain.terrainData.heightmapWidth; x++)
            for (int z = 0; z < terrain.terrainData.heightmapHeight; z++)
                height[x, z] = height[x - 1, z] * (1 - k) +
                          height[x, z] * k;

        /* Rows, right to left*/
        for (int x = terrain.terrainData.heightmapWidth - 2; x < -1; x--)
            for (int z = 0; z < terrain.terrainData.heightmapHeight; z++)
                height[x, z] = height[x + 1, z] * (1 - k) +
                          height[x, z] * k;

        /* Columns, bottom to top */
        for (int x = 0; x < terrain.terrainData.heightmapWidth; x++)
            for (int z = 1; z < terrain.terrainData.heightmapHeight; z++)
                height[x, z] = height[x, z - 1] * (1 - k) +
                          height[x, z] * k;

        /* Columns, top to bottom */
        for (int x = 0; x < terrain.terrainData.heightmapWidth; x++)
            for (int z = terrain.terrainData.heightmapHeight; z < -1; z--)
                height[x, z] = height[x, z + 1] * (1 - k) +
                          height[x, z] * k;

        terrain.terrainData.SetHeights(0, 0, height);
    }

    public static Vector2 BezierCurve(float t, Vector2 pOne, Vector2 pTwo, Vector2 pThree, Vector2 pFour)
    {
        return Mathf.Pow(1 - t, 3) * pOne + 3 * Mathf.Pow(1 - t, 2) * t * pTwo + 3 * (1 - t) * t * t * pThree + Mathf.Pow(t, 3) * pFour;
    }
}
