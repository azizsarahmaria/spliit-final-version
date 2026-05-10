
using Unity.Cinemachine;
using UnityEngine;

public class CameraFollowChanger : MonoBehaviour
{
    public CinemachineVirtualCamera virtualCamera;
    

    private void Start()
    {
        virtualCamera = this.GetComponent<CinemachineVirtualCamera>();
    }
    public void ChangeFollowTarget(Transform transform)
    {
        virtualCamera.Follow = transform;
    }
}