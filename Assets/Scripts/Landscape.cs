using System;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

// CG&CV: Group 7

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]

public class Landscape : MonoBehaviour
{
    private bool _isDirty;
    private Mesh _mesh;
    [SerializeField] private Gradient defaultGradient;
    [SerializeField] private Gradient buttesGradient;
    private Gradient gradient;


    public Slider persistanceSlider, lacunaritySlider, octavesSlider, shiftXSlider, shiftYSlider, scaleSlider;
    public Toggle buttesFilter;
    
    [Range(0, 1)] [SerializeField] private float gain = 0.5f;
    [Range(1, 3)] [SerializeField] private float lacunarity = 2f;
    [Range(1, 8)] [SerializeField] private int octaves = 4;
 
    [SerializeField] private float scale = 5f;
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

        // set default values
        gradient = defaultGradient;
        _isDirty = true;

        // add listeners to sliders
        persistanceSlider.onValueChanged.AddListener(delegate(float newValue)
        {
            gain = newValue;
            _isDirty = true;  
        });
        lacunaritySlider.onValueChanged.AddListener(delegate(float newValue)
        {
            lacunarity = newValue;
            _isDirty = true;
        });
        octavesSlider.onValueChanged.AddListener(delegate(float newValue)
        {
            octaves = (int) newValue;
            _isDirty = true;
        });
        shiftXSlider.onValueChanged.AddListener(delegate(float newValue)
        {
            shift.x = newValue;
            _isDirty = true;
        });
        shiftYSlider.onValueChanged.AddListener(delegate(float newValue)
        {
            shift.y = newValue;
            _isDirty = true;
        });
        scaleSlider.onValueChanged.AddListener(delegate(float newValue)
        {
            scale = newValue;
            _isDirty = true;
        });
        buttesFilter.onValueChanged.AddListener(delegate(bool isOn)
        {
            if(isOn) {
                gradient = buttesGradient;
            } else {
                gradient = defaultGradient;
            }
            _isDirty = true;
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
        int n_vertices = resolution * resolution;
        int n_triangles = (resolution-1) * (resolution-1) * 2; //(int) length; // ????
        var colors = new Color[n_vertices];
        var triangles = new int[n_triangles * 3];
        var vertices = new Vector3[n_vertices];
        _maxheight = 0f;

        // Then, loop over the vertices and populate the data structures:
        var i = 0;      // vertices counter
        for (int z = 0; z < resolution; z++)
        {
            for(var x = 0; x < resolution; x++)
            {
                // get coordinates
                var coords = new Vector2((float) x / (resolution - 1), (float) z / (resolution - 1));
                
                // compute elevation
                var elevation = FractalNoise(coords, gain, lacunarity, octaves, scale, shift, state);
                //var elevation = 1.414214f * FractalNoise(coords, gain, lacunarity, octaves, scale, shift, state);

                // set color and create vertices
                colors[i]   = gradient.Evaluate(elevation);
                vertices[i] = new Vector3(length * coords.x, height * elevation, length * coords.y);
                
                // save max height for Buttes filter
                if (height * elevation > _maxheight)
                {
                    _maxheight = height * elevation;
                }

                // increment counter
                i++;
            }
        }
        
        // Then looping over the vertices and producing the triangles 
        i = 0;          // vertices counter
        var tr = 0;     // triangle counter
        for (int z = 0; z < resolution-1; z++)
        {
            for(var x = 0; x < resolution-1; x++)
            {
                triangles[tr + 0] = i + 0;
                triangles[tr + 1] = i + (resolution-1) + 1;
                triangles[tr + 2] = i + 1;
                triangles[tr + 3] = i + 1;
                triangles[tr + 4] = i + (resolution-1) + 1;
                triangles[tr + 5] = i + (resolution-1) + 2;

                // apply Buttes filter
                if (buttesFilter.isOn) {
                    var vertex = vertices[i];
                    vertices[i].y = ButtesFilter(vertex.y);
                }
                
                // increment counters
                i++;
                tr += 6;
            }
            // apply Buttes filter
            if (buttesFilter.isOn) {
                var vertex = vertices[i];
                vertices[i].y = ButtesFilter(vertex.y);
            }
            
            // increment counter
            i++;
        }

        // Assign the data structures to the mesh
        _mesh.Clear();
        _mesh.SetVertices(vertices);
        _mesh.SetColors(colors);
        _mesh.SetTriangles(triangles, 0);
        _mesh.RecalculateNormals();
    }
 
    private static float FractalNoise(Vector2 coords, float gain, float lacunarity, int octaves, float scale,
        Vector2 shift, int state)
    {
        var noise = 0.0f;
        var frequency = 1.0f;
        var amplitude = 1.0f;
        Random.InitState(state);

        // Loop over octaves to compute fractal noise
        for(int i = 0; i < octaves; i++)
        {
            // get x and y values
            var x = coords.x * frequency * scale + Random.value + shift.x;
            var y = coords.y * frequency * scale + Random.value + shift.y;

            // compute new noise with Perlin noise
            noise = noise + (Mathf.PerlinNoise(x, y) * amplitude);

            // update frequency and amplitude
            frequency = frequency * lacunarity;
            amplitude = amplitude * gain;
        }

        return noise;
    }

    // Function to make Buttes'
    // Calculates the new height for one location
    private float ButtesFilter(float _height)
    {
        // Use the logistic function to get an S-curve
        var halfMax = _maxheight / 2;
        var scaledHeight = sharpness * ((_height - halfMax) / halfMax);
        // Take sigmoid
        var logistic = 1 / (1 + Mathf.Exp(-scaledHeight));
        return _maxheight * (1 + logistic / 2);
    }
    
    // private float WaterFilter(float _height)
    // {
    //     var waterHeight = terrainTypes[0].height * height;
    //     return _height <= waterHeight ? waterHeight: _height;
    // }
}