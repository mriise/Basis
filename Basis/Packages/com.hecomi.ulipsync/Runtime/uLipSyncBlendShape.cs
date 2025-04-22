using UnityEngine;
using System.Collections.Generic;

namespace uLipSync
{
    [ExecuteAlways]
    public class uLipSyncBlendShape : MonoBehaviour
    {
        public SkinnedMeshRenderer skinnedMeshRenderer;
        public List<BlendShapeInfo> CachedblendShapes = new List<BlendShapeInfo>();
        public BlendShapeInfo[] BlendShapeInfos;

        [Range(0f, 0.3f)]
        public float smoothness = 0.05f;
        public float maxBlendShapeValue = 100f;
        public float minVolume = -2.5f;
        public float maxVolume = -1.5f;
        public bool usePhonemeBlend = false;
        private float _volume = 0f;
        private float _openCloseVelocity = 0f;

        public void OnLipSyncUpdate( string phoneme,float volume, float rawVolume,Dictionary<string, float> phonemeRatios)
        {
            if (skinnedMeshRenderer == null || BlendShapeInfos == null || BlendShapeInfos.Length == 0)
                return;

            float normVol = 0f;
            if (rawVolume > 0f)
            {
                normVol = Mathf.Log10(rawVolume);
                normVol = Mathf.Clamp01((normVol - minVolume) / Mathf.Max(maxVolume - minVolume, 1e-4f));
            }

            _volume = SmoothDamp(_volume, normVol, ref _openCloseVelocity);
            float globalMultiplier = _volume * maxBlendShapeValue;

            float totalWeight = 0f;
            int count = BlendShapeInfos.Length;

            // First pass: compute weights and total sum
            for (int Index = 0; Index < count; Index++)
            {
                var bs = BlendShapeInfos[Index];
                float targetWeight = 0f;

                if (usePhonemeBlend && phonemeRatios != null && !string.IsNullOrEmpty(bs.phoneme))
                {
                    phonemeRatios.TryGetValue(bs.phoneme, out targetWeight);
                }
                else if (bs.phoneme == phoneme)
                {
                    targetWeight = 1f;
                }

                float weightVelocity = bs.weightVelocity;
                bs.weight = SmoothDamp(bs.weight, targetWeight, ref weightVelocity);
                bs.weightVelocity = weightVelocity;
                totalWeight += bs.weight;
            }

            float invTotal = (totalWeight > 0f) ? (1f / totalWeight) : 0f;

            // Second pass: normalize + apply
            for (int i = 0; i < count; i++)
            {
                var bs = BlendShapeInfos[i];

                if (bs.index < 0) continue;

                float weight = bs.weight * invTotal;
                float finalWeight = weight * bs.maxWeight * globalMultiplier;

                // Skip setting if final weight is very close to the last value
                if (Mathf.Abs(bs.LastValue - finalWeight) < 0.01f)
                    continue;

                skinnedMeshRenderer.SetBlendShapeWeight(bs.index, finalWeight);
                bs.LastValue = finalWeight;
            }
        }

        float SmoothDamp(float value, float target, ref float velocity)
        {
            return Mathf.SmoothDamp(value, target, ref velocity, smoothness);
        }

        public BlendShapeInfo GetBlendShapeInfo(string phoneme)
        {
            return CachedblendShapes.Find(info => info.phoneme == phoneme);
        }

        public void AddBlendShape(string phoneme, int blendShape)
        {
            var bs = GetBlendShapeInfo(phoneme);
            if (bs == null)
            {
                bs = new BlendShapeInfo { phoneme = phoneme };
                CachedblendShapes.Add(bs);
            }

            if (skinnedMeshRenderer != null)
            {
                bs.index = blendShape;
            }
        }
    }
}
