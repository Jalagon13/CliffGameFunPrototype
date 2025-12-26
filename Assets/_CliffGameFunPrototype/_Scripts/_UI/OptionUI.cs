using UnityEngine;
using UnityEngine.UI;

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
            Debug.Log($"Selecting Option {_buildOption}");
            switch (_buildOption)
            {
                case BuildOption.Wall:
                    BuildingManager.Instance.SetBuildType(SelectedBuildType.Wall);
                    break;
                case BuildOption.Floor:
                    BuildingManager.Instance.SetBuildType(SelectedBuildType.Floor);
                    break;
                case BuildOption.Destroy:
                    BuildingManager.Instance.SetBuildType(SelectedBuildType.DestroyMode);
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
