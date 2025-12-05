using UnityEngine;
using UnityEditor;

public class AddRoadColliders
{
    [MenuItem("Tools/Add Mesh Colliders To Selected Roads")]
    public static void AddColliders()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            MeshFilter mf = obj.GetComponent<MeshFilter>();
            if (mf)
            {
                MeshCollider mc = obj.GetComponent<MeshCollider>();
                if (!mc) mc = obj.AddComponent<MeshCollider>();

                mc.sharedMesh = mf.sharedMesh;
                mc.convex = false;
            }
        }

        Debug.Log("MeshColliders added to selected road pieces.");
    }
}
