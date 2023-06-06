using System;
using UnityEngine;
using System.Collections;
using UnityEngine.Assertions;
using System.Collections.Generic;

public class SSTDManager : MonoBehaviour
{
    #region Singleton Initialization
    private static SSTDManager instance;

    public static SSTDManager Get
    {
        get
        {
            if (instance == null)
            {
                GameObject g = new GameObject("SSTDManager");
                instance = g.AddComponent<SSTDManager>();

                DontDestroyOnLoad(g);
            }
            return instance;
        }
    }
    #endregion

    private int m_Gid = 1;
    private int m_RenderQueue = 2001;
    private List<GameObject> m_Primitives = new List<GameObject>();
    private Dictionary<Renderer, bool> m_RegisteredMeshes = new Dictionary<Renderer, bool>();

    public void RegisterMesh(GameObject gameObject)
    {
        m_RegisteredMeshes.Add(gameObject.GetComponent<Renderer>(), gameObject.GetComponent<SkinnedMeshRenderer>() == null ? false : true);
    }

    public GameObject CreatePrimitive(PrimitiveType type, GameObject tool, Color color, Texture texture = null, bool animate = true)
    {
        // Initialize base primitive gameObject
        GameObject primitive = InitializePrimitive(tool);

        // Create primitive gameObjects
        GameObject primitive0 = SpawnPrimitive(primitive, type);
        GameObject primitive1 = SpawnPrimitive(primitive, type);

        return PreparePrimitive(tool, primitive, primitive0, primitive1, color, texture, animate);
    }

    public GameObject CreatePrimitive(UnityEngine.Object primitiveObject, GameObject tool, Color color, Texture texture = null, bool animate = true)
    {
        // Initialize base primitive gameObject
        GameObject primitive = InitializePrimitive(tool);

        // Create primitive gameObjects
        GameObject primitive0 = SpawnPrimitive(primitive, primitiveObject);
        GameObject primitive1 = SpawnPrimitive(primitive, primitiveObject);

        return PreparePrimitive(tool, primitive, primitive0, primitive1, color, texture, animate);
    }

    public void ContinuousDeformation(GameObject tool, ref Vector3 previousToolPosition, float threshold, Action action)
    {
        if (Vector3.Distance(tool.transform.position, previousToolPosition) >= threshold)
        {
            previousToolPosition = tool.transform.position;
            action();
        }
    }

    public bool CheckThreshold(GameObject tool, ref Vector3 previousToolPosition, float threshold)
    {
        if (Vector3.Distance(tool.transform.position, previousToolPosition) >= threshold)
        {
            previousToolPosition = tool.transform.position;
            return true;
        }
        return false;
    }

    public void ExtendPrimitiveMesh(GameObject primitive)
    {
        MeshFilter[] meshFilters0 = new MeshFilter[] { m_Primitives[0].transform.GetChild(0).gameObject.GetComponent<MeshFilter>(), m_Primitives[1].transform.GetChild(0).gameObject.GetComponent<MeshFilter>() };
        MeshFilter[] meshFilters1 = new MeshFilter[] { m_Primitives[0].transform.GetChild(1).gameObject.GetComponent<MeshFilter>(), m_Primitives[1].transform.GetChild(1).gameObject.GetComponent<MeshFilter>() };

        CombineInstance[] combine0 = new CombineInstance[meshFilters0.Length];
        CombineInstance[] combine1 = new CombineInstance[meshFilters1.Length];

        int i = 0;
        while (i < meshFilters0.Length)
        {
            combine0[i].mesh = meshFilters0[i].sharedMesh;
            combine1[i].mesh = meshFilters1[i].sharedMesh;

            combine0[i].transform = meshFilters0[i].transform.localToWorldMatrix;
            combine1[i].transform = meshFilters1[i].transform.localToWorldMatrix;

            meshFilters0[i].gameObject.SetActive(false);
            meshFilters1[i].gameObject.SetActive(false);

            i++;
        }

        m_Primitives[0].transform.GetChild(0).gameObject.GetComponent<MeshFilter>().mesh = new Mesh();
        m_Primitives[0].transform.GetChild(0).gameObject.GetComponent<MeshFilter>().mesh.CombineMeshes(combine0, true);
        m_Primitives[0].transform.GetChild(0).gameObject.SetActive(true);

        m_Primitives[0].transform.GetChild(1).gameObject.GetComponent<MeshFilter>().mesh = new Mesh();
        m_Primitives[0].transform.GetChild(1).gameObject.GetComponent<MeshFilter>().mesh.CombineMeshes(combine1, true);
        m_Primitives[0].transform.GetChild(1).gameObject.SetActive(true);
    }

    public enum Face
    {
        Top = 6,
        Bottom = 18
    }

    public void DeleteCubeFace(GameObject gameObject, Face face)
    {
        var meshFilter = gameObject.GetComponent<MeshFilter>();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        meshFilter.mesh.GetVertices(vertices);
        meshFilter.mesh.GetTriangles(triangles, 0);

        int index = (int)face;

        triangles[index] = 0;
        triangles[index + 1] = 0;
        triangles[index + 2] = 0;

        triangles[index + 3] = 0;
        triangles[index + 4] = 0;
        triangles[index + 5] = 0;

        meshFilter.mesh.vertices = vertices.ToArray();
        meshFilter.mesh.triangles = triangles.ToArray();

        meshFilter.mesh.RecalculateBounds();
        meshFilter.mesh.RecalculateNormals();
        meshFilter.mesh.RecalculateTangents();
    }

    public void SwapRenderQueues()
    {
        int back_index = 1;

        for (int i = 0; i < Mathf.Ceil(m_Primitives.Count / 2f); i++)
        {
            GameObject prim00 = m_Primitives[i].transform.GetChild(0).gameObject;
            GameObject prim01 = m_Primitives[i].transform.GetChild(1).gameObject;

            GameObject prim10 = m_Primitives[m_Primitives.Count - back_index].transform.GetChild(0).gameObject;
            GameObject prim11 = m_Primitives[m_Primitives.Count - back_index].transform.GetChild(1).gameObject;

            int prev00 = prim00.GetComponent<Renderer>().materials[0].renderQueue;
            int prev01 = prim01.GetComponent<Renderer>().material.renderQueue;

            prim00.GetComponent<Renderer>().materials[0].renderQueue = prim10.GetComponent<Renderer>().material.renderQueue;
            prim00.GetComponent<Renderer>().materials[1].renderQueue = prim10.GetComponent<Renderer>().material.renderQueue;
            prim01.GetComponent<Renderer>().material.renderQueue = prim11.GetComponent<Renderer>().material.renderQueue;

            prim10.GetComponent<Renderer>().materials[0].renderQueue = prev00;
            prim10.GetComponent<Renderer>().materials[1].renderQueue = prev00;
            prim11.GetComponent<Renderer>().material.renderQueue = prev01;

            back_index++;
        }
    }

    public GameObject FindIntersectingMesh(Renderer rend)
    {
        foreach (var mesh in m_RegisteredMeshes)
        {
            if (rend.bounds.Intersects(mesh.Key.bounds))
            {
                if (mesh.Value)
                    return FindBone(mesh.Key as SkinnedMeshRenderer, rend);

                return mesh.Key.gameObject;
            }
        }

        Debug.LogError("Error: Cannot find intersecting renderer bounds specified.");

        return null;
    }

    private GameObject SpawnPrimitive(GameObject primitive, PrimitiveType type)
    {
        // Create primitive gameObject
        GameObject primitive0 = GameObject.CreatePrimitive(type);
        primitive0.transform.SetParent(primitive.transform);
        primitive0.transform.localPosition = new Vector3(0f, 0f, 0f);
        primitive0.transform.localRotation = new Quaternion();
        primitive0.transform.localScale = new Vector3(1f, 1f, 1f);

        // Remove box collider component
        Destroy(primitive0.GetComponent<Collider>());

        return primitive0;
    }

    private GameObject SpawnPrimitive(GameObject primitive, UnityEngine.Object primitiveObject)
    {
        // Create primitive gameObject
        GameObject primitive0 = Instantiate(primitiveObject as GameObject);
        primitive0.transform.SetParent(primitive.transform);
        primitive0.transform.localPosition = new Vector3(0f, 0f, 0f);
        primitive0.transform.localRotation = new Quaternion();
        primitive0.transform.localScale = new Vector3(1f, 1f, 1f);

        // Remove box collider component
        Destroy(primitive0.GetComponent<Collider>());

        return primitive0;
    }

    private IEnumerator AnimatePrimitive(GameObject primitive, Transform parent, Transform tr)
    {
        float target = tr.lossyScale.z;
        primitive.transform.SetParent(null);
        primitive.transform.localScale = new Vector3(tr.lossyScale.x, tr.lossyScale.y, 0f);

        while (primitive.transform.lossyScale.z < target)
        {
            primitive.transform.localScale += new Vector3(0f, 0f, 0.00035f);
            yield return new WaitForSeconds(0.1f);
        }

        primitive.transform.localScale = new Vector3(tr.lossyScale.x, tr.lossyScale.y, tr.lossyScale.z);
        primitive.transform.SetParent(parent);
    }

    private GameObject FindBone(SkinnedMeshRenderer smr, Renderer rend)
    {
        float closestPos = 10000f;
        Vector3 tip = rend.gameObject.transform.GetChild(0).position;
        Transform closestBone = null;

        foreach (Transform child in smr.bones)
        {
            if (Vector3.Distance(child.position, tip) < closestPos)
            {
                closestPos = Vector3.Distance(child.position, tip);
                closestBone = child;
            }
        }

        float dist_a = Vector3.Distance(closestBone.position, tip);
        float dist_b = Vector3.Distance(closestBone.parent.position, tip);
        float dist_c = Vector3.Distance(closestBone.position, closestBone.parent.position);

        if (dist_a <= dist_c && dist_b <= dist_c)
            return closestBone.parent.gameObject;

        return closestBone.gameObject;
    }

    private Material CreateMaterial(string shaderName, Color color = new Color())
    {
        Shader shader = LoadShader(shaderName);
        Material material = new Material(shader);

        material.SetInt("_Gid", m_Gid);
        material.SetColor("_Color", color);
        material.renderQueue = m_RenderQueue++;

        return material;
    }

    private Shader LoadShader(string name)
    {
        Shader shader = Shader.Find(name);

        if (shader != null)
            return shader;

        Debug.LogError("Error: Cannot find shader specified: " + name);
        Assert.IsNotNull<Shader>(shader);

        return null;
    }

    private void IncrementGid() { m_Gid++; }

    private GameObject InitializePrimitive(GameObject tool)
    {
        // Create empty gameObject
        GameObject primitive = new GameObject("PrimitiveGameObject");

        Transform tr = tool.transform.Find("Tip");

        // Position gameObject
        primitive.transform.localPosition = new Vector3(tr.position.x, tr.position.y, tr.position.z);
        primitive.transform.localScale = new Vector3(tr.lossyScale.x, tr.lossyScale.y, tr.lossyScale.z);
        primitive.transform.localRotation = tr.rotation;

        return primitive;
    }

    private GameObject PreparePrimitive(GameObject tool, GameObject primitive, GameObject primitive0, GameObject primitive1, Color color, Texture texture, bool animate)
    {
        GameObject parentGO = FindIntersectingMesh(tool.GetComponent<Renderer>());

        if (parentGO == null)
        {
            Destroy(primitive);
            return null;
        }

        Transform parent = parentGO.transform;
        Transform tr = tool.transform.Find("Tip");

        // Assign the materials to the renderers
        primitive0.GetComponent<Renderer>().materials = new Material[2] { CreateMaterial("Custom/hole_prepare_front_urp_shader"), CreateMaterial("Custom/hole_prepare_back_urp_shader") };
        primitive1.GetComponent<Renderer>().material = CreateMaterial("Custom/hole_shader_urp_simple_phong", color);

        if (texture != null)
            primitive1.GetComponent<Renderer>().material.SetTexture("_MainTex", texture);

        // Disable shadows
        primitive0.GetComponent<Renderer>().receiveShadows = false;
        primitive1.GetComponent<Renderer>().receiveShadows = false;
        primitive0.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        primitive1.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Increment gid for stencil test
        IncrementGid();

        // Add created primitve as a child of the intersecting registed mesh
        primitive.transform.SetParent(parent, true);

        m_Primitives.Add(primitive);

        if (animate)
            StartCoroutine(AnimatePrimitive(primitive, parent, tr));

        return primitive;
    }
 }
