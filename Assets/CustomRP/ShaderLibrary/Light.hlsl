//灯光数据相关库
#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_OTHER_LIGHT_COUNT 64
CBUFFER_START(_CustomLight)
    int _DirectionalLightCount;
    //定向光源颜色、方向、阴影等数据
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    //阴影数据
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
	float4 _OtherLightShadowData[MAX_OTHER_LIGHT_COUNT];
    //其他光源颜色，坐标,方向数据
    int _OtherLightCount;
    float4 _OtherLightColors[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightPositions[MAX_OTHER_LIGHT_COUNT];
	float4 _OtherLightDirections[MAX_OTHER_LIGHT_COUNT];
	float4 _OtherLightSpotAngles[MAX_OTHER_LIGHT_COUNT];
CBUFFER_END

//灯光的属性
struct Light {
    //颜色
	float3 color;
	//方向
	float3 direction;
	//衰减
	float attenuation;
};
//获得非定向光光源的数量
int GetOtherLightCount()
{
    return _OtherLightCount;
}
//获取方向光源的数量
int GetDirectionalLightCount() {
	return _DirectionalLightCount;
}
//获取非定向光的阴影数据
OtherShadowData GetOtherShadowData(int lightIndex)
{
   OtherShadowData data;
    data.lightPositionWS = 0.0;
    data.spotDirectionWS = 0.0;
   data.strngth=_OtherLightShadowData[lightIndex].x;
   data.tileIndex = _OtherLightShadowData[lightIndex].y;
   data.shadowMaskChannel=_OtherLightShadowData[lightIndex].w;
   data.isPoint = _OtherLightShadowData[lightIndex].z==1.0;
    data.lightDirectionWS = 0.0;
   return data;
}

//获取方向光的阴影数据
DirectionalShadowData GetDirectionalShadowData(int lightIndex, ShadowData shadowData) {
	DirectionalShadowData data;
	data.strength = _DirectionalLightShadowData[lightIndex].x;
	data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
	//法线偏差
	data.normalBias = _DirectionalLightShadowData[lightIndex].z;
	data.shadowMaskChannel = _DirectionalLightShadowData[lightIndex].w;
	return data;
}
//获取目标索引非定向光的属性
Light GetOtherLight(int index, Surface surfaceWS, ShadowData shadowData)
{
   Light light;
 
    OtherShadowData otherShadowData = GetOtherShadowData(index);
    light.color = _OtherLightColors[index].rgb;
    float3 position = _OtherLightPositions[index].xyz;
    float3 spotDirection = _OtherLightDirections[index].xyz;
    float3 ray = _OtherLightPositions[index].xyz - surfaceWS.position;
    light.direction = normalize(ray);
    float distanceSqr=  max(dot(ray, ray), 0.00001);
    float rangeAttenuation = Square(saturate(1.0 - Square(distanceSqr * _OtherLightPositions[index].w)));
	//得到聚光灯的衰减
	float spotAttenuation=Square(saturate(dot(_OtherLightDirections[index].xyz,light.direction)*_OtherLightSpotAngles[index].x+_OtherLightSpotAngles[index].y));

  //得到阴影数据
  //  DirectionalShadowData dirShadowData = GetDirectionalShadowData(index, shadowData);
	//得到阴影衰减
    light.attenuation =GetOtherShadowAttenuation(otherShadowData,shadowData,surfaceWS)* spotAttenuation*rangeAttenuation / distanceSqr;
  //计算聚光灯衰减值

    otherShadowData.lightPositionWS = position;
    otherShadowData.spotDirectionWS = spotDirection;
    otherShadowData.lightDirectionWS = light.direction;
    return light;

}

//获取目标索引定向光的属性
Light GetDirectionalLight (int index,Surface surfaceWS, ShadowData shadowData) {
	Light light;
	light.color = _DirectionalLightColors[index].rgb;
	light.direction = _DirectionalLightDirections[index].xyz;
	//得到阴影数据
	DirectionalShadowData dirShadowData = GetDirectionalShadowData(index,shadowData);
	//得到阴影衰减
	light.attenuation = GetDirectionalShadowAttenuation(dirShadowData,shadowData, surfaceWS);

	return light;
}


#endif