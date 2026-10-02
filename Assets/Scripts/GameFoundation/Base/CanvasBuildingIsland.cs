using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    /// <summary>Scene-authored Canvas building art; construction changes appearance, never layout.</summary>
    public sealed class CanvasBuildingIsland : MonoBehaviour
    {
        [SerializeField] private BaseBuildingConstruction construction;
        [SerializeField] private Image buttonBackground;
        [SerializeField] private GameObject buttonLabel;
        [SerializeField] private Image[] buildingImages;
        [SerializeField] private Material unbuiltTint;
        private void OnEnable() => BuildingUpgradeService.Changed += Refresh;
        private void OnDisable() => BuildingUpgradeService.Changed -= Refresh;
        private void Start() => Refresh();
        private void Refresh()
        {
            bool built=construction==null || construction.IsBuilt;
            if(buttonBackground!=null){buttonBackground.enabled=built;buttonBackground.raycastTarget=built;}
            if(buttonLabel!=null)buttonLabel.SetActive(built);
            Color tint=!built && unbuiltTint!=null ? unbuiltTint.GetColor("_Color") : Color.white;
            foreach(var image in buildingImages)if(image!=null)image.color=tint;
        }
    }
}
