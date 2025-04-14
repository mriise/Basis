using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile]
public struct FindClosestPointJob : IJobParallelFor
{
    public Vector2 target;
    public NativeArray<Vector2> coordKeys;
    public NativeArray<float> distances;

    public void Execute(int index)
    {
        distances[index] = Vector2.Distance(coordKeys[index], target);
    }
}
