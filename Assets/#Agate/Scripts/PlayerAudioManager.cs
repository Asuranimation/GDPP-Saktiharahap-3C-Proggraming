using UnityEngine;

public class PlayerAudioManager : MonoBehaviour
{
    [SerializeField]
    private AudioSource _footstepSfx;

    private void PlayFootstepSfx()
    {
        _footstepSfx.Play();
    }
}
