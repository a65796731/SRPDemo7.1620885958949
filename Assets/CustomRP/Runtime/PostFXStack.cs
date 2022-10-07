﻿
using UnityEngine;
using UnityEngine.Rendering;
public class PostFXStack 
{

    public bool IsActive => settings != null;
    const string bufferName = "Post FX";
    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };
    ScriptableRenderContext context;
    Camera camera;
    PostFXSetting settings;
    public void Setup(ScriptableRenderContext context,Camera camera,PostFXSetting settings)
    {
        
        this.context = context;
        this.camera = camera;
        this.settings = settings;
    }
    public void Render(int sourceId)
    {
        buffer.Blit(sourceId, BuiltinRenderTextureType.CameraTarget);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
}