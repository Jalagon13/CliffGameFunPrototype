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

        private void Awake()
        {
            _connectorCollider = GetComponent<SphereCollider>();
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
        }
    }
}
