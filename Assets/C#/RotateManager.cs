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
    }

    // When a new user joins we want to send them the relative transform for the anchor if we have it.

    private void Instance_SessionJoined(object sender, SharingSessionTracker.SessionJoinedEventArgs e)
    {
        BroadcastAll();
    }

    void BroadcastAll()
    {
        foreach (var part in partList)
        {
            RobotMessages.Instance.SendPartRotate(part.partNumber, part.rot, part.speed, part.isFast, part.isStopped);
        }
    }

    void OnPartRotate(NetworkInMessage msg)
    {
        // We read the user ID but we don't use it here

        msg.ReadInt64();

        var number = msg.ReadInt16();
        var part = partList[number];

        part.rot = msg.ReadFloat();
        part.speed = msg.ReadFloat();
        part.isFast = msg.ReadInt16() > 0;

        // We don't just set the flag as there's audio to control

        var stopped = msg.ReadInt16();
        if (stopped > 0)
            part.StopPart(true);
        else
            part.StartPart(true);       
    }
}