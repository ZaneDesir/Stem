using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class Stem : MonoBehaviour
{
    [Range(1.0f, 10.0f)]
    public float age = 1.0f;

    [Range(1.0f, 10.0f)]
    public float rateOfGrowth = 1.0f;

    [Range(0.0f, 10.0f)]
    public float roughness = 0.0f;

    [Range(3.0f, 20.0f)]
    public float thickness = 10.0f;

    private float noiseAmplitude = 1.0f;
    private float noiseFrequency = 1.0f;
    private float currentLength = 0.0f;

    private float length = 0.0f;
    private float maxLength = 5.0f;

    private float currentRoughness = 0.0f;
    private float currentThickness = 10.0f;

    private int heightSegments = 8;
    private int radialSegments = 8;

    private List<Vector3> points = new List<Vector3>();

    public void Update()
    {
        var length = 5.0f * age;
        if (!Mathf.Approximately(length, maxLength))
        {
            currentLength = 0.0f;
            maxLength = length;
        }

        if (currentLength < maxLength || 
            !Mathf.Approximately(thickness, currentThickness) ||
            !Mathf.Approximately(roughness, currentRoughness))
        {
            currentRoughness = roughness;
            currentThickness = thickness;
            currentLength += rateOfGrowth * Time.deltaTime;

            points.Clear();

            var numPoints = Mathf.RoundToInt(currentLength * 10.0f);
            points.Capacity = numPoints;

            var position = new Vector3(0.0f, 0.0f, 0.0f);
            for (var i = 0; i < numPoints; i++)
            {
                var t = (float)i / (numPoints - 1);
                position.y = i * 0.5f;

                var noise = Mathf.PerlinNoise(i * noiseFrequency, roughness) * noiseAmplitude;

                var xPos = Mathf.Sin(t * roughness * Mathf.PI);
                var yPos = Mathf.Cos(t * roughness * Mathf.PI);
                position += new Vector3(xPos * noise, 0.0f, yPos * noise);

                points.Add(position);
            }

            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter)
            {
                meshFilter.mesh = BuildMesh(points);
            }
        }
    }

    public Mesh BuildMesh(List<Vector3> points)
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        var offset = 0;

        for (var k = 0; k < points.Count - 1; k++)
        {
            var p0 = points[k];
            var p1 = points[k + 1];

            var vec = p1 - p0;
            var height = vec.magnitude * 0.5f;
            var vecNormalize = vec.normalized;

            var radius = thickness * (1.0f - (float)k / (float)(points.Count - 1));
            var angle = ((Mathf.PI * 2.0f) / radialSegments);

            for (int i = 0; i <= heightSegments; i++)
            {
                for (int j = 0; j <= radialSegments; j++)
                {
                    var x0 = radius * Mathf.Cos(j * angle);
                    var z0 = radius * Mathf.Sin(j * angle);

                    var offsetVec = p0 + (p1 - p0) * ((float)i / (float)heightSegments);
                    vertices.Add(offsetVec + new Vector3(x0, 0.0f, z0));

                    var n = new Vector3(x0, 0, z0);
                    normals.Add(n.normalized);
                    uvs.Add(new Vector2(j / (float)radialSegments, i / (float)heightSegments));

                    if (i != heightSegments)
                    {
                        triangles.Add(offset + radialSegments + 1);
                        triangles.Add(offset);
                        triangles.Add(offset + radialSegments);
                        triangles.Add(offset + radialSegments + 1);
                        triangles.Add(offset + 1);
                        triangles.Add(offset);
                    }

                    offset++;

                }
            }
        }

        var mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = triangles.ToArray();
        return mesh;
    }

}

[InitializeOnLoad]
class StemUpdate
{
    static StemUpdate()
    {
        EditorApplication.update += Update;
    }

    static void Update()
    {
        var stems = Component.FindObjectsOfType<Stem>();
        foreach(var stem in stems)
        {
            stem.Update();
        }
    }
}