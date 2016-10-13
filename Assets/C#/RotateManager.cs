using UnityEngine;
using HoloToolkit.Sharing;

public class RotateManager : MonoBehaviour
{
    public int parts = 6;
    private Rotate[] partList;

    void Start()
    {
        // Get all the Rotate objects

        var partObjects = gameObject.GetComponentsInChildren<Rotate>();

        // Sort them into a new array based on partNumber

        partList = new Rotate[parts];
        foreach(var partObj in partObjects)
        {
            partList[partObj.partNumber] = partObj;
        }

        RobotMessages.Instance.MessageHandlers[RobotMessages.RobotMessageID.PartRotate] = this.OnPartRotate;

        SharingSessionTracker.Instance.SessionJoined += this.OnSessionJoined;
    }

    private void OnSessionJoined(object sender, SharingSessionTracker.SessionJoinedEventArgs e)
    {
        if (AnchorManager.Instance.Creator)
            BroadcastAll();
    }

    private void BroadcastAll()
    {
        foreach (var part in partList)
        {
            RobotMessages.Instance.SendPartRotate(part.partNumber, part.rot, part.speed, part.isFast, part.isStopped);
        }
    }

    private void OnPartRotate(NetworkInMessage msg)
    {
        // We read the user ID but we don't use it here

        msg.ReadInt64();

        // Get the part number and then the part based on it

        var number = msg.ReadInt16();
        var part = partList[number];
        part.SetData(msg.ReadFloat(), msg.ReadFloat(), msg.ReadInt16() > 0, msg.ReadInt16() > 0);
    }
}