using UnityEngine;

namespace CliffGame
{
    public enum BuildOption
    {
        Wall,
        Floor,
        Destroy
    }

    public class OptionUI : MonoBehaviour
    {
        [SerializeField] private BuildOption _buildOption;
        public BuildOption BuildOption => _buildOption;
        
        public void OnSelected()
        {
            switch (_buildOption)
            {
                case BuildOption.Wall:
                    BuildingManager.Instance.SetIsBuilding(true);
                    break;
                case BuildOption.Floor:
                    BuildingManager.Instance.SetIsBuilding(true);
                    break;
                case BuildOption.Destroy:
                    BuildingManager.Instance.SetIsDestroying(true);
                    break;
            }
        }
        
        public void OnDeselected()
        {
            switch (_buildOption)
            {
                case BuildOption.Wall:
                    BuildingManager.Instance.SetIsBuilding(false);
                    break;
                case BuildOption.Floor:
                    BuildingManager.Instance.SetIsBuilding(false);
                    break;
                case BuildOption.Destroy:
                    BuildingManager.Instance.SetIsDestroying(false);
                    break;
            }
        }
    }
}
