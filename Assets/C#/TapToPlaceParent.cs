using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Windows.Speech;
using HoloToolkit.Unity;
using HoloToolkit.Sharing;

public class TapToPlaceParent : MonoBehaviour
{
    bool placing = false;

    // Called by GazeGestureManager when the user performs a Select gesture
    public void OnSelect()
    {
        // On each Select gesture, toggle whether the user is in placing mode.
        placing = !placing;

        // If the user is in placing mode, display the spatial mapping mesh.
        if (placing)
        {
            SpatialMapping.Instance.DrawVisualMeshes = true;

            GotTransform = false;
        }
        // If the user is not in placing mode, hide the spatial mapping mesh.
        else
        {
            SpatialMapping.Instance.DrawVisualMeshes = false;

            // Note that we have a transform.
            GotTransform = true;

            // And send it to our friends.
            CustomMessages.Instance.SendStageTransform(transform.parent.localPosition, transform.parent.localRotation);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (GotTransform)
        {
            if (ImportExportAnchorManager.Instance.AnchorEstablished && animationPlayed == false)
            {
                // This triggers the animation sequence for the anchor model and 
                // puts the cool materials on the model.
                //SendMessage("OnSelect");
                animationPlayed = true;
            }
        }
        else
        {
            // If the user is in placing mode,
            // update the placement to match the user's gaze.

            if (placing)
            {
                // Do a raycast into the world that will only hit the Spatial Mapping mesh.
                var headPosition = Camera.main.transform.position;
                var gazeDirection = Camera.main.transform.forward;

                RaycastHit hitInfo;
                if (Physics.Raycast(headPosition, gazeDirection, out hitInfo,
                    30.0f, SpatialMapping.PhysicsRaycastMask))
                {
                    // Move this object's parent object to
                    // where the raycast hit the Spatial Mapping mesh.
                    this.transform.parent.position = hitInfo.point;

                    // Rotate this object's parent object to face the user.
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
        CustomMessages.Instance.MessageHandlers[CustomMessages.TestMessageID.StageTransform] = this.OnStageTransfrom;

        // And when a new user join we will send the anchor transform we have.
        SharingSessionTracker.Instance.SessionJoined += Instance_SessionJoined;
    }

    // When a new user joins we want to send them the relative transform for the anchor if we have it.

    private void Instance_SessionJoined(object sender, SharingSessionTracker.SessionJoinedEventArgs e)
    {
        if (GotTransform)
        {
            CustomMessages.Instance.SendStageTransform(transform.parent.localPosition, transform.parent.localRotation);
        }
    }

    Vector3 ProposeTransformPosition()
    {
        // Put the anchor 2m in front of the user.
        Vector3 retval = Camera.main.transform.position + Camera.main.transform.forward * 2;

        return retval;
    }

    // When a remote system has a transform for us, we'll get it here.

    void OnStageTransfrom(NetworkInMessage msg)
    {
        // We read the user ID but we don't use it here.
        msg.ReadInt64();

        transform.parent.localPosition = CustomMessages.Instance.ReadVector3(msg);
        transform.parent.localRotation = CustomMessages.Instance.ReadQuaternion(msg);

        // The first time, we'll want to send the message to the anchor to do its animation and
        // swap its materials.
        if (GotTransform == false)
        {
            //SendMessage("OnSelect");
        }

        GotTransform = true;
    }

    public void ResetStage()
    {
        // We'll use this later.
    }
}