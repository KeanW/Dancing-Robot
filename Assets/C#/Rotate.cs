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
    public bool isStopped = true;       // Flag to allow stopping

    // Internal variable to track overall rotation (if constrained)

    public float rot = 0f;
    private float diff = 0f;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = this.gameObject.GetComponent<AudioSource>();
    }

    public void StartPart(bool suppressBroadcast = false)
    {
        isStopped = false;
        if (audioSource != null)
            audioSource.Play();
        BroadcastData(suppressBroadcast);
    }

    public void ReversePart(bool suppressBroadcast = false)
    {
        speed = -speed;
        BroadcastData(suppressBroadcast);
    }

    public void StopPart(bool suppressBroadcast = false)
    {
        isStopped = true;
        if (audioSource)
            audioSource.Stop();
        BroadcastData(suppressBroadcast);
    }

    public void TogglePart(bool suppressBroadcast = false)
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
        BroadcastData(suppressBroadcast);
    }

    public void SetData(float rot, float speed, bool fast, bool stopped)
    {
        diff = rot - this.rot;
        this.speed = speed;
        isFast = fast;
        if (stopped)
            StopPart(true);
        else
            StartPart(true);
    }

    void BroadcastData(bool suppress)
    {
        if (!suppress)
        {
            RotateManager.Instance.TakeControl();
            RobotMessages.Instance.SendPartRotate(partNumber, rot, speed, isFast, isStopped);
        }
    }

    void Update()
    {
        if (isStopped && System.Math.Abs(diff) < 0.00001f)
            return;

        // Calculate the rotation amount as speed x time
        // (may get reduced to a smaller amount if near the angle limits)
        // Include the diff that's set by other clients

        var delta = (isStopped ? 0 : (speed * Time.deltaTime * (isFast ? 2f : 1f))) + diff;
        diff = 0f;

        // If we're constraining movement (via min & max angles)...

        var targetRot = rot + delta;

        if (minRot != maxRot)
        {
            // Then track the overall rotation

            if (targetRot < minRot)
            {
                // Don't go below the minimum angle

                delta = minRot - rot;
            }
            else if (targetRot > maxRot)
            {
                // Don't go above the maximum angle

                delta = maxRot - rot;
            }

            rot += delta;

            // And reverse the direction if we're at a limit

            if (rot <= minRot || rot >= maxRot)
            {
                speed = -speed;
            }
        }

        // Perform the rotation itself

        transform.Rotate(axis, delta);
    }
}