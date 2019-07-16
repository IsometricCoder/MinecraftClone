﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Block
{
    //public static readonly Dictionary<string, BlockType> BLOCK_TYPES = new Dictionary<string, BlockType>();


    public enum Type
    {
        Air,
        Grass,
        Dirt,
        Stone,
        Bedrock,
        Water,
        Sand
    }

    public static BlockType[] BLOCK_TYPES = Resources.LoadAll<BlockType>("Block Types");



    public static readonly Vector3[] FACE_DIRECTIONS = {
        Vector3.up,
        Vector3.down,
        Vector3.right,
        Vector3.left,
        Vector3.forward,
        Vector3.back
    };

    public BlockType type;

    //public Type type;

    //private static Dictionary<string, BlockType> LoadBlockTypes()
    //{
    //    Dictionary<string, BlockType> types = new Dictionary<string, BlockType>();

    //}

    public Block(string typeName)
    {
        //this.type = type;

        // Temp
        //string typeName = type.ToString();
        foreach (BlockType blockType in BLOCK_TYPES)
        {
            if (blockType.displayName == typeName)
            {
                this.type = blockType;
                break;
            }
        }
    }

    public bool IsTransparent()
    {
        //return this.type == Type.Air || this.type == Type.Water;
        return this.type.isTransparent;
    }

    public static Mesh GenerateCube()
    {
        Vector3[] directions = {
            Vector3.up,
            Vector3.down,
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right
        };

        CombineInstance[] combine = new CombineInstance[directions.Length];

        for (int i = 0; i < combine.Length; i++)
        {
            combine[i].mesh = GenerateQuad(directions[i]);
            //Debug.Log(combine[i].mesh.vertexCount);
            combine[i].transform = Matrix4x4.identity;
        }
        Mesh mesh = new Mesh();
        mesh.CombineMeshes(combine, true);

        return mesh;
    }

    public static Mesh GenerateQuad(Vector3 direction)
    {
        List<Vector3> vertices = new List<Vector3>();
        //List<Vector2> uvs = new List<Vector2>();
        int[] triangles = new int[6];

        float min = -0.5f;
        float max = 0.5f;
        vertices.Add(new Vector3(min, max, min));
        vertices.Add(new Vector3(min, max, max));
        vertices.Add(new Vector3(max, max, max));
        vertices.Add(new Vector3(max, max, min));

        Quaternion rot = Quaternion.FromToRotation(Vector3.up, direction);

        Vector3 temp = direction;
        temp.y = 0.0f;
        if (Vector3.Dot(Vector3.forward, temp) > 0.99f)
        {
            Quaternion rot2 = Quaternion.AngleAxis(180f, Vector3.up);
            rot = rot * rot2;
        }
        else if (temp.sqrMagnitude > 0.01f)
        {
            Quaternion rot2 = Quaternion.FromToRotation(Vector3.back, temp);
            rot = rot * rot2;
        }

        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] = rot * vertices[i];
        }



        //uvs.Add(new Vector2(0f, 0f));
        //uvs.Add(new Vector2(0f, 1f));
        //uvs.Add(new Vector2(1f, 1f));
        //uvs.Add(new Vector2(1f, 0f));

        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        triangles[3] = 0;
        triangles[4] = 2;
        triangles[5] = 3;

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        //mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();

        return mesh;
    }

    public Mesh GenerateFaces(bool[] faceIsVisible, AtlasReader atlasReader)
    {
        List<List<Vector3>> vertexLists = new List<List<Vector3>>();
        List<List<Vector3>> normalLists = new List<List<Vector3>>();
        List<List<Vector2>> uvLists = new List<List<Vector2>>();
        List<int[]> triangleLists = new List<int[]>();

        for (int i = 0; i < FACE_DIRECTIONS.Length; i++)
        {
            if (faceIsVisible[i] == false)
            {
                continue; // Don't bother making a mesh for a face that can't be seen.
            }

            GenerateBlockFace(FACE_DIRECTIONS[i], out List<Vector3> vertices, out List<Vector3> normals, out int[] triangles);

            Vector2Int[] atlasPositions = type.atlasPositions;
            int index = atlasPositions.Length == 1 ? 0 : i;

            List<Vector2> uvs = atlasReader.GetUVs(atlasPositions[index].x, atlasPositions[index].y);
            

            vertexLists.Add(vertices);
            normalLists.Add(normals);
            uvLists.Add(uvs);
            triangleLists.Add(triangles);
        }

        List<Vector3> allVertices = new List<Vector3>();
        List<Vector3> allNormals = new List<Vector3>();
        List<Vector2> allUVs = new List<Vector2>();
        List<int> allTriangles = new List<int>();

        foreach (List<Vector3> vertexList in vertexLists)
        {
            allVertices.AddRange(vertexList);
        }

        foreach (List<Vector3> normalList in normalLists)
        {
            allNormals.AddRange(normalList);
        }

        foreach (List<Vector2> uvList in uvLists)
        {
            allUVs.AddRange(uvList);
        }

        for (int i = 0; i < triangleLists.Count; i++)
        {
            for (int j = 0; j < triangleLists[i].Length; j++)
            {
                triangleLists[i][j] += i * 4;
            }
            allTriangles.AddRange(triangleLists[i]);
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(allVertices);
        mesh.SetNormals(allNormals);
        mesh.SetUVs(0, allUVs);
        mesh.SetTriangles(allTriangles.ToArray(), 0);

        return mesh;
    }

    public static void GenerateBlockFace(in Vector3 direction, out List<Vector3> vertices, out List<Vector3> normals, out int[] triangles)
    {
        vertices = new List<Vector3>();
        normals = new List<Vector3>() { direction, direction, direction, direction };
        triangles = new int[6]; // 2 Triangles

        // Set vertices
        float min = -0.5f;
        float max = 0.5f;
        vertices.Add(new Vector3(min, max, min));
        vertices.Add(new Vector3(min, max, max));
        vertices.Add(new Vector3(max, max, max));
        vertices.Add(new Vector3(max, max, min));

        Quaternion rot = Quaternion.FromToRotation(Vector3.up, direction);

        Vector3 temp = direction;
        temp.y = 0.0f;
        if (Vector3.Dot(Vector3.forward, temp) > 0.99f)
        {
            Quaternion rot2 = Quaternion.AngleAxis(180f, Vector3.up);
            rot = rot * rot2;
        }
        else if (temp.sqrMagnitude > 0.01f)
        {
            Quaternion rot2 = Quaternion.FromToRotation(Vector3.back, temp);
            rot = rot * rot2;
        }

        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] = rot * vertices[i];
        }

        // Set triangles
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        triangles[3] = 0;
        triangles[4] = 2;
        triangles[5] = 3;
    }
}