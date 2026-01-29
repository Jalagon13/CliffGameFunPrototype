using UnityEngine;
using UnityEngine.UI;

namespace CliffGame
{
    public enum BuildOption
    {
        Fence,
        Platform,
        Stairs,
        Destroy,
        Repair
    }

    public class OptionUI : MonoBehaviour
    {
        [SerializeField] private BuildOption _buildOption;
        public BuildOption BuildOption => _buildOption;
        
        public void OnSelected()
        {
            Debug.Log($"Selecting Option {_buildOption}");
            switch (_buildOption)
            {
                case BuildOption.Fence:
                    BuildingManager.Instance.SetBuildType(SelectedBuildType.Fence);
                    break;
                case BuildOption.Platform:
                    BuildingManager.Instance.SetBuildType(SelectedBuildType.Platform);
                    break;
                case BuildOption.Stairs:
                    BuildingManager.Instance.SetBuildType(SelectedBuildType.Stairs);
                    break;
                case BuildOption.Destroy:
                    BuildingManager.Instance.SetBuildType(SelectedBuildType.DestroyMode);
                    break;
                case BuildOption.Repair:
                    BuildingManager.Instance.SetBuildType(SelectedBuildType.RepairMode);
                    break;
            }
            
            Image selectedImage = transform.GetChild(0).GetComponent<Image>();
            selectedImage.enabled = true;
        }
        
        public void OnDeselected()
        {
            Image selectedImage = transform.GetChild(0).GetComponent<Image>();
            selectedImage.enabled = false;
        }
    }
}
