using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompile]
public struct RecordAllFingersJob : IJobParallelForTransform
{
    [ReadOnly]
    public NativeArray<bool> HasProximal;
    [WriteOnly]
    public NativeArray<MuscleLocalPose> FingerPoses;

    public void Execute(int index, TransformAccess transform)
    {
        if (HasProximal[index])
        {
            transform.GetLocalPositionAndRotation(out Vector3 localPosition, out Quaternion rotation);
            FingerPoses[index] = new MuscleLocalPose
            {
                position = localPosition,
                rotation = rotation
            };
        }
    }
}
