using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
/// <summary>
/// 灯光管理类
/// </summary>
public class Lighting
{

	const string bufferName = "Lighting";

	CommandBuffer buffer = new CommandBuffer
	{
		name = bufferName
	};
    //设置最大可见定向光数量
    const int maxDirLightCount = 4;
    const int maxOtherLightCount = 64;


    static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
    static int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");
    static int otherLightShadowDataId = Shader.PropertyToID("_OtherLightShadowData");
    static int OtherLightCountId = Shader.PropertyToID("_OtherLightCount");
    static int OtherLightColorId = Shader.PropertyToID("_OtherLightColors");
    static int OtherLighPositionId = Shader.PropertyToID("_OtherLightPositions");
    static int OtherLightDirectionsId = Shader.PropertyToID("_OtherLightDirections");
    static int OtherLightSpotAnglesId = Shader.PropertyToID("_OtherLightSpotAngles");
    //存储定向光的颜色和方向
    static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
    static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];
    static Vector4[] otherLightColors = new Vector4[maxOtherLightCount];
    static Vector4[] otherLightPosition = new Vector4[maxOtherLightCount];
    static Vector4[] otherLightDirections=new Vector4[maxOtherLightCount];
    //存储定向光的阴影数据
    static Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];
    //存储非定向光的阴影数据
    static Vector4[] otherLightShadowData = new Vector4[maxDirLightCount];
    static Vector4[] otherLightSpotAngles = new Vector4[maxDirLightCount];

    static string lightPerObjectKeyword = "_LIGHT_PER_OBJECT";
    //存储相机剔除后的结果
    CullingResults cullingResults;

    Shadows shadows = new Shadows();
    //初始化设置
    public void Setup(ScriptableRenderContext context, CullingResults cullingResults,ShadowSettings shadowSettings, bool useLightsPerObject)
	{
        this.cullingResults = cullingResults;
        buffer.BeginSample(bufferName);
        //阴影的初始化设置
        shadows.Setup(context, cullingResults, shadowSettings);
        //存储并发送所有光源数据
        SetupLights(useLightsPerObject);
        //渲染阴影
        shadows.Render();
        buffer.EndSample(bufferName);
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}
  
    //将点光源的颜色和位置信息存储到数组
    void SetupPointLight(int index, int visibleIndex, ref VisibleLight visibleLight)
    {
        otherLightColors[index] = visibleLight.finalColor;
        Vector4 position= visibleLight.localToWorldMatrix.GetColumn(3);
        position.w = visibleLight.range * visibleLight.range;
        position.w = 1f/Mathf.Max(position.w, 0.000001f);
        otherLightPosition[index] = position;
        otherLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        otherLightSpotAngles[index] = new Vector4(0, 1f);
        otherLightShadowData[index] = shadows.ReserveOtherShadows(visibleLight.light, visibleIndex);
       
    }
    
    void SetupSpotight(int index, int visibleIndex, ref VisibleLight visibleLight)
    {
        otherLightColors[index] = visibleLight.finalColor;
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        position.w = visibleLight.range * visibleLight.range;
        position.w = 1 / Mathf.Max(position.w, 0.000001f);
        otherLightPosition[index] = position;
        otherLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        Light light = visibleLight.light;
        float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
        float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
        float angleRangeInv=1f/Mathf.Max((innerCos - outerCos), 0.000001f);
        otherLightSpotAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
        otherLightShadowData[index] = shadows.ReserveOtherShadows(visibleLight.light, visibleIndex);
    }
	/// <summary>
    /// 存储定向光的数据
    /// </summary>
    /// <param name="index"></param>
    /// <param name="visibleIndex"></param>
    /// <param name="visibleLight"></param>
    /// <param name="light"></param>
	void SetupDirectionalLight(int index,int visibleIndex, ref VisibleLight visibleLight) {
        dirLightColors[index] = visibleLight.finalColor;
        //通过VisibleLight.localToWorldMatrix属性找到前向矢量,它在矩阵第三列，还要进行取反
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        //存储阴影数据
        dirLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light, visibleIndex);
    }
    /// <summary>
    /// 存储并发送所有光源数据
    /// </summary>
    /// <param name="useLightsPerObject"></param>
    /// <param name="renderingLayerMask"></param>
    void SetupLights(bool useLightsPerObject) {
        //得到所有影响相机渲染物体的可见光数据
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        NativeArray<int> indexMap = useLightsPerObject ? cullingResults.GetLightIndexMap(Allocator.Temp) : default;
        int dirLightCount = 0;
        int otherLightCount = 0;
        int i = 0;
        for ( i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];

            int newIndex = -1;
            switch (visibleLight.lightType)
            {
                case LightType.Directional:
                    //VisibleLight结构很大,我们改为传递引用不是传递值，这样不会生成副本
               
                    if (dirLightCount< maxDirLightCount)
                    {
                        SetupDirectionalLight(dirLightCount++, i,ref visibleLight);
                    }
                    break;
                case LightType.Point:
                    if (otherLightCount < maxOtherLightCount)
                    {
                        newIndex = otherLightCount;
                        SetupPointLight(otherLightCount++,i, ref visibleLight);
                    }
                    break;
                case LightType.Spot:
                    if (otherLightCount < maxOtherLightCount)
                    {
                        newIndex = otherLightCount;
                        SetupSpotight(otherLightCount++, i,ref visibleLight);
                    }
                    break;
            }
            if(useLightsPerObject)
            {
                indexMap[i] = newIndex;
            }

        }
        if (useLightsPerObject)
        {
            for (; i < indexMap.Length; i++)
            {
                indexMap[i] = -1;
            }
            cullingResults.SetLightIndexMap(indexMap);
            indexMap.Dispose();
            Shader.EnableKeyword(lightPerObjectKeyword);
        }
        else
        {
            Shader.DisableKeyword(lightPerObjectKeyword);
        }
        buffer.SetGlobalInt(dirLightCountId, dirLightCount);
        if (dirLightCount > 0)
        {
           
            buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
            buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
            buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
        }
        buffer.SetGlobalInt(OtherLightCountId, otherLightCount);
        if (otherLightCount>0)
        {
            buffer.SetGlobalVectorArray(OtherLightSpotAnglesId, otherLightSpotAngles);
            buffer.SetGlobalVectorArray(OtherLightColorId, otherLightColors);
            buffer.SetGlobalVectorArray(OtherLighPositionId, otherLightPosition);
            buffer.SetGlobalVectorArray(OtherLightDirectionsId, otherLightDirections);
            buffer.SetGlobalVectorArray(otherLightShadowDataId, otherLightShadowData);
        }    
    }
    //释放申请的RT内存
    public void Cleanup()
    {
        shadows.Cleanup();
    }
}
