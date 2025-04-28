using System;
using UnityEngine;
using UnityEngine.LowLevelPhysics;

[System.Serializable]
public class HoverSphere
{
    public bool enabled = true;
    public Vector3 WorldPosition;
    [SerializeField]
    public float Radius;

    /// <summary>
    /// Maximum expected number of colliders this is ever expected to see.
    /// Using a max result value less than that will cause undefined behavior. 
    /// </summary>
    [SerializeField]
    public int MaxResults;
    [SerializeField]
    public LayerMask LayerMask;
    public QueryTriggerInteraction QueryTrigger = QueryTriggerInteraction.UseGlobal;

    /// <summary>
    /// Unsorted list of colliders in this sphere
    /// </summary>
    public Collider[] HitResults;
    public int ResultCount = 0;

    /// <summary>
    /// List of hits sorted by distance.
    /// Values of hits are updated per CheckSphere call.
    /// </summary>
    [SerializeField]
    public HoverResult[] Results;

    [System.Serializable]
    public struct HoverResult
    {
        [SerializeField]
        public Collider collider;
        [SerializeField]
        public float distanceToCenter;
        [SerializeField]
        public Vector3 closestPointToCenter;

        public HoverResult(Collider collider, Vector3 worldPos)
        {
            this.collider = collider;
            switch (collider.GeometryHolder.Type)
            {
                case GeometryType.Sphere:
                case GeometryType.Capsule:
                case GeometryType.Box:
                case GeometryType.ConvexMesh:
                    // Physics.ClosestPoint can only be used with a BoxCollider, SphereCollider, CapsuleCollider and a convex MeshCollider
                    closestPointToCenter = collider.ClosestPoint(worldPos);
                    distanceToCenter = Vector3.Distance(closestPointToCenter, worldPos);
                    break;
                case GeometryType.TriangleMesh:
                case GeometryType.Terrain:
                case GeometryType.Invalid:
                default:
                    closestPointToCenter = collider.ClosestPointOnBounds(worldPos);
                    distanceToCenter = Vector3.Distance(closestPointToCenter, worldPos);
                    break;
            }
        }
    }

    public HoverSphere(Vector3 position, float radius, int maxResults, LayerMask layerMask, bool startEnabled)
    {
        WorldPosition = position;
        Radius = radius;
        MaxResults = maxResults;
        HitResults = new Collider[MaxResults];
        Results = new HoverResult[MaxResults];
        LayerMask = layerMask;
        enabled = startEnabled;
    }

    public void PollSystem(Vector3 worldPositionUpdate)
    {
        if (!enabled)
        {
            ResultCount = 0;
            return;
        }
        WorldPosition = worldPositionUpdate;

        if (MaxResults != HitResults.Length)
        {
            HitResults = new Collider[MaxResults];
            Results = new HoverResult[MaxResults];
        }

        ResultCount = Physics.OverlapSphereNonAlloc(WorldPosition, Radius, HitResults, LayerMask, QueryTrigger);
        HitsToSortedResults();
    }

    private void HitsToSortedResults()
    {
        // Calculate results
        for (int Index = 0; Index < ResultCount; Index++)
        {
            var col = HitResults[Index];
            HoverResult result;
            if (col == null)
                result = default;
            else
                result = new HoverResult(col, WorldPosition);
            Results[Index] = result;
        }
        // Sort by object distance to center
        Array.Sort(Results[..ResultCount], (a, b) => a.distanceToCenter.CompareTo(b.distanceToCenter));
    }

    public HoverResult? ClosestResult()
    {
        if (ResultCount == 0 || MaxResults <= 0)
            return null;
        return Results[0];
    }
}