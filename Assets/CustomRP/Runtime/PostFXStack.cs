
using UnityEngine;
using UnityEngine.Rendering;

enum Pass
{
    BloomHorizontal,
    BloomVertical,
    BloomCombine,
    BloomPrefilter,
    Copy
}

public partial class PostFXStack 
{
    const int maxBloomPyramidLevels = 16;
    int bloomPyrmidId;
    public bool IsActive => settings != null;
    const string bufferName = "Post FX";
    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };
    int fxSourceId = Shader.PropertyToID("_PostFXSource");
    int fxSource2Id = Shader.PropertyToID("_PostFXSource2");
    int bloomPreFilterId = Shader.PropertyToID("_BloomPrefilter");
    int bloomBucibicUpsamplingId = Shader.PropertyToID("_BloomBicubicUpsampling");
    int bloomThresholdId = Shader.PropertyToID("_BloomThreshold");
    ScriptableRenderContext context;
    Camera camera;
    PostFXSetting settings;
    
    public PostFXStack()
    {
        bloomPyrmidId = Shader.PropertyToID("_BloomPyrmid0");
        for(int i=1;i< maxBloomPyramidLevels*2; i++)
        {
            Shader.PropertyToID("_BloomPyrmid"+i);
        }
    }
    public void Setup(ScriptableRenderContext context,Camera camera,PostFXSetting settings)
    {
        
        this.context = context;
        this.camera = camera;
        this.settings =camera.cameraType<=CameraType.SceneView? settings:null;
        ApplySceneViewState();
    }
    
    public void Render(int sourceId)
    {

        DoBloom(sourceId);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    private void Draw(RenderTargetIdentifier form,RenderTargetIdentifier to,Pass pass)
    {
        buffer.SetGlobalTexture(fxSourceId, form);
        buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity,settings.Material,(int)pass,MeshTopology.Triangles,3);

    }
    void DoBloom(int sourceId)
    {
        buffer.BeginSample("Bloom");
       
        PostFXSetting.BloomSettings bloomSettings = settings.Bloom;
      
        int width = camera.pixelWidth / 2;
        int height = camera.pixelHeight / 2;
      

        if (bloomSettings.maxIterations==0
           ||width<bloomSettings.downscaleLimit*2||height<bloomSettings.downscaleLimit*2)
        {
            Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
            buffer.EndSample("Bloom");
            return;
        }
        Vector4 threshold;
        threshold.x = Mathf.GammaToLinearSpace(bloomSettings.threshold);
        threshold.y = threshold.x * bloomSettings.thresholdKnee;
        threshold.z = 2.0f * threshold.y;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        buffer.SetGlobalVector(bloomThresholdId, threshold);
        RenderTextureFormat format = RenderTextureFormat.Default;
        buffer.GetTemporaryRT(bloomPreFilterId, width, height, 0, FilterMode.Bilinear, format);
        Draw(sourceId, bloomPreFilterId, Pass.BloomPrefilter);

        width /= 2;
        height /=  2;
        int formId = bloomPreFilterId;
        int toId = bloomPyrmidId+1;
        int i;
        for (i = 0; i < bloomSettings.maxIterations; i++)
        { 
            if (width < bloomSettings.downscaleLimit || height < bloomSettings.downscaleLimit)
            {
                break;
            }
            int midId = toId - 1;
            buffer.GetTemporaryRT(midId, width, height, 0, FilterMode.Bilinear, format);
            buffer.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, format);
            Draw(formId, midId, Pass.BloomHorizontal);
            Draw(midId, toId, Pass.BloomVertical);
            formId = toId;
            toId += 2;
            width /= 2;
            height /= 2;
        }
        buffer.ReleaseTemporaryRT(bloomPreFilterId);
        buffer.SetGlobalFloat(bloomBucibicUpsamplingId, settings.bicubicUpsampling ? 1f : 0f);
        if (i > 1)
        {
            buffer.ReleaseTemporaryRT(formId - 1);
            toId -= 5;
            for (i -= 1; i >= 0; i--)
            {
                buffer.SetGlobalTexture(fxSource2Id, toId + 1);
                Draw(formId, toId, Pass.BloomCombine);

                buffer.ReleaseTemporaryRT(formId);
                buffer.ReleaseTemporaryRT(toId + 1);
                formId = toId;
                toId -= 2;
            }
            buffer.SetGlobalTexture(fxSource2Id, sourceId);
            Draw(formId, BuiltinRenderTextureType.CameraTarget, Pass.BloomCombine);
        }
        else
        {
            buffer.ReleaseTemporaryRT(bloomPyrmidId);
        }
      
        buffer.EndSample("Bloom");
    }
}
