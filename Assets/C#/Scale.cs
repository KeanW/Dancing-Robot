using UnityEngine;
using HoloToolkit.Sharing;

public class Scale : MonoBehaviour
{
    private const float DefaultSizeFactor = 2.0f;
    private Vector3 scaleToApply;
    private bool scalePending = false;

    [Tooltip("Size multiplier to use when scaling the object up and down.")]
    public float SizeFactor = DefaultSizeFactor;

    private void Start()
    {
        if (SizeFactor <= 0.0f)
        {
            SizeFactor = DefaultSizeFactor;
        }

        RobotMessages.Instance.MessageHandlers[RobotMessages.RobotMessageID.RobotScale] = this.OnRobotScale;

        SharingSessionTracker.Instance.SessionJoined += this.OnSessionJoined;
    }

    private void Update()
    {
        if (scalePending)
        {
            transform.localScale = scaleToApply;
            scalePending = false;
        }
    }

    public void OnBigger()
    {
        Vector3 scale = transform.localScale;
        scale *= SizeFactor;
        scaleToApply = scale;
        scalePending = true;
        RobotMessages.Instance.SendRobotScale(scale);
    }

    public void OnSmaller()
    {
        Vector3 scale = transform.localScale;
        scale /= SizeFactor;
        scaleToApply = scale;
        scalePending = true;
        RobotMessages.Instance.SendRobotScale(scale);
    }

    private void OnSessionJoined(object sender, SharingSessionTracker.SessionJoinedEventArgs e)
    {
        if (RobotMessages.Instance.localUserID != e.joiningUser.GetID())
            RobotMessages.Instance.SendRobotScale(transform.localScale);
    }

    void OnRobotScale(NetworkInMessage msg)
    {
        // We read the user ID but we don't use it here

        msg.ReadInt64();

        scaleToApply = RobotMessages.Instance.ReadVector3(msg);
        scalePending = true;
    }
}