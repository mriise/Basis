using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


namespace Basis.Scripts.Networking.Transmitters
{
    [BurstCompile]
    public struct BasisDistanceJobs : IJobParallelFor
    {
        public float VoiceDistance;
        public float HearingDistance;
        public float AvatarDistance;
        [ReadOnly]
        public float3 referencePosition;
        [ReadOnly]
        public NativeArray<float3> targetPositions;

        [WriteOnly]
        public NativeArray<float> distances;
        [WriteOnly]
        public NativeArray<bool> DistanceResults;
        [WriteOnly]
        public NativeArray<bool> HearingResults;
        [WriteOnly]
        public NativeArray<bool> AvatarResults;

        // Shared result for the smallest distance
        [NativeDisableParallelForRestriction]
        public NativeArray<float> smallestDistance;

        public void Execute(int index)
        {
            // Calculate distance
            Vector3 diff = targetPositions[index] - referencePosition;
            float sqrDistance = diff.sqrMagnitude;
            distances[index] = sqrDistance;

            // Determine boolean results
            DistanceResults[index] = sqrDistance < VoiceDistance;
            HearingResults[index] = sqrDistance < HearingDistance;
            AvatarResults[index] = sqrDistance < AvatarDistance;

            // Update the smallest distance (atomic operation to avoid race conditions)
            float currentSmallest = smallestDistance[0];
            if (sqrDistance < currentSmallest)
            {
                smallestDistance[0] = sqrDistance;
            }
        }
    }
}
