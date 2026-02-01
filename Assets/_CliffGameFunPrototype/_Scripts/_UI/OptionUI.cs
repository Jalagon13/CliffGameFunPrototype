using UnityEngine;
using UnityEngine.UI;

namespace CliffGame
{
    public class OptionUI : MonoBehaviour
    {
        [SerializeField] private BuildOption _buildOption;
        public BuildOption BuildOption => _buildOption;
        
        public void OnSelected()
        {
            // Debug.Log($"Selecting Option {_buildOption}");
            switch (_buildOption)
            {
                case BuildOption.Fence:
                    BuildingManager.Instance.SetBuildType(BuildOption.Fence);
                    break;
                case BuildOption.Platform:
                    BuildingManager.Instance.SetBuildType(BuildOption.Platform);
                    break;
                case BuildOption.Stairs:
                    BuildingManager.Instance.SetBuildType(BuildOption.Stairs);
                    break;
                case BuildOption.DestroyMode:
                    BuildingManager.Instance.SetBuildType(BuildOption.DestroyMode);
                    break;
                case BuildOption.RepairMode:
                    BuildingManager.Instance.SetBuildType(BuildOption.RepairMode);
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
