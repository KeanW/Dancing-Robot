using UnityEngine;
using HoloToolkit.Sharing;

public class Scale : MonoBehaviour
{
    private const float DefaultSizeFactor = 2.0f;

    [Tooltip("Size multiplier to use when scaling the object up and down.")]
    public float SizeFactor = DefaultSizeFactor;

    private void Start()
    {
        if (SizeFactor <= 0.0f)
        {
            SizeFactor = DefaultSizeFactor;
        }

        RobotMessages.Instance.MessageHandlers[RobotMessages.RobotMessageID.RobotScale] = this.OnRobotScale;

        //SharingSessionTracker.Instance.SessionJoined += this.OnSessionJoined;
    }

    public void OnBigger()
    {
        Vector3 scale = transform.localScale;
        scale *= SizeFactor;
        transform.localScale = scale;
        RobotMessages.Instance.SendRobotScale(scale);
    }

    public void OnSmaller()
    {
        Vector3 scale = transform.localScale;
        scale /= SizeFactor;
        transform.localScale = scale;
        RobotMessages.Instance.SendRobotScale(scale);
    }

    private void OnSessionJoined(object sender, SharingSessionTracker.SessionJoinedEventArgs e)
    {
        if (AnchorManager.Instance.Creator)
            RobotMessages.Instance.SendRobotScale(transform.localScale);
    }

    void OnRobotScale(NetworkInMessage msg)
    {
        // We read the user ID but we don't use it here

        msg.ReadInt64();

        transform.localScale = RobotMessages.Instance.ReadVector3(msg);
    }
}