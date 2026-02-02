using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CliffGame
{

    [System.Serializable]
    public enum ConnectorPosition
    {
        Left,
        Right,
        Top,
        Bottom
    }

    public class Connector : MonoBehaviour
    {
        public ConnectorPosition ConnectorPosition;
        public BuildOption[] AllowedConnectionTypes;

        private SphereCollider _connectorCollider;
        public SphereCollider ConnectorCollider => _connectorCollider;
        
        private HashSet<Connector> _connectedConnectors = new();

        [SerializeField, Tooltip("DEBUG (Read Only): Connected build types")]
        private List<BuildOption> _debugConnectedBuildTypes = new();

        public IEnumerable<BuildPiece> ConnectedBuildPieces => _connectedConnectors.Select(c => c.BuildPiece).Where(bp => bp != null);
        public bool HasAnyConnection => _connectedConnectors.Count > 0;
        public BuildPiece BuildPiece { get; private set; }
        

        private void Awake()
        {
            BuildPiece = transform.root.GetComponent<BuildPiece>();

            _connectorCollider = GetComponent<SphereCollider>();
        }

        public bool CanConnectTo(BuildOption incomingBuildType)
        {
            if(!AllowedConnectionTypes.Contains(incomingBuildType))
                return false;
                
            if(!PassesOccupancyRules(incomingBuildType))
                return false;

            return true;
        }

        private bool PassesOccupancyRules(BuildOption incomingBuildType)
        {
            if(incomingBuildType == BuildOption.Platform && (IsConnectedTo(BuildOption.Platform) || IsConnectedTo(BuildOption.Stairs)))
                return false;

            if (incomingBuildType == BuildOption.Stairs && (IsConnectedTo(BuildOption.Stairs) || IsConnectedTo(BuildOption.Fence)))
                return false;

            if (incomingBuildType == BuildOption.Fence && (IsConnectedTo(BuildOption.Fence) || IsConnectedTo(BuildOption.Stairs)))
                return false;

            return true;
        }

        public bool IsConnectedTo(BuildOption type)
        {
            return _connectedConnectors.Any(c => c.BuildPiece.BuildType == type);
        }

        private void RefreshDebugConnectedBuildTypes()
        {
            _debugConnectedBuildTypes.Clear();
            foreach (var connector in _connectedConnectors)
            {
                if (connector.BuildPiece != null)
                    _debugConnectedBuildTypes.Add(connector.BuildPiece.BuildType);
            }
        }

        public void EstablishConnection(bool rootCall = false)
        {
            Collider[] collidersTouchingConnectorCollider = Physics.OverlapSphere(transform.position, _connectorCollider.radius);
            
            foreach (Collider collider in collidersTouchingConnectorCollider)
            {
                if (collider.GetInstanceID() == _connectorCollider.GetInstanceID() || !collider.gameObject.activeInHierarchy || collider.gameObject.layer != gameObject.layer) continue;

                Connector foundConnector = collider.GetComponent<Connector>();

                if (foundConnector == null) continue;
                
                // Connection establishing logic
                _connectedConnectors.Add(foundConnector);
                RefreshDebugConnectedBuildTypes();
                // Debug.Log($"Connector on {BuildPiece.BuildType} connected to {foundConnector.BuildPiece.BuildType}");
                
                if(rootCall)
                {
                    foundConnector.EstablishConnection();
                    foundConnector.RefreshDebugConnectedBuildTypes();
                }
            }
        }

        public void CleanupConnections()
        {
            // Tell all connected connectors to forget about me
            foreach (var connected in _connectedConnectors)
            {
                connected.RemoveConnection(this);
            }

            // Clear my own connections
            _connectedConnectors.Clear();

            // Refresh debug view
            RefreshDebugConnectedBuildTypes();
        }

        private void RemoveConnection(Connector connector)
        {
            if (_connectedConnectors.Remove(connector))
            {
                RefreshDebugConnectedBuildTypes();
            }
        }

        private void OnDrawGizmos()
        {
            if (_connectorCollider == null)
                _connectorCollider = GetComponent<SphereCollider>();

            // Color rules (debug):
            // Green  = no connections (fully available)
            // Yellow = partially occupied (has connections but still usable)
            // Red    = fully blocked (cannot connect to anything anymore)

            Gizmos.color = Color.green;

            if (_connectedConnectors != null && _connectedConnectors.Count > 0)
            {
                // Default to partially occupied
                Gizmos.color = Color.yellow;

                // If this connector can no longer accept any allowed build type, mark as blocked
                bool canAcceptAnything = false;
                foreach (var buildOption in AllowedConnectionTypes)
                {
                    if (CanConnectTo(buildOption))
                    {
                        canAcceptAnything = true;
                        break;
                    }
                }

                if (!canAcceptAnything)
                {
                    Gizmos.color = Color.red;
                }
            }

            Gizmos.DrawWireSphere(transform.position, _connectorCollider.radius);
        }
    }
}
