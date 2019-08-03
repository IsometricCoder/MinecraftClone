﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldChunk : MonoBehaviour
{
    public bool highlight = true;
    public ChunkManager chunkManager;

    public Texture2D blockAtlas;
    public Material chunkOpaqueMaterial;
    public Material chunkFadeMaterial;

    public Vector3Int _size = Vector3Int.one * 16;

    protected Vector3Int _minCorner;
    protected Block[,,] _blocks;
    protected AtlasReader _atlasReader;

    public void Initialize(Vector3Int minCorner, Vector3Int size)
    {
        _size = size;
        _minCorner = minCorner;

        this.transform.position = minCorner;
        this.gameObject.isStatic = true;

        _blocks = new Block[size.x, size.y, size.z];

        _atlasReader = new AtlasReader(blockAtlas, 32);

        for (int x = 0; x < _size.x; x++)
        {
            for (int z = 0; z < _size.z; z++)
            {
                for (int y = 0; y < _size.y; y++)
                {
                    BlockType type = GetBlockType(x + _minCorner.x, y + _minCorner.y, z + _minCorner.z);

                    if (type != null)
                    {
                        _blocks[x, y, z] = new Block(type);
                    }
                }
            }
        }

        Debug.Log("Initialized Blocks in Chunk");
    }

    public static BlockType GetBlockType(Vector3 pos)
    {
        int x = Mathf.RoundToInt(pos.x);
        int y = Mathf.RoundToInt(pos.y);
        int z = Mathf.RoundToInt(pos.z);
        return GetBlockType(x,y,z);
    }

    public BlockType ReadBlockType(Vector3Int worldPos)
    {
        Vector3Int localPos = WorldToLocalPosition(worldPos);
        Block block = _blocks[localPos.x, localPos.y, localPos.z];
        BlockType type = block == null ? BlockType.GetBlockType("Air") : block.type;
        return type;
    }

    public static BlockType GetBlockType(int x, int y, int z)
    {
        Vector2Int offset = new Vector2Int(10000, 100000);
        int noise = Mathf.FloorToInt(Mathf.PerlinNoise(1 * (x) / (float)32 + offset.x, 1 * (z) / (float)32 + offset.y) * 30);
        noise += 30;

        BlockType type;

        int waterLevel = 35;

        if (y <= 0)
        {
            type = BlockType.GetBlockType("Bedrock");
        }
        else if (y < noise - 3)
        {
            type = BlockType.GetBlockType("Stone");

            if (Random.value < 0.1f)
            {
                type = BlockType.GetBlockType("Iron Ore");
            }

        }
        else if (y < noise)
        {
            type = BlockType.GetBlockType("Dirt");
        }
        else if (y == noise && y > waterLevel)
        {
            type = BlockType.GetBlockType("Grass");
        }
        else if (y == noise && y <= waterLevel)
        {
            type = BlockType.GetBlockType("Sand");
        }
        else if (y <= waterLevel)
        {
            type = BlockType.GetBlockType("Water");
        }
        else
        {
            type = null;
        }

        return type;
    }

    public void BuildMesh()
    {
        BuildOpaqueMesh();
        BuildWaterMesh();
    }

    public Mesh BuildOpaqueMesh()
    {
        bool[,,] isExternalBlock = GetExternalBlockMatrix();

        List<Mesh> meshes = new List<Mesh>();
        List<Matrix4x4> translations = new List<Matrix4x4>();

        for (int i = 0; i < _size.x; i++)
        {
            for (int j = 0; j < _size.y; j++)
            {
                for (int k = 0; k < _size.z; k++)
                {
                    Block block = _blocks[i, j, k];

                    if (isExternalBlock[i,j,k] && block.IsTransparent() == false)
                    {
                        bool[] visibility = GetVisibility(i,j,k);
                        
                        Mesh mesh = block.GenerateFaces(visibility, _atlasReader);
                        meshes.Add(mesh);

                        translations.Add(Matrix4x4.Translate(new Vector3(i, j, k)));
                    }
                }
            }
        }

        Mesh final = new Mesh();
        CombineInstance[] combine = new CombineInstance[meshes.Count];

        for (int i = 0; i < combine.Length; i++)
        {
            combine[i].mesh = meshes[i];
            combine[i].transform = translations[i];
        }

        final.CombineMeshes(combine, true);

        string childName = "Mesh (Opaque)";
        GameObject go = transform.Find(childName)?.gameObject;
        if (go == null)
        {
            go = new GameObject(childName)
            {
                isStatic = true
            };
            go.transform.parent = this.transform;
            go.transform.localPosition = new Vector3(0, 0, 0);
        }

        MeshFilter mf = go.GetComponent<MeshFilter>();
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        MeshCollider mc = go.GetComponent<MeshCollider>();

        mf = mf == null ? go.AddComponent<MeshFilter>() : mf;
        mr = mr == null ? go.AddComponent<MeshRenderer>() : mr;
        mc = mc == null ? go.AddComponent<MeshCollider>() : mc;

        mf.sharedMesh = final;
        mr.sharedMaterial = chunkOpaqueMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        mc.sharedMesh = mf.sharedMesh;

        return final;
    }

    public Mesh BuildWaterMesh()
    {
        // TODO: Store this from other build
        bool[,,] isExternalBlock = GetExternalBlockMatrix();

        List<Mesh> meshes = new List<Mesh>();
        List<Matrix4x4> translations = new List<Matrix4x4>();

        for (int i = 0; i < _size.x; i++)
        {
            for (int j = 0; j < _size.y; j++)
            {
                for (int k = 0; k < _size.z; k++)
                {
                    Block block = _blocks[i, j, k];

                    if (isExternalBlock[i, j, k] && block.type != null && block.type.name == "Water")
                    {
                        bool[] visibility = GetVisibility(i, j, k);

                        Mesh mesh = block.GenerateFaces(visibility, _atlasReader);
                        meshes.Add(mesh);

                        translations.Add(Matrix4x4.Translate(new Vector3(i, j, k)));
                    }
                }
            }
        }

        Mesh final = new Mesh();
        CombineInstance[] combine = new CombineInstance[meshes.Count];

        for (int i = 0; i < combine.Length; i++)
        {
            combine[i].mesh = meshes[i];
            combine[i].transform = translations[i];
        }

        final.CombineMeshes(combine, true);

        string childName = "Mesh (Water)";
        GameObject go = transform.Find(childName)?.gameObject;
        if (go == null)
        {
            go = new GameObject(childName)
            {
                isStatic = true
            };
            go.transform.parent = this.transform;
            go.transform.localPosition = new Vector3(0, -0.0625f, 0);// Shift down to create gap between shore and the water's surface.
            go.tag = "Water";
        }

        //GameObject go = new GameObject("Mesh (Water)");
        //go.isStatic = true;
        //go.transform.parent = this.transform;
        //go.transform.localPosition = new Vector3(0, -0.0625f, 0); // Shift down to create gap between shore and the water's surface.
        MeshFilter mf = go.GetComponent<MeshFilter>();
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        MeshCollider mc = go.GetComponent<MeshCollider>();

        mf = mf == null ? go.AddComponent<MeshFilter>() : mf;
        mr = mr == null ? go.AddComponent<MeshRenderer>() : mr;
        mc = mc == null ? go.AddComponent<MeshCollider>() : mc;

        mf.sharedMesh = final;
        mr.sharedMaterial = chunkFadeMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        mc.sharedMesh = mf.sharedMesh;
        mc.convex = true; // TODO: Temporary fix
        mc.isTrigger = true;

        return final;
    }

    public bool[,,] GetExternalBlockMatrix()
    {
        bool[,,] isExternalBlock = new bool[_size.x, _size.y, _size.z];

        for (int i = 0; i < _size.x; i++)
        {
            for (int j = 0; j < _size.y; j++)
            {
                for (int k = 0; k < _size.z; k++)
                {
                    if (_blocks[i,j,k] == null)
                    {
                        // Don't render air blocks
                        continue;
                    }

                    List<Vector3Int> neighbors = GetNeighbors(i, j, k);

                    if (neighbors.Count < 6)
                    {
                        isExternalBlock[i, j, k] = true;
                        continue;
                    }

                    bool hasTransparentNeighbor = false;
                    foreach (Vector3Int coord in neighbors)
                    {
                        Block block = _blocks[coord.x, coord.y, coord.z];
                        if (block == null || block.IsTransparent())
                        {
                            hasTransparentNeighbor = true;
                            break;
                        }
                    }

                    isExternalBlock[i, j, k] = hasTransparentNeighbor;
                }
            }
        }

        return isExternalBlock;
    }

    protected List<Vector3Int> GetNeighbors(int i, int j, int k)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>();
        if (i != 0)
        {
            neighbors.Add(new Vector3Int(i - 1, j, k));
        }
        if (i != _size.x - 1)
        {
            neighbors.Add(new Vector3Int(i + 1, j, k));
        }

        if (j != 0)
        {
            neighbors.Add(new Vector3Int(i, j - 1, k));
        }
        if (j != _size.y - 1)
        {
            neighbors.Add(new Vector3Int(i, j + 1, k));
        }

        if (k != 0)
        {
            neighbors.Add(new Vector3Int(i, j, k - 1));
        }
        if (k != _size.z - 1)
        {
            neighbors.Add(new Vector3Int(i, j, k + 1));
        }

        return neighbors;
    }

    protected bool[] GetVisibility(int i, int j, int k)
    {
        bool[] visibility = new bool[6];

        BlockType type = _blocks[i, j, k].type;

        // Up, Down, Front, Back, Left, Right
        Vector3Int[] neighbors = {
            new Vector3Int(i,j+1,k),
            new Vector3Int(i,j-1,k),
            new Vector3Int(i+1,j,k),
            new Vector3Int(i-1,j,k),
            new Vector3Int(i,j,k+1),
            new Vector3Int(i,j,k-1)
        };

        for (int ni = 0; ni < neighbors.Length; ni++)
        {
            Vector3Int npos = neighbors[ni];
            BlockType ntype;
            if (LocalPositionIsInRange(npos))
            {
                Block block = _blocks[npos.x, npos.y, npos.z];
                ntype = block == null ? null : block.type;
            } else
            {
                Vector3Int worldPos = LocalToWorldPosition(npos);
                WorldChunk chunkNeighbor = chunkManager.GetNearestChunk(worldPos);
                if (chunkNeighbor != null)
                {
                    ntype = chunkNeighbor.ReadBlockType(worldPos);
                }
                else
                {
                    ntype = GetBlockType(worldPos);
                }

            }
            visibility[ni] = (ntype == null || ntype.isTransparent) && (type != ntype);
        }

        return visibility;
    }

    private Vector3Int WorldToLocalPosition(Vector3Int worldPos)
    {
        return worldPos - _minCorner;
    }

    private Vector3Int LocalToWorldPosition(Vector3Int localPos)
    {
        return localPos + _minCorner;
    }

    private bool LocalPositionIsInRange(Vector3Int localPos)
    {
        return localPos.x >= 0 && localPos.z >= 0 && localPos.x < _size.x && localPos.z < _size.z && localPos.y >= 0 && localPos.y < _size.y;
    }

    public void UpdateBlocks(List<Vector3Int> positions, List<Block> newBlocks)
    {
        bool shouldRebuild = false;

        for (int i = 0; i < positions.Count; i++)
        {
            Vector3Int worldPos = positions[i];
            Vector3Int localPos = WorldToLocalPosition(worldPos);
            if (LocalPositionIsInRange(localPos))
            {
                Block newBlock = newBlocks[i].type == null ? null : newBlocks[i];
                Block currBlock = _blocks[localPos.x, localPos.y, localPos.z];

                BlockType currType = currBlock == null ? null : currBlock.type;
                BlockType newType = newBlock == null ? null : newBlock.type;
                if (currType != newType)
                {
                    _blocks[localPos.x, localPos.y, localPos.z] = newBlock;
                    shouldRebuild = true;
                }
            }
            
        }

        if (shouldRebuild)
        {
            Debug.Log("Rebuilding...");
            //Transform child = transform.Find("Mesh (Opaque)");
            //child.name = "TO DESTROY";
            //Destroy(child.gameObject);
            BuildMesh(); //TODO: Add chunk to build queue instead
        }
    }

    public Vector3 Center()
    {
        return new Vector3(_size.x / 2f + _minCorner.x, _size.y / 2f + _minCorner.y, _size.z / 2f + _minCorner.z);
    }

    public void Highlight(Color color)
    {
        Vector3 chunkCenter = Center();


        Gizmos.color = color;
        Gizmos.DrawWireCube(chunkCenter, _size);
        //Debug.DrawLine(chunkCenter + Vector3.down * 20, chunkCenter + Vector3.up * 20, color);
    }

    private void OnDrawGizmos()
    {
        if (highlight)
        {
            Highlight(Color.red);
        }
    }

}
