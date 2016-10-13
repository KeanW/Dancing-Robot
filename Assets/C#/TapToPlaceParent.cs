using UnityEngine;
using HoloToolkit.Sharing;

public class TapToPlaceParent : MonoBehaviour
{
    AudioSource audioSource;
    bool placing = false;

    // Called by GazeGestureManager when the user performs a Select gesture

    public void OnSelect()
    {
        // On each Select gesture, toggle whether the user is in placing mode

        placing = !placing;

        // If the user is in placing mode, display the spatial mapping mes

        if (placing)
        {
            AnchorManager.Instance.RemoveLastAnchor();

            SpatialMapping.Instance.DrawVisualMeshes = true;

            GotTransform = false;
        }
        // If the user is not in placing mode, hide the spatial mapping mesh
        else
        {
            AnchorManager.Instance.ReplaceLastAnchor();

            SpatialMapping.Instance.DrawVisualMeshes = false;

            // Note that we have a transform

            GotTransform = true;

            // And send it to our friends

            RobotMessages.Instance.SendRobotTransform(transform.parent.localPosition, transform.parent.localRotation);
        }
    }

    // Update is called once per frame

    void Update()
    {
        if (GotTransform)
        {
            if (AnchorManager.Instance.AnchorEstablished && !animationPlayed)
            {
                //SendMessage("OnSelect");
                animationPlayed = true;

                //if (audioSource)
                //    audioSource.Play();
            }
        }
        else
        {
            // If the user is in placing mode, update the placement to match the user's gaze

            if (placing)
            {
                // Do a raycast into the world that will only hit the Spatial Mapping mesh

                var headPosition = Camera.main.transform.position;
                var gazeDirection = Camera.main.transform.forward;

                RaycastHit hitInfo;
                if (Physics.Raycast(headPosition, gazeDirection, out hitInfo,
                    30.0f, SpatialMapping.PhysicsRaycastMask))
                {
                    // Move this object's parent object to
                    // where the raycast hit the Spatial Mapping mesh

                    this.transform.parent.position = hitInfo.point;

                    // Rotate this object's parent object to face the user

                    Quaternion toQuat = Camera.main.transform.localRotation;
                    toQuat.x = 0;
                    toQuat.z = 0;
                    this.transform.parent.rotation = toQuat;
                }
            }
        }
    }
    
    // Tracks if we have been sent a transform for the anchor model.
    // The anchor model is rendered relative to the actual anchor.

    public bool GotTransform { get; private set; }

    private bool animationPlayed = false;

    void Start()
    {
        // We care about getting updates for the anchor transform.
        RobotMessages.Instance.MessageHandlers[RobotMessages.RobotMessageID.RobotTransform] = this.OnRobotTransfrom;

        // And when a new user join we will send the anchor transform we have.
        SharingSessionTracker.Instance.SessionJoined += Instance_SessionJoined;

        audioSource = this.gameObject.GetComponent<AudioSource>();
    }

    // When a new user joins we want to send them the relative transform for the anchor if we have it.

    private void Instance_SessionJoined(object sender, SharingSessionTracker.SessionJoinedEventArgs e)
    {
        if (GotTransform)
        {
            RobotMessages.Instance.SendRobotTransform(transform.parent.localPosition, transform.parent.localRotation);
        }
    }

    // When a remote system has a transform for us, we'll get it here.

    void OnRobotTransfrom(NetworkInMessage msg)
    {
        // We read the user ID but we don't use it here

        msg.ReadInt64();

        transform.parent.localPosition = RobotMessages.Instance.ReadVector3(msg);
        transform.parent.localRotation = RobotMessages.Instance.ReadQuaternion(msg);

        if (!GotTransform)
        {
            //SendMessage("OnSelect");
            if (audioSource)
                audioSource.Play();
        }

        GotTransform = true;
    }
}