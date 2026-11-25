namespace WorldTile
{
    using UnityEngine;
    using UnityEngine.Rendering;

    /// <summary>
    /// Bridges a terrain mesh to the GPU grass generator. It never creates
    /// per-blade GameObjects or CPU vertex arrays.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GpuGrassRenderer : MonoBehaviour
    {
        private const string GeneratorResource = "TerrainGrassGenerator";
        private const string BladeShaderResource = "TerrainGrassBlades";
        private const int BladeStride = 64;
        // Full density is kept through 50 m and progressively reduced through
        // the 50-100 m fade ring. This capacity includes headroom for a nearly
        // all-green area at the default density.
        private const int MaxBlades = 262144;
        private const float RebuildDistanceMeters = 2.0f;

        private static readonly int TerrainPositionsId = Shader.PropertyToID("_TerrainPositions");
        private static readonly int TerrainUvsId = Shader.PropertyToID("_TerrainUVs");
        private static readonly int TerrainIndicesId = Shader.PropertyToID("_TerrainIndices");
        private static readonly int GrassBladesId = Shader.PropertyToID("_GrassBlades");
        private static readonly int IndirectArgsId = Shader.PropertyToID("_IndirectArgs");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int LocalToWorldId = Shader.PropertyToID("_LocalToWorld");
        private static readonly int CameraPositionId = Shader.PropertyToID("_CameraPositionWS");
        private static readonly int TriangleCountId = Shader.PropertyToID("_TriangleCount");
        private static readonly int IndexStartId = Shader.PropertyToID("_IndexStart");
        private static readonly int PositionStrideId = Shader.PropertyToID("_PositionStride");
        private static readonly int PositionOffsetId = Shader.PropertyToID("_PositionOffset");
        private static readonly int UvStrideId = Shader.PropertyToID("_UvStride");
        private static readonly int UvOffsetId = Shader.PropertyToID("_UvOffset");
        private static readonly int GrassDistanceId = Shader.PropertyToID("_GrassRenderDistanceWorld");
        private static readonly int GrassFadeStartId = Shader.PropertyToID("_GrassFadeStartWorld");
        private static readonly int GrassMinHeightId = Shader.PropertyToID("_GrassMinHeightWorld");
        private static readonly int GrassMaxHeightId = Shader.PropertyToID("_GrassMaxHeightWorld");
        private static readonly int GrassWidthId = Shader.PropertyToID("_GrassWidthWorld");
        private static readonly int GrassRootLiftId = Shader.PropertyToID("_GrassRootLiftWorld");
        private static readonly int GrassThresholdGRId = Shader.PropertyToID("_GrassThresholdGR");
        private static readonly int GrassThresholdBGId = Shader.PropertyToID("_GrassThresholdBG");
        private static readonly int GrassSoftnessId = Shader.PropertyToID("_GrassSoftness");
        private static readonly int GrassSlopeLimitId = Shader.PropertyToID("_GrassSlopeLimit");
        private static readonly int GrassDensityId = Shader.PropertyToID("_GrassDensity");
        private static readonly int GrassDarknessThresholdId = Shader.PropertyToID("_GrassDarknessThreshold");
        private static readonly int MetersToWorldId = Shader.PropertyToID("_MetersToWorld");
        private static readonly int GrassRenderDistanceMetersId = Shader.PropertyToID("_GrassRenderDistance");
        private static readonly int GrassFadeStartMetersId = Shader.PropertyToID("_GrassFadeStart");
        private static readonly int GrassMinHeightMetersId = Shader.PropertyToID("_GrassMinHeight");
        private static readonly int GrassMaxHeightMetersId = Shader.PropertyToID("_GrassMaxHeight");
        private static readonly int GrassWidthMetersId = Shader.PropertyToID("_GrassWidth");
        private static readonly int GrassRootLiftMetersId = Shader.PropertyToID("_GrassRootLift");

        private static ComputeShader s_generator;
        private static Material s_bladeMaterial;
        private static int s_generateKernel = -1;
        private static int s_finalizeKernel = -1;
        private static int s_cameraFrame = -1;
        private static Camera s_frameCamera;
        private static Camera[] s_cameraBuffer = new Camera[4];
        private static readonly uint[] EmptyIndirectArgs = { 0, 1, 0, 0 };
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static bool s_diagnosticRequested;
        private static bool s_componentActivationLogged;
        private static bool s_fallbackCameraLogged;
#endif

        private MeshFilter _meshFilter;
        private Renderer _terrainRenderer;
        private Texture _satelliteTexture;
        private Mesh _sourceMesh;
        private GraphicsBuffer _positionBuffer;
        private GraphicsBuffer _uvBuffer;
        private GraphicsBuffer _indexBuffer;
        private ComputeBuffer _bladeBuffer;
        private ComputeBuffer _indirectArgs;
        private MaterialPropertyBlock _drawProperties;
        private Vector3 _lastBuildCameraPosition;
        private Matrix4x4 _lastBuildLocalToWorld;
        private bool _hasBuiltGrass;
        private bool _configurationDirty = true;

        private static readonly int PaintTexId = Shader.PropertyToID("_PaintTex");
        private static readonly int HasPaintTexId = Shader.PropertyToID("_HasPaintTex");

        private Texture _paintTexture;

        public void SetPaintMask(Texture paintTexture)
        {
            if (_paintTexture != paintTexture)
            {
                _paintTexture = paintTexture;
                _configurationDirty = true;
            }
        }

        public void Configure(MeshFilter meshFilter, Texture satelliteTexture)
        {
            Mesh nextMesh = meshFilter != null ? meshFilter.sharedMesh : null;
            if (_meshFilter != meshFilter || _sourceMesh != nextMesh)
            {
                ReleaseMeshBuffers();
                _meshFilter = meshFilter;
                _sourceMesh = nextMesh;
                _terrainRenderer = meshFilter != null ? meshFilter.GetComponent<Renderer>() : null;
                _configurationDirty = true;
            }

            if (_satelliteTexture != satelliteTexture)
            {
                _satelliteTexture = satelliteTexture;
                _configurationDirty = true;
            }
        }

        private void OnEnable()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!s_componentActivationLogged)
            {
                s_componentActivationLogged = true;
                Debug.Log("[GpuGrass] Component active; waiting for a visible terrain mesh and satellite texture.");
            }
#endif
        }

        private void LateUpdate()
        {
            Camera camera = GetFrameCamera();
            if (camera == null || _meshFilter == null || _satelliteTexture == null)
                return;

            Mesh currentMesh = _meshFilter.sharedMesh;
            if (currentMesh == null)
                return;
            if (currentMesh != _sourceMesh)
                Configure(_meshFilter, _satelliteTexture);

            if (!EnsureSharedResources())
                return;

            float metersToWorld = Mathf.Max(TileStat.ToUnit, 0.0001f);
            float renderDistance = s_bladeMaterial.GetFloat(GrassRenderDistanceMetersId) * metersToWorld;
            Bounds terrainBounds = CalculateTerrainBounds();

            if (HorizontalSqrDistance(terrainBounds, camera.transform.position) > renderDistance * renderDistance)
            {
                _hasBuiltGrass = false;
                ReleaseInputBufferHandles();
                ReleaseOutputBuffers();
                return;
            }

            // Allocate the append/indirect buffers only for tiles actually near
            // the camera. Distant pooled tiles therefore reserve no grass VRAM.
            if (!EnsureBuffers())
                return;

            Bounds drawBounds = terrainBounds;
            float maximumHeight = s_bladeMaterial.GetFloat(GrassMaxHeightMetersId) * metersToWorld;
            drawBounds.Expand(new Vector3(0.0f, maximumHeight * 2.0f, 0.0f));

            Matrix4x4 localToWorld = _meshFilter.transform.localToWorldMatrix;
            float rebuildDistance = RebuildDistanceMeters * metersToWorld;
            bool cameraMoved = !_hasBuiltGrass ||
                (camera.transform.position - _lastBuildCameraPosition).sqrMagnitude >= rebuildDistance * rebuildDistance;
            bool transformChanged = !_hasBuiltGrass || localToWorld != _lastBuildLocalToWorld;

#if UNITY_EDITOR
            // In editor, rebuild if material parameters change to enable real-time slider updates
            if (!_configurationDirty && _hasBuiltGrass)
            {
                _configurationDirty = true;
            }
#endif

            if (_configurationDirty || cameraMoved || transformChanged)
                GenerateGrass(camera, localToWorld, metersToWorld);

            if (!_hasBuiltGrass)
                return;

            _drawProperties ??= new MaterialPropertyBlock();
            _drawProperties.SetBuffer(GrassBladesId, _bladeBuffer);
            _drawProperties.SetFloat(GrassDistanceId, renderDistance);
            _drawProperties.SetFloat(
                GrassFadeStartId,
                Mathf.Min(s_bladeMaterial.GetFloat(GrassFadeStartMetersId) * metersToWorld, renderDistance));

            Graphics.DrawProceduralIndirect(
                s_bladeMaterial,
                drawBounds,
                MeshTopology.Triangles,
                _indirectArgs,
                0,
                camera,
                _drawProperties,
                ShadowCastingMode.Off,
                false,
                gameObject.layer);
        }

        private void GenerateGrass(Camera camera, Matrix4x4 localToWorld, float metersToWorld)
        {
            if (_sourceMesh == null || _sourceMesh.subMeshCount == 0)
                return;

            SubMeshDescriptor subMesh = _sourceMesh.GetSubMesh(0);
            int triangleCount = subMesh.indexCount / 3;
            if (triangleCount <= 0)
                return;

            float renderDistance = s_bladeMaterial.GetFloat(GrassRenderDistanceMetersId) * metersToWorld;
            float generationDistance = renderDistance + RebuildDistanceMeters * metersToWorld;

            _bladeBuffer.SetCounterValue(0);
            s_generator.SetBuffer(s_generateKernel, TerrainPositionsId, _positionBuffer);
            s_generator.SetBuffer(s_generateKernel, TerrainUvsId, _uvBuffer);
            s_generator.SetBuffer(s_generateKernel, TerrainIndicesId, _indexBuffer);
            s_generator.SetBuffer(s_generateKernel, GrassBladesId, _bladeBuffer);
            s_generator.SetTexture(s_generateKernel, MainTexId, _satelliteTexture);
            if (_paintTexture != null)
            {
                s_generator.SetTexture(s_generateKernel, PaintTexId, _paintTexture);
                s_generator.SetFloat(HasPaintTexId, 1.0f);
            }
            else
            {
                s_generator.SetTexture(s_generateKernel, PaintTexId, Texture2D.blackTexture);
                s_generator.SetFloat(HasPaintTexId, 0.0f);
            }
            s_generator.SetMatrix(LocalToWorldId, localToWorld);
            s_generator.SetVector(CameraPositionId, camera.transform.position);
            s_generator.SetInt(TriangleCountId, triangleCount);
            s_generator.SetInt(IndexStartId, subMesh.indexStart);
            s_generator.SetInt(PositionStrideId, _sourceMesh.GetVertexBufferStride(
                _sourceMesh.GetVertexAttributeStream(VertexAttribute.Position)));
            s_generator.SetInt(PositionOffsetId, _sourceMesh.GetVertexAttributeOffset(VertexAttribute.Position));
            s_generator.SetInt(UvStrideId, _sourceMesh.GetVertexBufferStride(
                _sourceMesh.GetVertexAttributeStream(VertexAttribute.TexCoord0)));
            s_generator.SetInt(UvOffsetId, _sourceMesh.GetVertexAttributeOffset(VertexAttribute.TexCoord0));
            s_generator.SetFloat(GrassDistanceId, generationDistance);
            s_generator.SetFloat(
                GrassFadeStartId,
                Mathf.Min(s_bladeMaterial.GetFloat(GrassFadeStartMetersId) * metersToWorld, generationDistance));
            s_generator.SetFloat(GrassMinHeightId, s_bladeMaterial.GetFloat(GrassMinHeightMetersId) * metersToWorld);
            s_generator.SetFloat(GrassMaxHeightId, s_bladeMaterial.GetFloat(GrassMaxHeightMetersId) * metersToWorld);
            s_generator.SetFloat(GrassWidthId, s_bladeMaterial.GetFloat(GrassWidthMetersId) * metersToWorld);
            s_generator.SetFloat(GrassRootLiftId, s_bladeMaterial.GetFloat(GrassRootLiftMetersId) * metersToWorld);
            if (s_bladeMaterial.HasProperty(GrassThresholdGRId))
                s_generator.SetFloat(GrassThresholdGRId, s_bladeMaterial.GetFloat(GrassThresholdGRId));
            else
                s_generator.SetFloat(GrassThresholdGRId, 0.04f);

            if (s_bladeMaterial.HasProperty(GrassThresholdBGId))
                s_generator.SetFloat(GrassThresholdBGId, s_bladeMaterial.GetFloat(GrassThresholdBGId));
            else
                s_generator.SetFloat(GrassThresholdBGId, 0.04f);
            s_generator.SetFloat(GrassSoftnessId, s_bladeMaterial.GetFloat(GrassSoftnessId));
            s_generator.SetFloat(GrassSlopeLimitId, s_bladeMaterial.GetFloat(GrassSlopeLimitId));
            if (s_bladeMaterial.HasProperty(GrassDarknessThresholdId))
                s_generator.SetFloat(GrassDarknessThresholdId, s_bladeMaterial.GetFloat(GrassDarknessThresholdId));
            else
                s_generator.SetFloat(GrassDarknessThresholdId, 0.03f);
            s_generator.SetFloat(GrassDensityId, Mathf.Clamp(
                s_bladeMaterial.GetFloat(GrassDensityId), 1.0f, 16.0f));
            s_generator.SetFloat(MetersToWorldId, metersToWorld);

            s_generator.Dispatch(s_generateKernel, Mathf.CeilToInt(triangleCount / 64.0f), 1, 1);

            ComputeBuffer.CopyCount(_bladeBuffer, _indirectArgs, 0);
            s_generator.SetBuffer(s_finalizeKernel, IndirectArgsId, _indirectArgs);
            s_generator.Dispatch(s_finalizeKernel, 1, 1, 1);

            _lastBuildCameraPosition = camera.transform.position;
            _lastBuildLocalToWorld = localToWorld;
            _configurationDirty = false;
            _hasBuiltGrass = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RequestSingleDiagnosticReadback(renderDistance / metersToWorld);
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void RequestSingleDiagnosticReadback(float renderDistanceMeters)
        {
            if (s_diagnosticRequested || _indirectArgs == null)
                return;

            s_diagnosticRequested = true;
            AsyncGPUReadback.Request(_indirectArgs, request =>
            {
                if (request.hasError)
                {
                    Debug.LogWarning("[GpuGrass] Diagnostic readback failed; indirect GPU grass is unsupported by the active graphics device.");
                    return;
                }

                uint vertexCount = request.GetData<uint>()[0];
                Debug.Log($"[GpuGrass] GPU generation active: {vertexCount / 3u} blades inside {renderDistanceMeters:0.#} m horizontal radius.");
            });
        }
#endif

        private bool EnsureSharedResources()
        {
            if (!SystemInfo.supportsComputeShaders)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (!s_diagnosticRequested)
                {
                    s_diagnosticRequested = true;
                    Debug.LogWarning("[GpuGrass] Compute shaders are not supported by the active graphics device.");
                }
#endif
                return false;
            }

            if (s_generator == null)
            {
                s_generator = Resources.Load<ComputeShader>(GeneratorResource);
                if (s_generator == null)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogError($"[GpuGrass] Resource '{GeneratorResource}' was not found.");
#endif
                    return false;
                }
                s_generateKernel = s_generator.FindKernel("GenerateGrass");
                s_finalizeKernel = s_generator.FindKernel("FinalizeIndirectArgs");
            }

            if (s_bladeMaterial == null)
            {
                if (TileManager.Instance != null && TileManager.Instance.GpuGrassBladeMaterial != null)
                {
                    s_bladeMaterial = TileManager.Instance.GpuGrassBladeMaterial;
                }
                else
                {
                    Shader shader = Resources.Load<Shader>(BladeShaderResource);
                    if (shader == null)
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.LogError($"[GpuGrass] Resource '{BladeShaderResource}' was not found.");
#endif
                        return false;
                    }
                    s_bladeMaterial = new Material(shader)
                    {
                        name = "GPU Terrain Grass (Shared)",
                        hideFlags = HideFlags.HideAndDontSave
                    };
                }
            }
            return true;
        }

        private bool EnsureBuffers()
        {
            if (_sourceMesh == null || !_sourceMesh.HasVertexAttribute(VertexAttribute.Position) ||
                !_sourceMesh.HasVertexAttribute(VertexAttribute.TexCoord0))
                return false;

            if (_positionBuffer == null)
            {
                try
                {
                    int positionStream = _sourceMesh.GetVertexAttributeStream(VertexAttribute.Position);
                    int uvStream = _sourceMesh.GetVertexAttributeStream(VertexAttribute.TexCoord0);
                    _positionBuffer = _sourceMesh.GetVertexBuffer(positionStream);
                    _uvBuffer = uvStream == positionStream
                        ? _positionBuffer
                        : _sourceMesh.GetVertexBuffer(uvStream);
                    _indexBuffer = _sourceMesh.GetIndexBuffer();
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning($"[GpuGrass] Terrain GPU buffers unavailable: {exception.Message}");
                    ReleaseMeshBuffers();
                    return false;
                }
            }

            _bladeBuffer ??= new ComputeBuffer(MaxBlades, BladeStride, ComputeBufferType.Append);
            if (_indirectArgs == null)
            {
                _indirectArgs = new ComputeBuffer(4, sizeof(uint), ComputeBufferType.IndirectArguments);
                _indirectArgs.SetData(EmptyIndirectArgs);
            }
            return true;
        }

        private Bounds CalculateTerrainBounds()
        {
            return _terrainRenderer != null
                ? _terrainRenderer.bounds
                : new Bounds(_meshFilter.transform.position, Vector3.one);
        }

        private static float HorizontalSqrDistance(Bounds bounds, Vector3 point)
        {
            float distanceX = Mathf.Max(Mathf.Abs(point.x - bounds.center.x) - bounds.extents.x, 0.0f);
            float distanceZ = Mathf.Max(Mathf.Abs(point.z - bounds.center.z) - bounds.extents.z, 0.0f);
            return distanceX * distanceX + distanceZ * distanceZ;
        }

        private static Camera GetFrameCamera()
        {
            if (s_cameraFrame != Time.frameCount)
            {
                s_cameraFrame = Time.frameCount;
                s_frameCamera = Camera.main;

                // The demo scene renders through an enabled "OWT Camera",
                // while its tagged MainCamera object is disabled. Camera.main
                // is therefore null there. Use the actual active game camera
                // without allocating Camera.allCameras every frame.
                if (s_frameCamera == null || !s_frameCamera.isActiveAndEnabled)
                {
                    int cameraCount = Camera.allCamerasCount;
                    if (cameraCount > s_cameraBuffer.Length)
                        System.Array.Resize(ref s_cameraBuffer, Mathf.NextPowerOfTwo(cameraCount));

                    int copiedCount = Camera.GetAllCameras(s_cameraBuffer);
                    s_frameCamera = null;
                    for (int cameraIndex = 0; cameraIndex < copiedCount; ++cameraIndex)
                    {
                        Camera candidate = s_cameraBuffer[cameraIndex];
                        if (candidate == null || !candidate.isActiveAndEnabled ||
                            candidate.cameraType != CameraType.Game)
                            continue;

                        if (s_frameCamera == null || candidate.depth > s_frameCamera.depth)
                            s_frameCamera = candidate;
                    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (s_frameCamera != null && !s_fallbackCameraLogged)
                    {
                        s_fallbackCameraLogged = true;
                        Debug.Log($"[GpuGrass] Using active camera '{s_frameCamera.name}' because no enabled MainCamera exists.");
                    }
#endif
                }
            }
            return s_frameCamera;
        }

        private void ReleaseMeshBuffers()
        {
            ReleaseInputBufferHandles();
            _sourceMesh = null;
            _hasBuiltGrass = false;
        }

        private void ReleaseInputBufferHandles()
        {
            _positionBuffer?.Dispose();
            if (_uvBuffer != null && !ReferenceEquals(_uvBuffer, _positionBuffer))
                _uvBuffer.Dispose();
            _indexBuffer?.Dispose();
            _positionBuffer = null;
            _uvBuffer = null;
            _indexBuffer = null;
        }

        private void ReleaseOutputBuffers()
        {
            _bladeBuffer?.Release();
            _indirectArgs?.Release();
            _bladeBuffer = null;
            _indirectArgs = null;
        }

        private void OnDisable()
        {
            ReleaseMeshBuffers();
            ReleaseOutputBuffers();
        }

        private void OnDestroy()
        {
            ReleaseMeshBuffers();
            ReleaseOutputBuffers();
        }
    }
}
