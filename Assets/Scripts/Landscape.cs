using System;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Landscape : MonoBehaviour
{
    private bool _isDirty;
    private Mesh _mesh;
    [SerializeField] private TerrainType[] terrainTypes;

    public Slider persistanceSlider, lacunaritySlider, octavesSlider, shiftXSlider, shiftYSlider, scaleSlider;
    public Toggle toggleFilter;
    
    [Range(0, 1)] [SerializeField] private float persistance = 0.5f;
    [Range(1, 3)] [SerializeField] private float lacunarity = 2f;
    [Range(1, 8)] [SerializeField] private int octaves = 4;
 
    [SerializeField] private float scale = 1f;
    [SerializeField] private Vector2 shift = Vector2.zero;
    [SerializeField] private int state = 0;
    [SerializeField] private int resolution = 256;
    [SerializeField] private float length = 256f;
    [SerializeField] private float height = 50f;
    [SerializeField] private float sharpness;
    
    private static float _maxheight = 0f;

    private void Awake()
    {
        (GetComponent<MeshFilter>().mesh = _mesh = new Mesh {name = name}).MarkDynamic();
        persistanceSlider.onValueChanged.AddListener(delegate(float newValue)
        {
            persistance = newValue; GenerateLandscape();  
        });
        lacunaritySlider.onValueChanged.AddListener(delegate(float newValue)
        {
            lacunarity = newValue;
            GenerateLandscape();
        });
        octavesSlider.onValueChanged.AddListener(delegate(float newValue)
        {
            octaves = (int) newValue;
            GenerateLandscape();
        });
        shiftXSlider.onValueChanged.AddListener(delegate(float newValue)
        {
            shift.x = newValue;
            GenerateLandscape();
        });
        shiftYSlider.onValueChanged.AddListener(delegate(float newValue)
        {
            shift.y = newValue;
            GenerateLandscape();
        });
        scaleSlider.onValueChanged.AddListener(delegate(float newValue)
        {
            scale = newValue;
            GenerateLandscape();
        });
        toggleFilter.onValueChanged.AddListener(delegate(bool isOn)
        {
            GenerateLandscape();
        });
        
    }


    private void OnValidate()
    {
        _isDirty = true;
    }
 
    private void Update()
    {
        if (!_isDirty) return;
        GenerateLandscape();
        _isDirty = false;
    }
 
    private void GenerateLandscape()
    {
        // First, initialize the data structures:
        var numberOfVertices = (resolution + 1) * (resolution + 1);
        var colors = new Color[numberOfVertices];
        var triangles = new int[resolution * resolution * 6];
        var vertices = new Vector3[numberOfVertices];
        _maxheight = 0f;

        // Then, loop over the vertices and populate the data structures:
        for(int i = 0, z = 0; z <= resolution; z++)
        {
            for (var x = 0; x <= resolution; x++)
            {
                var coords = new Vector2((float) x / resolution,  (float) z / resolution);
                var elevation =  1.414214f * FractalNoise(coords, persistance, lacunarity, octaves, scale, shift, state);
                foreach (var terrainType in terrainTypes)
                {
                    if (!(elevation <= terrainType.height)) continue;
                    colors[i] = terrainType.color;
                    break;
                }

                var fractalHeight = height * elevation;
                vertices[i] = new Vector3(length * coords.x,  fractalHeight, length * coords.y);
                if (fractalHeight > _maxheight)
                {
                    _maxheight = fractalHeight;
                }
                i++;
            }
        }
        
        // Then looping over the vertices and producing the triangles 
        var tris = 0;
        var vert = 0;
        for (var z = 0; z < resolution; z++) {
            for(var x = 0; x < resolution; x++) {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + resolution + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + resolution + 1;   
                triangles[tris + 5] = vert + resolution + 2;

                if (toggleFilter.isOn) {
                    var vertex = vertices[vert];
                    vertices[vert].y = ButtesFilter(vertex.y);
                }
                
                vert++;
                tris += 6;

            }

            vert++;

        }

        // Assign the data structures to the mesh
        _mesh.Clear();
        _mesh.SetVertices(vertices);
        _mesh.SetColors(colors);
        _mesh.SetTriangles(triangles, 0);
        _mesh.RecalculateNormals();
    }
 
    private static float FractalNoise(Vector2 coords, float persistance, float lacunarity, int octaves, float scale,
        Vector2 shift, int state)
    {
        var fractalNoise = 0f;
        Random.InitState(state);
        for (var i = 0; i < octaves; i++)
        {
            var frequency = Mathf.Pow(lacunarity, i);
            var amplitude = Mathf.Pow(persistance, i);
            var x = coords.x / scale * frequency + Random.value + shift.x;
            var y = coords.y / scale * frequency + Random.value + shift.y;
            fractalNoise += Mathf.PerlinNoise(x, y) * amplitude;
        }

        return fractalNoise;
    }

    private float ButtesFilter(float _height)
    {
        var halfMax = _maxheight / 2;
        var scaledHeight = sharpness * ((_height - halfMax) / halfMax);
        var logistic = 1 / (1 + Mathf.Exp(scaledHeight));
        return _maxheight * (1 + logistic / 2);
    }
    
    private float WaterFilter(float _height)
    {
        var waterHeight = terrainTypes[0].height * height;
        return _height <= waterHeight ? waterHeight: _height;
    } 

}

[Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;
}