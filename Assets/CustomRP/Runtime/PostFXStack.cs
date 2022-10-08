
using UnityEngine;
using UnityEngine.Rendering;

enum Pass
{
    BloomHorizontal,
    BloomVertical,
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
        RenderTextureFormat format = RenderTextureFormat.Default;
        int formId = sourceId;
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
        Draw(formId, BuiltinRenderTextureType.CameraTarget, Pass.BloomHorizontal);
        for (i -= 1; i >= 0; i--)
        {
            buffer.ReleaseTemporaryRT(formId );
            buffer.ReleaseTemporaryRT(formId -1);
            formId -= 2;
        }
        buffer.EndSample("Bloom");
    }
}
