using UnityEngine;

public class CheckpointSystem : MonoBehaviour
{
    private Rigidbody2D playerRigidbody2d;
    private Vector2 checkpointPosition;
    [SerializeField]
    private Vector2 offset;
    private PlayerMovement playerMovement;
    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerRigidbody2d = GetComponent<Rigidbody2D>();
        checkpointPosition = playerRigidbody2d.position;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Hazard"))
        {
            Respawn();
        }
        if (collision.CompareTag("Checkpoint"))
        {
            SetCheckpointPosition(collision);
        }
    }

    private void SetCheckpointPosition(Collider2D collision)
    {
        checkpointPosition = collision.transform.position;
    }

    private void Respawn()
    {
        playerRigidbody2d.position = checkpointPosition + offset;
        playerMovement.BeginRespawn();
    }
}
