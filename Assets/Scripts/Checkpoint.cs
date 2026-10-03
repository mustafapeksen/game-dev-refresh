using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private Animator checkpointAnimator;
    private bool isActive;

    void Awake()
    {
        checkpointAnimator = GetComponent<Animator>();
    }

    public void Activate()
    {
        if (isActive)
            return;

        checkpointAnimator.SetTrigger("Active");
        isActive = true;
    }
}
