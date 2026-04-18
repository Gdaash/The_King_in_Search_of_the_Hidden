using UnityEngine;

public class PauseButtonHandler : MonoBehaviour {
    private ResourceRequester _requester;

    void Start() => _requester = GetComponentInParent<ResourceRequester>();

    void OnMouseDown() {
        if (_requester != null) _requester.TogglePause();
    }
}