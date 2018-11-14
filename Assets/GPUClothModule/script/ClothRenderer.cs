using UnityEngine;
//[ExecuteInEditMode]
// ReSharper disable once CheckNamespace
public class ClothRenderer : MonoBehaviour {
    public Material mat;
    public Light lightgameObject;
    public MeshTopology meshTopology;
    private ComputeBuffer computeBufferPosition;
    private ComputeBuffer computeBufferVelocity;
    private ComputeBuffer computeBufferTexcoord;
    private ComputeBuffer computeBufferTriangle;
    private ComputeBuffer computeBufferNormal;
    private ComputeBuffer computeBufferForce;
    private ComputeBuffer computeBufferCollisionResult;

    int vertn;
    int vertm;
    int numVertex;

    private Vector4 lightP;
    private Vector3 lightI;

    private ClothSimulation anchor;
    [HideInInspector]
    public bool ready;

    private void linker()
    {
        computeBufferPosition = anchor.computeBufferWorldPosition;
        computeBufferVelocity = anchor.computeBufferVelocity;
        computeBufferTexcoord = anchor.computeBufferTexcoord;
        computeBufferTriangle = anchor.triangleBuffer;
        computeBufferNormal = anchor.normalBuffer;
        computeBufferForce = anchor.forceBuffer;
        vertn = anchor.vertexColumn;
        vertm = anchor.vertexRow;
        numVertex = anchor.numVertex;
    }
    public void controller()
    {
        anchor = GetComponent<ClothSimulation>();
        linker();
        initLight(lightgameObject);
        initMat();
        ready = true;
    }

    private void initMat()
    {
        mat.SetBuffer("Forces",computeBufferForce);
        mat.SetFloat("maxForce", 200.0f);

        mat.SetBuffer("CollisionResult", computeBufferCollisionResult);
        mat.SetBuffer("Position", computeBufferPosition);
        mat.SetBuffer("Velocity", computeBufferVelocity);
        mat.SetBuffer("TC", computeBufferTexcoord);
        mat.SetBuffer("Trimap", computeBufferTriangle);
        mat.SetBuffer("Nor", computeBufferNormal);

        mat.SetInt("vertn", vertn);
        mat.SetInt("vertm", vertm);

        mat.SetVector("Light_P", lightP);
        mat.SetVector("Light_I", lightI);
        mat.SetVector("Kd", new Vector3(1.0f, 1.0f, 1.0f));
        mat.SetVector("Ka", new Vector3(0.4f, 0.4f, 0.4f));
        mat.SetVector("Ks", new Vector3(0.8f, 0.8f, 0.8f));
        mat.SetFloat("Shininess", 256.0f);
    }
    #region initialize Light values
    private void initLight(Light inputLight)
    {
        lightP = new Vector4(inputLight.transform.position.x, inputLight.transform.position.y, inputLight.transform.position.z, 1.0f);
        lightI = new Vector3(inputLight.intensity, inputLight.intensity, inputLight.intensity);
        lightI = new Vector3(1.0f, 1.0f, 1.0f);
    }
    #endregion
    
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Local
    private void OnRenderObject()
    {
        if (!ready) return;
        mat.SetPass(0);
        switch (meshTopology)
        {
            case MeshTopology.Points:
                Graphics.DrawProcedural(meshTopology, numVertex, 1);
                break;
            case MeshTopology.Triangles:
                Graphics.DrawProcedural(meshTopology, (vertn - 1) * (vertm - 1) * 6, 1);
                break;
            case MeshTopology.LineStrip:
                Graphics.DrawProcedural(meshTopology, (vertn - 1) * (vertm - 1) * 6, 1);
                break;
            case MeshTopology.Quads:
                Graphics.DrawProcedural(meshTopology, numVertex / 4, 1);
                break;
            case MeshTopology.Lines:
                break;
            default:
                Debug.Log("unhandled Mesh Topology\n");
                break;
        }
    }
}
