namespace uLipSync
{
    [System.Serializable]
    public class BlendShapeInfo
    {
        public string phoneme;
        public int index = -1;
        public float maxWeight = 1f;
        public float weight { get; set; } = 0f;
        public float weightVelocity { get; set; } = 0f;
        public float LastValue { get; set; } = -1f; // Use -1 so it always sets the first time
    }
}
