using UnityEngine;
using System;

public class PartCommands : MonoBehaviour
{
    // Called by GazeGestureManager when the user performs a Select gesture

    public void OnSelect()
    {
        CallOnParent(r => r.TogglePart());
    }

    public void OnStart()
    {
        CallOnParent(r => r.StartPart());
    }

    public void OnStop()
    {
        CallOnParent(r => r.StopPart());
    }

    public void OnQuick()
    {
        CallOnParent(r => r.isFast = true);
    }

    public void OnSlow()
    {
        CallOnParent(r => r.isFast = false);
    }

    public void OnReverse()
    {
        CallOnParent(r => r.ReversePart());
    }

    private void CallOnParent(Action<Rotate> f)
    {
        var rot = this.gameObject.GetComponentInParent<Rotate>();
        if (rot)
        {
            f(rot);
        }
    }
}