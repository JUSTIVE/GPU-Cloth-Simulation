using JetBrains.Annotations;
using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
public class ClothSimulation : MonoBehaviour
{
    public struct DispatchDim
    {
        public int x { get; set; }
        public int y { get; set; }
        public int z { get; set; }
    }
    //publics 
    private ClothRenderer clothRendererInstance;

    [Header("Compute Shaders")]
    public ComputeShader nodeUpdateComputeShader;
    public ComputeShader triCollisionResponseComputeShader;
    public ComputeShader normalComputeShader;

    [Header("Attribute of Cloth")]
    public int vertexColumn;
    public int vertexRow;
    
    public Vector2 clothSize;
    public float clothMass = 1024.0f;

    [HideInInspector]
    private float nodeMass;

    [Range(20, 500)] public int looper;

    [Header("Constant for Cloth")]
    public float springK;
    public float damping;
    public Vector3 externalForce;
    //public bool Hang;

    //privates
    //private float[] const_force_max = { 1042.855f, 200, 200, 919.184f };  //used for force visualization, deprecated
    //private float[] const_force_max = { 100.0f, 100.0f, 100.0f, 100.0f }; //used for force visualization, deprecated
    private ComputeBuffer computeBufferPrevPosition { get; set; }
    public ComputeBuffer forceBuffer { get; set; }
    public ComputeBuffer triangleBuffer { get; set; }

    public ComputeBuffer computeBufferWorldPosition { get; set; }

    public ComputeBuffer computeBufferPosition { get; set; }
    public ComputeBuffer computeBufferPositionTemp { get; set; }

    public ComputeBuffer computeBufferVelocity { get; set; }
    public ComputeBuffer computebufferVelocityTemp { get; set; }

    public ComputeBuffer computeBufferTexcoord { get; set; }
    public ComputeBuffer normalBuffer { get; set; }
    public ComputeBuffer trsBuffer { get; set; }

    //computeshaderDim
    private DispatchDim NUCSdispatchDim;
    private DispatchDim TCRCSdispatchDim;

    //computeShaderHandles
    private int nodeUpdateComputeShaderHandle;
    private int triCollisionResponseShaderHandle;
    private int normalComputeShaderHandle;
    private float dx, dy;
    //for cloth
    private Vector3[] vec3Positions;
    private Vector4[] positions;
    private Vector4[] positionTemps;
    private Vector4[] velocities;
    private Vector4[] velocityTemps;
    private Vector2[] texcoords;
    private Vector4[] normals;
    private Vector4[] worldPositions;
    private Matrix4x4[] ClothTRS = new Matrix4x4[2];
    /// <summary>
    /// array for store triangle data
    /// </summary>
    private int[] triangles;
    /// <summary>
    /// 
    /// </summary>
    private int triangleIndex;
    /// <summary>
    /// 정점의 개수
    /// </summary>
    public int numVertex { get; set; }
    public int clothTriangledSerialNodeSize { get; set; }
    public int clothTriangleSize { get; set; }
    //colliders
    /// <summary>
    /// 각각의 ColliderObject의 objectsToBeCollided 길이를 가진 배열
    /// </summary>
    /// <summary>
    /// numbers of spherecollider component
    /// </summary>
    private bool isInitialized;
    private float deltaT;

    private float structualConstraint { get; set; }
    private float shearConstraint { get; set; }

    // ReSharper disable once UnusedMember.Local
    // ReSharper disable once ArrangeTypeMemberModifiers
    // ReSharper disable once InconsistentNaming
    void Start()
    {
        initialize();
        clothRendererInstance = GetComponent<ClothRenderer>();
        clothRendererInstance.controller();
        isInitialized = true;
    }
    //initializing pipeline
    private void initialize()
    {
        deltaT = 0.01f / looper;
        //deltaT = Time.deltaTime/ looper;
        dataInitialize();
        initCompute();
    }
    //compute외에 들어갈 데이터를 초기화하는 함수
    private void dataInitialize()
    {
        initClothData();
    }
    private void initClothData()
    {
        int i, j;
        //calculate numero des vertex
        numVertex = vertexColumn * vertexRow;
        //calculate Node Mass
        nodeMass = clothMass / numVertex;

        vec3Positions = new Vector3[numVertex];
        positions = new Vector4[numVertex];
        positionTemps = new Vector4[numVertex];
        velocities = new Vector4[numVertex];
        velocityTemps = new Vector4[numVertex];
        texcoords = new Vector2[numVertex];
        //distance between particles
        dx = clothSize.x / (vertexColumn - 1);
        dy = clothSize.y / (vertexRow - 1);
        structualConstraint = Math.Max(dx, dy);
        shearConstraint = (float)Math.Sqrt(dx * dx + dy * dx);
        //setup initial datas
        vec3Positions.Initialize();
        positions.Initialize();
        velocities.Initialize();
        texcoords.Initialize();
        //setup initial vertex positions
        for (i = 0; i < vertexRow; i++)
            for (j = 0; j < vertexColumn; j++)
            {
                int index = i * vertexRow + j;
                positions[index] = new Vector4(dx * j - clothSize.x / 2,
                    0.0f,
                    dy * i - clothSize.y / 2,
                    1.0f);
                texcoords[index] = new Vector2((positions[index].x - clothSize.x / 2) / clothSize.x,
                    (positions[index].z - clothSize.y / 2) / clothSize.y);
            }
        //setup for triangle
        clothTriangledSerialNodeSize = (vertexColumn - 1) * (vertexRow - 1) * 6;
        clothTriangleSize = clothTriangledSerialNodeSize/3;
        triangles = new int[clothTriangledSerialNodeSize];
        triangleIndex = 0;
        //front face
        for (i = 0; i < vertexColumn - 1; i++)
            for (j = 0; j < vertexRow - 1; j++, triangleIndex += 3)
            {
                triangles[triangleIndex] = i * vertexColumn + j;
                triangles[triangleIndex + 1] = (i + 1) * vertexColumn + j;
                triangles[triangleIndex + 2] = i * vertexColumn + j + 1;
            }
        for (i = 0; i < vertexColumn - 1; i++)
            for (j = 0; j < vertexRow - 1; j++, triangleIndex += 3)
            {
                triangles[triangleIndex] = i * vertexColumn + j + 1;
                triangles[triangleIndex + 1] = (i + 1) * vertexColumn + j;
                triangles[triangleIndex + 2] = (i + 1) * vertexColumn + j + 1;
            }

        print("tri: " + triangleIndex);
        
        //init normal
        normals = new Vector4[numVertex];
        normals.Initialize();
        //init worldPositions
        worldPositions = new Vector4[numVertex];
        worldPositions.Initialize();
    }

    /// <summary>
    /// compute shader 및 compute buffer들을 초기화하는 메소드들을 호출하는 메소드
    /// </summary>
    private void initCompute()
    {
        initComptueBuffer();
        initNucs();
        initTcrcs();
        initNormal();
    }

    /// <summary>
    /// 모든 compute buffer를 초기화하는 메소드
    /// </summary>
    //position/positionTemp/velocity/velocityTemp/worldPosition/triCollisionResult/collisionResult/TexCoord/normal/objectTriangle/prevPosition/TRS/triangle
    //triCollisionResult/collisionResult/objectTriangle
    private void initComptueBuffer()
    {
        trsBuffer = new ComputeBuffer(8, 16);
        //position
        computeBufferPosition = new ComputeBuffer(numVertex, 16); computeBufferPosition.SetData(positions);
        computeBufferPositionTemp = new ComputeBuffer(numVertex, 16); computeBufferPositionTemp.SetData(positionTemps);
        computeBufferPrevPosition = new ComputeBuffer(numVertex, 16);
        computeBufferWorldPosition = new ComputeBuffer(numVertex, 16);computeBufferWorldPosition.SetData(worldPositions);
        //velo
        computeBufferVelocity = new ComputeBuffer(numVertex, 16); computeBufferVelocity.SetData(velocities);
        computebufferVelocityTemp = new ComputeBuffer(numVertex, 16); computebufferVelocityTemp.SetData(velocityTemps);
        //texcoord
        computeBufferTexcoord = new ComputeBuffer(numVertex, 8); computeBufferTexcoord.SetData(texcoords);
        //clothTriangle
        triangleBuffer = new ComputeBuffer(clothTriangledSerialNodeSize, 4); triangleBuffer.SetData(triangles);
        //normal
        normalBuffer = new ComputeBuffer(numVertex, 16); normalBuffer.SetData(normals); 
    }

    /// <summary>
    ///Node update compute shader를 초기화하는 메소드 
    /// </summary>
    private void initNucs()
    {
        //grab kernel handle
        nodeUpdateComputeShaderHandle = nodeUpdateComputeShader.FindKernel("NodeUpdate");
        //set variables for NUCS
        nodeUpdateComputeShader.SetInt("vertexColumn", vertexColumn);
        nodeUpdateComputeShader.SetInt("vertexRow", vertexRow);
        nodeUpdateComputeShader.SetFloat("RestLengthHoriz", dx);
        nodeUpdateComputeShader.SetFloat("RestLengthVert", dy);
        nodeUpdateComputeShader.SetFloat("RestLengthDiag", Mathf.Sqrt(Mathf.Pow(dx, 2) + Mathf.Pow(dy, 2)));

        nodeUpdateComputeShader.SetVector("ExeternalForce", new Vector4(externalForce.x, externalForce.y, externalForce.z, 1.0f));
        nodeUpdateComputeShader.SetFloat("SpringK", springK);
        nodeUpdateComputeShader.SetFloat("DampingConst", damping);
        nodeUpdateComputeShader.SetFloat("DeltaT", deltaT);
        nodeUpdateComputeShader.SetFloat("DeltaT2", deltaT * deltaT);
        nodeUpdateComputeShader.SetFloat("nodeMass", nodeMass);
        nodeUpdateComputeShader.SetFloat("structualConstraint", structualConstraint);
        nodeUpdateComputeShader.SetFloat("shearConstraint", shearConstraint);
        nodeUpdateComputeShader.SetFloat("structualConstraint1",structualConstraint*1.1f);
        nodeUpdateComputeShader.SetFloat("shearConstraint1", shearConstraint * 1.1f);
        //set buffers for NUCS
        nodeUpdateComputeShader.SetBuffer(nodeUpdateComputeShaderHandle, "Position", computeBufferPosition);
        nodeUpdateComputeShader.SetBuffer(nodeUpdateComputeShaderHandle, "PositionTemp", computeBufferPositionTemp);
        nodeUpdateComputeShader.SetBuffer(nodeUpdateComputeShaderHandle, "prevPosition", computeBufferPrevPosition);
        nodeUpdateComputeShader.SetBuffer(nodeUpdateComputeShaderHandle, "Velocity", computeBufferVelocity);
        nodeUpdateComputeShader.SetBuffer(nodeUpdateComputeShaderHandle, "VelocityTemp", computebufferVelocityTemp);
        //set dispatchdim
        NUCSdispatchDim.x = vertexColumn / 8;
        NUCSdispatchDim.y = vertexRow/ 8;
        NUCSdispatchDim.z = 1;
    }

    /// <summary>
    /// TriResponse comput shader를 초기화하는 메소드
    /// </summary>
    private void initTcrcs()
    {
        //grab kernel handle
        triCollisionResponseShaderHandle = triCollisionResponseComputeShader.FindKernel("TriCollisionResponseShader");
        //set variables for TCRCS
        triCollisionResponseComputeShader.SetInt("vertexColumn", vertexColumn);
        triCollisionResponseComputeShader.SetInt("vertexRow", vertexRow);
        triCollisionResponseComputeShader.SetFloat("DeltaT", deltaT);
        //set buffers for TCRCS
        triCollisionResponseComputeShader.SetBuffer(triCollisionResponseShaderHandle, "WorldPosition", computeBufferWorldPosition);
        triCollisionResponseComputeShader.SetBuffer(triCollisionResponseShaderHandle, "PositionTemp", computeBufferPositionTemp);
        triCollisionResponseComputeShader.SetBuffer(triCollisionResponseShaderHandle, "Position", computeBufferPosition);
        triCollisionResponseComputeShader.SetBuffer(triCollisionResponseShaderHandle, "PrevPosition", computeBufferPrevPosition);

        triCollisionResponseComputeShader.SetBuffer(triCollisionResponseShaderHandle, "Velocity", computeBufferVelocity);
        triCollisionResponseComputeShader.SetBuffer(triCollisionResponseShaderHandle, "VelocityTemp", computebufferVelocityTemp);

        //set dispatchdim
        TCRCSdispatchDim.x = vertexColumn / 16;
        TCRCSdispatchDim.y = vertexRow / 16;
        TCRCSdispatchDim.z = 1;
    }
    /// <summary>
    /// normal compute shader를 초기화하는 메소드
    /// </summary>
    private void initNormal()
    {
        //grab kernel handle
        normalComputeShaderHandle = normalComputeShader.FindKernel("NORM");
        //set variables for NCS
        normalComputeShader.SetInt("vertexColumn", vertexColumn);
        normalComputeShader.SetInt("vertexRow", vertexRow);
        //set buffers for NCS
        normalComputeShader.SetBuffer(normalComputeShaderHandle, "Pos", computeBufferPosition);
        normalComputeShader.SetBuffer(normalComputeShaderHandle, "Nor", normalBuffer);
    }
    /// <summary>현재 모델의 MVP 매트릭스를 긁어옴</summary>
    /// <returns>현재 모델의 MVP</returns>
    private Matrix4x4 getModelMatrix()
    {
        Matrix4x4 returnMatrix = transform.localToWorldMatrix;//= this.transform.localToWorldMatrix;
        if (!transform.parent) return returnMatrix;
        Transform currentTransform = transform.parent;
        while (!currentTransform.parent)
        {
            currentTransform = transform;
            returnMatrix = currentTransform.localToWorldMatrix * returnMatrix;
            currentTransform = transform.parent;
        }
        return returnMatrix;
    }
    
    /// <summary>
    /// main.OnDestroy()
    /// Releases all the compute shaders used in this project
    /// </summary>
    // ReSharper disable once UnusedMember.Local
    // ReSharper disable once InconsistentNaming
    private void OnDestroy()
    {
        computeBufferPosition.Release();
        computeBufferWorldPosition.Release();
        computebufferVelocityTemp.Release();
        computeBufferPositionTemp.Release();
        computeBufferTexcoord.Release();
        computeBufferVelocity.Release();
        normalBuffer.Release();
        triangleBuffer.Release();
        trsBuffer.Release();
        computeBufferPrevPosition.Release();
    }
    /// <summary>
    /// node의 간격과 SphereCollider에 의한 Penetration을 방지하기 위한 Offset을 계산하는 메소드
    /// </summary>
    /// <param name="sc">offset이 계산되어질 objectsToBeCollided</param>
    /// <returns>계산되어진 offset 값</returns>
    // ReSharper disable once UnusedMember.Local
    private float precomputeSphereOffset([NotNull] SphereCollider sc)
    {
        if (!sc) throw new ArgumentNullException("sc");
        float rad = sc.GetComponent<SphereCollider>().radius * sc.transform.localScale.x;
        return rad * rad / Mathf.Sqrt(rad * rad - dx*dx / 4) - rad;
    }
    /// <summary>
    /// ComputeBuffer에 4x4 행렬을 담기 위해 변형하는 함수
    /// </summary>
    /// <param name="input">변형될 4x4 행렬</param>
    /// <returns>배열로 변환된 행렬</returns>
    private static Vector4[] mat4X4ToFloatArray(Matrix4x4 input)
    {
        Vector4[] returnArray = new Vector4[4];
        returnArray[0] = new Vector4(input.m00, input.m01, input.m02, input.m03);
        returnArray[1] = new Vector4(input.m10, input.m11, input.m12, input.m13);
        returnArray[2] = new Vector4(input.m20, input.m21, input.m22, input.m23);
        returnArray[3] = new Vector4(input.m30, input.m31, input.m32, input.m33);
        return returnArray;
    }
    /// <summary>
    /// Cloth GameObject의 MVP를 업데이트하여 컴퓨트버퍼의 데이터를 갱신하는 메소드
    /// </summary>
    private void updateClothTrs()
    {
        ClothTRS[0] = getModelMatrix();
        ClothTRS[1] = ClothTRS[0].inverse;
        Vector4[] tempArray = new Vector4[8];
        mat4X4ToFloatArray(ClothTRS[0]).CopyTo(tempArray, 0);
        mat4X4ToFloatArray(ClothTRS[1]).CopyTo(tempArray, 4);
        trsBuffer.SetData(tempArray);
    }

    // ReSharper disable once UnusedMember.Local
    // ReSharper disable once InconsistentNaming
    private void Update()
    {
        looper = 1;
        if (!isInitialized) return;
        updateClothTrs();
        nodeUpdateComputeShader.SetBuffer(nodeUpdateComputeShaderHandle,"trsMatrix", trsBuffer);
        triCollisionResponseComputeShader.SetBuffer(triCollisionResponseShaderHandle,"trsMatrix",trsBuffer);
        normalComputeShader.SetBuffer(normalComputeShaderHandle, "trsMatrix",trsBuffer);
        
#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlaying) return;
#endif
        for (int i = 0; i < looper; i++)
        {
            nodeUpdateComputeShader.Dispatch(nodeUpdateComputeShaderHandle, NUCSdispatchDim.x, NUCSdispatchDim.y, NUCSdispatchDim.z);
            triCollisionResponseComputeShader.Dispatch(triCollisionResponseShaderHandle, TCRCSdispatchDim.x, TCRCSdispatchDim.y, TCRCSdispatchDim.z);
        }
        normalComputeShader.Dispatch(normalComputeShaderHandle, vertexColumn / 8, vertexRow / 8, 1);
    }
}

