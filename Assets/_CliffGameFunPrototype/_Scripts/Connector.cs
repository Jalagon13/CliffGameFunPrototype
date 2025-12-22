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
        public SelectedBuildType ConnectorParentType;

        [HideInInspector]
        public bool IsConnectedToFloor = false;

        [HideInInspector]
        public bool IsConnectedToWall = false;

        [HideInInspector]
        public bool CanConnectTo = true;

        [SerializeField]
        private bool _canConnectToFloor = true;

        [SerializeField]
        private bool _canConnectToWall = true;
        
        private SphereCollider _connectorCollider;
        private Floor _floor;
        
        public bool IsNotConnectedToAnything { get; private set; }
        

        private void Awake()
        {
            _connectorCollider = GetComponent<SphereCollider>();
            _floor = transform.root.GetComponent<Floor>();
        }

        private void OnDrawGizmos()
        {
            _connectorCollider = GetComponent<SphereCollider>();

            // Red: Can no longer be connected at all, Green: Can connect to both wall and floor, Yellow: Can only connect to floor, Blue: Can only connect to wall
            Gizmos.color = IsConnectedToFloor ? (IsConnectedToWall ? Color.red : Color.blue) : (!IsConnectedToWall ? Color.green : Color.yellow);
            Gizmos.DrawWireSphere(transform.position, _connectorCollider.radius);
        }

        public void UpdateConnectors(bool rootCall = false)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, _connectorCollider.radius);

            IsConnectedToFloor = !_canConnectToFloor;
            IsConnectedToWall = !_canConnectToWall;

            foreach (Collider collider in colliders)
            {
                if (collider.GetInstanceID() == GetComponent<Collider>().GetInstanceID()) continue;

                if (!collider.gameObject.activeInHierarchy) continue;

                if (collider.gameObject.layer == gameObject.layer)
                {
                    Connector foundConnector = collider.GetComponent<Connector>();

                    if (foundConnector.ConnectorParentType == SelectedBuildType.Floor)
                    {
                        IsConnectedToFloor = true;
                    }

                    if (foundConnector.ConnectorParentType == SelectedBuildType.Wall)
                    {
                        IsConnectedToWall = true;
                    }

                    if (rootCall)
                    {
                        foundConnector.UpdateConnectors();
                    }
                }
            }

            CanConnectTo = true;

            if (IsConnectedToFloor && IsConnectedToWall)
            {
                CanConnectTo = false;
            }

            IsNotConnectedToAnything = !IsConnectedToFloor && !IsConnectedToWall;
            
            if(CheckIfBuildingIsNotConntectedToAnything())
            {
                if(_floor.DestroyIfNotConntectedToAnything)
                {
                    Destroy(_floor.gameObject);
                }
            }
        }

        // Still WIP. Only works if the platform is not connected to anything but like bunches of platforms not connected to the cliff still stand
        private bool CheckIfBuildingIsNotConntectedToAnything()
        {
            if (transform.parent == null)
                return true;

            foreach (Transform item in transform.parent)
            {
                Connector connector = item.GetComponent<Connector>();
                if (connector == null) continue;

                if (!connector.IsNotConnectedToAnything)
                    return false; // At least one connector is connected
            }

            return true; // All connectors are unconnected
        }
    }
}
