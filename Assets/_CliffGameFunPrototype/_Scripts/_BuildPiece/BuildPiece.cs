using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CliffGame
{
    public class BuildPiece : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _destructionParticles;
        
        [SerializeField] private BoxCollider _pieceCollider;

        [field: SerializeField]
        public BuildOption BuildType { get; private set; }
    
        public bool IsAnchored { get; private set; }
        
        public IReadOnlyList<Connector> Connectors { get; private set; }
        public int DistanceFromAnchor { get; private set; }

        public bool IsRefundableOnDestroy { get; private set; }

        public void MarkRefundable()
        {
            IsRefundableOnDestroy = true;
        }

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

            if(BuildType != BuildOption.Platform && BuildType != BuildOption.Stairs)
            {
                // Only platforms and stairs can be anchored
                return;
            }
            
            Vector3 worldCenter = _pieceCollider.transform.TransformPoint(_pieceCollider.center);
            Vector3 worldHalfExtents = Vector3.Scale(_pieceCollider.size * 0.5f, _pieceCollider.transform.lossyScale);
            Quaternion worldRotation = _pieceCollider.transform.rotation;

            Collider[] overlaps = Physics.OverlapBox(worldCenter, worldHalfExtents, worldRotation);

            foreach (Collider col in overlaps)
            {
                if (col == _pieceCollider)
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
            if (IsRefundableOnDestroy)
            {
                RefundCostToPlayer();
                IsRefundableOnDestroy = false; // safety
            }

            CleanupConnectors();
            PlayDestructionGameFeel();
            Destroy(gameObject);
        }

        private void RefundCostToPlayer()
        {
            Debug.Log($"Refunding");
            InventoryManager.Instance.AddItems(BuildingManager.Instance.ItemsNeededForBuilding);
        }
    }
}
