namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// The TwoBoneIK constraint data.
    /// </summary>
    [System.Serializable]
    public struct BasisTwoBoneIKConstraintData : IAnimationJobData, BasisITwoBoneIKConstraintData
    {
        [SerializeField] Transform m_Root;
        [SerializeField] Transform m_Mid;
        [SerializeField] Transform m_Tip;
        [SyncSceneToStream, SerializeField]
        public Vector3 TargetPosition;
        [SyncSceneToStream, SerializeField]
        public Vector3 TargetRotation;
        [SyncSceneToStream, SerializeField]
        public Vector3 HintPosition;
        [SyncSceneToStream, SerializeField]
        public Vector3 HintRotation;

        Vector3 BasisITwoBoneIKConstraintData.targetPosition { get => TargetPosition; }

         Vector3 BasisITwoBoneIKConstraintData.targetRotation { get => TargetRotation; }

         Vector3 BasisITwoBoneIKConstraintData.hintPosition { get => HintPosition; }
         Vector3 BasisITwoBoneIKConstraintData.HintRotation { get => HintRotation; }

        [SyncSceneToStream, SerializeField, Range(0f, 1f)] float m_TargetPositionWeight;
        [SyncSceneToStream, SerializeField, Range(0f, 1f)] float m_TargetRotationWeight;
        [SyncSceneToStream, SerializeField, Range(0f, 1f)] float m_HintWeight;

        [NotKeyable, SerializeField] bool m_MaintainTargetPositionOffset;
        [NotKeyable, SerializeField] bool m_MaintainTargetRotationOffset;

        /// <inheritdoc />
        public Transform root { get => m_Root; set => m_Root = value; }
        /// <inheritdoc />
        public Transform mid { get => m_Mid; set => m_Mid = value; }
        /// <inheritdoc />
        public Transform tip { get => m_Tip; set => m_Tip = value; }
        /// <inheritdoc />

        /// <summary>The weight for which target position has an effect on IK calculations. This is a value in between 0 and 1.</summary>
        public float targetPositionWeight { get => m_TargetPositionWeight; set => m_TargetPositionWeight = Mathf.Clamp01(value); }
        /// <summary>The weight for which target rotation has an effect on IK calculations. This is a value in between 0 and 1.</summary>
        public float targetRotationWeight { get => m_TargetRotationWeight; set => m_TargetRotationWeight = Mathf.Clamp01(value); }
        /// <summary>The weight for which hint transform has an effect on IK calculations. This is a value in between 0 and 1.</summary>
        public float hintWeight { get => m_HintWeight; set => m_HintWeight = Mathf.Clamp01(value); }

        /// <inheritdoc />
        public bool maintainTargetPositionOffset { get => m_MaintainTargetPositionOffset; set => m_MaintainTargetPositionOffset = value; }
        /// <inheritdoc />
        public bool maintainTargetRotationOffset { get => m_MaintainTargetRotationOffset; set => m_MaintainTargetRotationOffset = value; }

        /// <inheritdoc />
        string BasisITwoBoneIKConstraintData.targetPositionWeightFloatProperty => ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(m_TargetPositionWeight));
        /// <inheritdoc />
        string BasisITwoBoneIKConstraintData.targetRotationWeightFloatProperty => ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(m_TargetRotationWeight));
        /// <inheritdoc />
        string BasisITwoBoneIKConstraintData.hintWeightFloatProperty => ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(m_HintWeight));

        string BasisITwoBoneIKConstraintData.TargetpositionVector3Property => ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(TargetPosition));

        string BasisITwoBoneIKConstraintData.TargetrotationVector3Property => ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(TargetRotation));

        string BasisITwoBoneIKConstraintData.HintpositionVector3Property => ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(HintPosition));

        string BasisITwoBoneIKConstraintData.HintrotationVector3Property => ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(HintRotation));

        /// <inheritdoc />
        bool IAnimationJobData.IsValid() => (m_Tip != null && m_Mid != null && m_Root != null && m_Tip.IsChildOf(m_Mid) && m_Mid.IsChildOf(m_Root));

        /// <inheritdoc />
        void IAnimationJobData.SetDefaultValues()
        {
            m_Root = null;
            m_Mid = null;
            m_Tip = null;
            m_TargetPositionWeight = 1f;
            m_TargetRotationWeight = 1f;
            m_HintWeight = 1f;
            m_MaintainTargetPositionOffset = false;
            m_MaintainTargetRotationOffset = false;
        }
    }

    /// <summary>
    /// TwoBoneIK constraint
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("Animation Rigging/Two Bone IK Constraint")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.3/manual/constraints/TwoBoneIKConstraint.html")]
    public class BasisTwoBoneIKConstraint : RigConstraint<BasisTwoBoneIKConstraintJob,BasisTwoBoneIKConstraintData,BasisTwoBoneIKConstraintJobBinder<BasisTwoBoneIKConstraintData>>
    {
        /// <inheritdoc />
        protected override void OnValidate()
        {
            base.OnValidate();
            m_Data.hintWeight = Mathf.Clamp01(m_Data.hintWeight);
            m_Data.targetPositionWeight = Mathf.Clamp01(m_Data.targetPositionWeight);
            m_Data.targetRotationWeight = Mathf.Clamp01(m_Data.targetRotationWeight);
        }
    }
    /// <summary>
    /// The TwoBoneIK constraint job.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct BasisTwoBoneIKConstraintJob : IWeightedAnimationJob
    {
        /// <summary>The transform handle for the root transform.</summary>
        public ReadWriteTransformHandle root;
        /// <summary>The transform handle for the mid transform.</summary>
        public ReadWriteTransformHandle mid;
        /// <summary>The transform handle for the tip transform.</summary>
        public ReadWriteTransformHandle tip;

        /// <summary>The transform handle for the hint transform.</summary>
        public Vector3Property hintPosition;
        /// <summary>The transform handle for the target transform.</summary>
        public Vector3Property targetPosition;

        /// <summary>The transform handle for the hint transform.</summary>
        public Vector3Property hintRotation;
        /// <summary>The transform handle for the target transform.</summary>
        public Vector3Property targetRotation;

        /// <summary>The offset applied to the target transform if maintainTargetPositionOffset or maintainTargetRotationOffset is enabled.</summary>
        public AffineTransform targetOffset;

        /// <summary>The weight for which target position has an effect on IK calculations. This is a value in between 0 and 1.</summary>
        public FloatProperty targetPositionWeight;
        /// <summary>The weight for which target rotation has an effect on IK calculations. This is a value in between 0 and 1.</summary>
        public FloatProperty targetRotationWeight;
        /// <summary>The weight for which hint transform has an effect on IK calculations. This is a value in between 0 and 1.</summary>
        public FloatProperty hintWeight;

        /// <summary>The main weight given to the constraint. This is a value in between 0 and 1.</summary>
        public FloatProperty jobWeight { get; set; }

        /// <summary>
        /// Defines what to do when processing the root motion.
        /// </summary>
        /// <param name="stream">The animation stream to work on.</param>
        public void ProcessRootMotion(AnimationStream stream) { }

        /// <summary>
        /// Defines what to do when processing the animation.
        /// </summary>
        /// <param name="stream">The animation stream to work on.</param>
        public void ProcessAnimation(AnimationStream stream)
        {
            float w = jobWeight.Get(stream);
            if (w > 0f)
            {
              // BasisDebug.Log("Value is " + targetPosition);
                AffineTransform target = new AffineTransform(targetPosition.Get(stream), Quaternion.Euler(targetRotation.Get(stream)));
                AffineTransform hint = new AffineTransform(hintPosition.Get(stream), Quaternion.Euler(hintRotation.Get(stream)));
                BasisAnimationRuntimeUtils.SolveTwoBoneIK(stream, root, mid, tip, target, hint,targetPositionWeight.Get(stream) * w,targetRotationWeight.Get(stream) * w,hintWeight.Get(stream) * w, targetOffset);
            }
            else
            {
                BasisAnimationRuntimeUtils.PassThrough(stream, root);
                BasisAnimationRuntimeUtils.PassThrough(stream, mid);
                BasisAnimationRuntimeUtils.PassThrough(stream, tip);
            }
        }
    }

    /// <summary>
    /// This interface defines the data mapping for the TwoBoneIK constraint.
    /// </summary>
    public interface BasisITwoBoneIKConstraintData
    {
        /// <summary>The root transform of the two bones hierarchy.</summary>
        Transform root { get; }
        /// <summary>The mid transform of the two bones hierarchy.</summary>
        Transform mid { get; }
        /// <summary>The tip transform of the two bones hierarchy.</summary>
        Transform tip { get; }
        public Vector3 targetPosition { get; }
        public Vector3 targetRotation { get; }
        public Vector3 hintPosition { get; }
        public Vector3 HintRotation { get; }
        /// <summary>This is used to maintain the offset of the tip position to the target position.</summary>
        bool maintainTargetPositionOffset { get; }
        /// <summary>This is used to maintain the offset of the tip rotation to the target rotation.</summary>
        bool maintainTargetRotationOffset { get; }

        /// <summary>The path to the target position weight property in the constraint component.</summary>
        string targetPositionWeightFloatProperty { get; }
        /// <summary>The path to the target rotation weight property in the constraint component.</summary>
        string targetRotationWeightFloatProperty { get; }
        /// <summary>The path to the hint weight property in the constraint component.</summary>
        string hintWeightFloatProperty { get; }

        /// <summary>The path to the override position property in the constraint component.</summary>
        string TargetpositionVector3Property { get; }
        /// <summary>The path to the override rotation property in the constraint component.</summary>
        string TargetrotationVector3Property { get; }

        /// <summary>The path to the override position property in the constraint component.</summary>
        string HintpositionVector3Property { get; }
        /// <summary>The path to the override rotation property in the constraint component.</summary>
        string HintrotationVector3Property { get; }
    }

    /// <summary>
    /// The TwoBoneIK constraint job binder.
    /// </summary>
    /// <typeparam name="T">The constraint data type</typeparam>
    public class BasisTwoBoneIKConstraintJobBinder<T> : AnimationJobBinder<BasisTwoBoneIKConstraintJob, T>where T : struct, IAnimationJobData, BasisITwoBoneIKConstraintData
    {
        /// <summary>
        /// Creates the animation job.
        /// </summary>
        /// <param name="animator">The animated hierarchy Animator component.</param>
        /// <param name="data">The constraint data.</param>
        /// <param name="component">The constraint component.</param>
        /// <returns>Returns a new job interface.</returns>
        public override BasisTwoBoneIKConstraintJob Create(Animator animator, ref T data, Component component)
        {
            BasisTwoBoneIKConstraintJob job = new BasisTwoBoneIKConstraintJob
            {
                root = ReadWriteTransformHandle.Bind(animator, data.root),
                mid = ReadWriteTransformHandle.Bind(animator, data.mid),
                tip = ReadWriteTransformHandle.Bind(animator, data.tip),
                targetPosition = Vector3Property.Bind(animator, component, data.TargetpositionVector3Property),
                targetRotation = Vector3Property.Bind(animator, component, data.TargetrotationVector3Property),

                hintPosition = Vector3Property.Bind(animator, component, data.HintpositionVector3Property),
                hintRotation = Vector3Property.Bind(animator, component, data.HintrotationVector3Property),

                targetOffset = AffineTransform.identity,
            };
            if (data.maintainTargetPositionOffset)
            {
                job.targetOffset.translation = data.tip.position - data.targetPosition;
            }
            if (data.maintainTargetRotationOffset)
            {
                job.targetOffset.rotation = Quaternion.Inverse(Quaternion.Euler(data.targetRotation)) * data.tip.rotation;
            }
            job.targetPositionWeight = FloatProperty.Bind(animator, component, data.targetPositionWeightFloatProperty);
            job.targetRotationWeight = FloatProperty.Bind(animator, component, data.targetRotationWeightFloatProperty);
            job.hintWeight = FloatProperty.Bind(animator, component, data.hintWeightFloatProperty);

            return job;
        }

        /// <summary>
        /// Destroys the animation job.
        /// </summary>
        /// <param name="job">The animation job to destroy.</param>
        public override void Destroy(BasisTwoBoneIKConstraintJob job)
        {
        }
    }
}
