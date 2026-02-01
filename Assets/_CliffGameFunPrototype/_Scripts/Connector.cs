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
        private HashSet<Connector> _connectedConnectors = new();
        
        public BuildPiece BuildPiece { get; private set; }
        

        private void Awake()
        {
            BuildPiece = transform.root.GetComponent<BuildPiece>();

            _connectorCollider = GetComponent<SphereCollider>();
        }

        private void OnDrawGizmos()
        {
            // _connectorCollider = GetComponent<SphereCollider>();

            // Red: Can no longer be connected at all, Green: Can connect to both wall and floor, Yellow: Can only connect to floor, Blue: Can only connect to wall
            // Gizmos.color = IsConnectedToFloor ? ((IsConnectedToFence || IsConnectedToStairs) ? Color.red : Color.blue) : ((!IsConnectedToFence || !IsConnectedToStairs) ? Color.green : Color.yellow);
            // Gizmos.DrawWireSphere(transform.position, _connectorCollider.radius);
        }

        public bool CanConnectTo(BuildOption currentSelectedBuildOption)
        {
            bool canConnect = AllowedConnectionTypes.Contains(currentSelectedBuildOption);
        
            return canConnect;
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
                
                if(rootCall)
                {
                    foundConnector.EstablishConnection();
                }
            }
        }

        public void UpdateConnectors(bool rootCall = false)
        {
            // Collider[] colliders = Physics.OverlapSphere(transform.position, _connectorCollider.radius);

            // IsConnectedToFloor = !_canConnectToFloor;
            // IsConnectedToFence = !_canConnectToWall;
            // IsConnectedToStairs = !_canConnectToStairs;

            // foreach (Collider collider in colliders)
            // {
            //     if (collider.GetInstanceID() == GetComponent<Collider>().GetInstanceID()) continue;

            //     if (!collider.gameObject.activeInHierarchy) continue;

            //     if (collider.gameObject.layer == gameObject.layer)
            //     {
            //         Connector foundConnector = collider.GetComponent<Connector>();

            //         if (foundConnector.ConnectorParentType == BuildOption.Platform)
            //         {
            //             IsConnectedToFloor = true;
            //         }

            //         if (foundConnector.ConnectorParentType == BuildOption.Fence)
            //         {
            //             IsConnectedToFence = true;
            //         }

            //         if (foundConnector.ConnectorParentType == BuildOption.Stairs)
            //         {
            //             IsConnectedToStairs = true;
            //         }

            //         if (rootCall)
            //         {
            //             foundConnector.UpdateConnectors();
            //         }
            //     }
            // }

            // CanConnectTo = true;

            // if (IsConnectedToFloor && IsConnectedToFence && IsConnectedToStairs)
            // {
            //     CanConnectTo = false;
            // }
        }
    }
}
