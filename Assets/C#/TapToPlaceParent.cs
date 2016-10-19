using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Sharing;

public class TapToPlaceParent : Singleton<TapToPlaceParent>
{
    private AudioSource audioSource;
    private bool placing = false;
    private bool inControl = false;
    private bool anchored = false;

    void Start()
    {
        // We care about getting updates for the anchor transform

        RobotMessages.Instance.MessageHandlers[RobotMessages.RobotMessageID.RobotTransform] = this.OnRobotTransfrom;

        // And when a new user join we will send the anchor transform we have

        SharingSessionTracker.Instance.SessionJoined += this.OnSessionJoined;

        audioSource = this.gameObject.GetComponent<AudioSource>();

        anchored = false;
    }

    // Update is called once per frame

    void Update()
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

    // Called by GazeGestureManager when the user performs a Select gesture

    public void OnSelect()
    {
        // On each Select gesture, toggle whether the user is in placing mode

        placing = !placing;

        if (placing)
        {
            // If the user is in placing mode, display the spatial mapping mesh

            AnchorManager.Instance.RemoveAnchor();

            SpatialMapping.Instance.DrawVisualMeshes = true;
            inControl = false;
        }
        else
        {
            // If the user is not in placing mode, hide the spatial mapping mesh

            anchored = AnchorManager.Instance.PlaceAnchor(); // This should fire an update to other devices

            SpatialMapping.Instance.DrawVisualMeshes = false;
            inControl = true;

            //RobotMessages.Instance.SendUpdateAnchor();
            if (anchored)
            {
                Debug.Log(string.Format("Sent position: {0}, {1}, {2}", transform.parent.localPosition.x, transform.parent.localPosition.y, transform.parent.localPosition.z));
                RobotMessages.Instance.SendRobotTransform(transform.parent.localPosition, transform.parent.localRotation);
            }
        }
    }

    // When a new user joins we want to send them the relative transform for the anchor if we have it.

    private void OnSessionJoined(object sender, SharingSessionTracker.SessionJoinedEventArgs e)
    {
        if (inControl && AnchorManager.Instance.IsAnchored())
        {
            RobotMessages.Instance.SendRobotTransform(transform.parent.localPosition, transform.parent.localRotation);
        }
    }

    // When a remote system has a transform for us, we'll get it here.

    private void OnRobotTransfrom(NetworkInMessage msg)
    {
        // We read the user ID but we don't use it here

        msg.ReadInt64();

        if (AnchorManager.Instance.IsAnchored())
        {
            AnchorManager.Instance.RemoveAnchor();

            transform.parent.localPosition = RobotMessages.Instance.ReadVector3(msg);
            transform.parent.localRotation = RobotMessages.Instance.ReadQuaternion(msg);

            Debug.Log(string.Format("Loaded position: {0}, {1}, {2}", transform.parent.localPosition.x, transform.parent.localPosition.y, transform.parent.localPosition.z));
            //if (audioSource != null)
            //    audioSource.Play();

            AnchorManager.Instance.ReplaceAnchor();

            inControl = false;
        }
        else
        {
            RobotMessages.Instance.ReadVector3(msg);
            RobotMessages.Instance.ReadQuaternion(msg);
        }
    }
}