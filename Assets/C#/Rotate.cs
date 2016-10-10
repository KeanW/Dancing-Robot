using UnityEngine;

public class Rotate : MonoBehaviour
{
    // Parameters provided by Unity that will vary per object

    public int partNumber = 0;          // Part number to help identify when all are stopped
    public float speed = 50f;           // Speed of the rotation
    public Vector3 axis = Vector3.up;   // Axis of rotation
    public float maxRot = 170f;         // Minimum angle of rotation (to contstrain movement)
    public float minRot = -170f;        // Maximim angle of rotation (if == min then unconstrained)
    public bool isFast = false;         // Flag to allow speed-up on selection
    public bool isStopped = true;      // Flag to allow stopping

    // Internal variable to track overall rotation (if constrained)

    private float rot = 0f;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = this.gameObject.GetComponent<AudioSource>();
    }

    public void StartPart()
    {
        isStopped = false;
        if (audioSource)
            audioSource.Play();
    }

    public void ReversePart()
    {
        speed = -speed;
    }

    public void StopPart()
    {
        isStopped = true;
        if (audioSource)
            audioSource.Stop();
    }

    public void TogglePart()
    {
        isStopped = !isStopped;
        if (audioSource)
        {
            if (isStopped)
            {
                audioSource.Stop();
            }
            else
            {
                ReversePart();
                audioSource.Play();
            }
        }
    }

    void Update()
    {
        if (isStopped)
            return;
        
        // Calculate the rotation amount as speed x time
        // (may get reduced to a smaller amount if near the angle limits)

        var locRot = speed * Time.deltaTime * (isFast ? 2f : 1f);

        // If we're constraining movement (via min & max angles)...

        if (minRot != maxRot)
        {
            // Then track the overall rotation

            if (locRot + rot < minRot)
            {
                // Don't go below the minimum angle

                locRot = minRot - rot;
            }
            else if (locRot + rot > maxRot)
            {
                // Don't go above the maximum angle

                locRot = maxRot - rot;
            }

            rot += locRot;

            // And reverse the direction if we're at a limit

            if (rot <= minRot || rot >= maxRot)
            {
                speed = -speed;
            }
        }

        // Perform the rotation itself

        transform.Rotate(axis, locRot);
    }
}