using System;
using UnityEngine;
using Cinemachine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] public CameraState CameraState = CameraState.ThirdPerson;
    [SerializeField] private CinemachineVirtualCamera _fpsCamera;
    [SerializeField] private CinemachineFreeLook _tpsCamera;

    public Action OnChangePerspective;

    public void SwitchCamera()
    {
        if (CameraState == CameraState.ThirdPerson)
        {
            CameraState = CameraState.FirstPerson;
            _tpsCamera.gameObject.SetActive(false);
            _fpsCamera.gameObject.SetActive(true);
        }
        else
        {
            CameraState = CameraState.ThirdPerson;
            _tpsCamera.gameObject.SetActive(true);
            _fpsCamera.gameObject.SetActive(false);
        }

        if (OnChangePerspective != null)
            OnChangePerspective();
    }

    public void SetFPSClampedCamera(bool isClamped, Vector3 playerRotation)
    {
        if (_fpsCamera == null) return;

        CinemachinePOV pov = _fpsCamera.GetCinemachineComponent<CinemachinePOV>();
        if (pov == null) return;

        if (isClamped)
        {
            pov.m_HorizontalAxis.m_Wrap = false;
            pov.m_HorizontalAxis.m_MinValue = playerRotation.y - 45;
            pov.m_HorizontalAxis.m_MaxValue = playerRotation.y + 45;
        }
        else
        {
            pov.m_HorizontalAxis.m_MinValue = -180;
            pov.m_HorizontalAxis.m_MaxValue = 180;
            pov.m_HorizontalAxis.m_Wrap = true;
        }
    }

    public void SetTPSFieldOfView(float fieldOfView)
    {
        if (_tpsCamera != null)
            _tpsCamera.m_Lens.FieldOfView = fieldOfView;
    }
}