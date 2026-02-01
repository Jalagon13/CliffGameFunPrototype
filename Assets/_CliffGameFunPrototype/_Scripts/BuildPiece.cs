using UnityEngine;

namespace CliffGame
{
    public class BuildPiece : MonoBehaviour
    {
        [field: SerializeField]
        public BuildOption BuildType { get; private set; }
    
        public bool IsAnchored { get; private set; }
        
        public void InitializeAnchoredStatus()
        {
            IsAnchored = false;

            Platform platform = GetComponent<Platform>();
            if (platform == null)
            {
                // Debug.Log("BuildPiece is NOT anchored.");
                return;
            }

            BoxCollider boxCollider = platform.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                Debug.LogWarning($"Platform is missing BoxCollider component.");
                return;
            }

            Vector3 worldCenter = boxCollider.transform.TransformPoint(boxCollider.center);
            Vector3 worldHalfExtents = Vector3.Scale(boxCollider.size * 0.5f, boxCollider.transform.lossyScale);
            Quaternion worldRotation = boxCollider.transform.rotation;

            Collider[] overlaps = Physics.OverlapBox(worldCenter, worldHalfExtents, worldRotation);

            foreach (Collider col in overlaps)
            {
                if (col == boxCollider)
                    continue;

                if (col is TerrainCollider)
                {
                    IsAnchored = true;
                    // Debug.Log("BuildPiece anchored to terrain.");
                    return;
                }
            }
            
            // Debug.Log("BuildPiece is NOT anchored.");
        }
    }
}
