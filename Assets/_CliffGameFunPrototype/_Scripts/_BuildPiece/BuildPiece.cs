using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CliffGame
{
    public class BuildPiece : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _destructionParticles;

        [field: SerializeField]
        public BuildOption BuildType { get; private set; }
    
        public bool IsAnchored { get; private set; }
        
        public IReadOnlyList<Connector> Connectors { get; private set; }
        public int DistanceFromAnchor { get; private set; }

        private void Awake()
        {
            Connectors = GetComponentsInChildren<Connector>();
        }
        
        public void PlayDestructionGameFeel()
        {
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.WoodDestroyedSFX, transform.position);
            Instantiate(_destructionParticles.gameObject, transform.position + Vector3.up * 0.25f, Quaternion.identity);
        }

        public IEnumerable<BuildPiece> GetConnectedBuildPieces()
        {
            foreach (Connector connector in Connectors)
            {
                foreach (BuildPiece connectedBuildPiece in connector.ConnectedBuildPieces)
                {
                    if(connectedBuildPiece != this)
                        yield return connectedBuildPiece;
                }
            }
        }

        public void SetDistanceFromAnchor(int distance)
        {
            DistanceFromAnchor = distance;
        }

        public void InitializeAnchoredStatus()
        {
            IsAnchored = false;

            BuildPieceDurability platform = GetComponent<BuildPieceDurability>();
            if (platform == null)
            {
                // Debug.Log("BuildPiece is NOT anchored.");
                return;
            }
            
            if(BuildType != BuildOption.Platform)
            {
                // Only platforms can be anchored
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
                    DistanceFromAnchor = 0;
                    return;
                }
            }
            
        }

        public void CleanupConnectors()
        {
            if(this == null) return;
        
            foreach (var connector in GetComponentsInChildren<Connector>())
            {
                connector.CleanupConnections();
                connector.gameObject.SetActive(false);
            }
        }

        public void HandleDestroy()
        {
            CleanupConnectors();
            PlayDestructionGameFeel();
            Destroy(gameObject);
        }
    }
}
