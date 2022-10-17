using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="Render/Custom Post FX Setting")]
public class PostFXSetting : ScriptableObject
{

    [SerializeField]
    Shader shader = default;
    [System.NonSerialized]
    Material material;
   
    public bool bicubicUpsampling;

    public Material Material
    {
        get
        {
            if (material == null)
            {
                material = new Material(shader);
                material.hideFlags = HideFlags.HideAndDontSave;
            }
             return material;
        }

    }
    [System.Serializable]
    public struct BloomSettings
    {
        [Range(0f, 16f)]
        public int maxIterations;

        [Min(1f)]
        public int downscaleLimit;

        [Min(0)]
        public float threshold;

        [Range(0f, 1f)]
        public float thresholdKnee;

        [Min(0f)]
        public float intensity;

    }
    [SerializeField]
    BloomSettings bloom = default;

    public BloomSettings Bloom => bloom;

}
