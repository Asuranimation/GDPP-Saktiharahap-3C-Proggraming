using UnityEngine;

public class Deadzone : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CharacterController cc = other.GetComponent<CharacterController>();
            PlayerStateMachine psm = other.GetComponent<PlayerStateMachine>();

            if (cc != null)
            {
                cc.enabled = false;
                other.transform.position = spawnPoint.position;

                if (psm != null)
                {
                    psm.CurrentMovementY = 0;
                    psm.AppliedMovementY = 0;
                    psm.AppliedMovementX = 0;
                    psm.AppliedMovementZ = 0;
                }

                cc.enabled = true;
            }

            Debug.Log("Respawned");
        }
    }
}