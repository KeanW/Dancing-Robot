using UnityEngine;
using System.Net;

public class Dance : MonoBehaviour {

    private bool dancing = false;
    private bool update = false;

    private int port = 4444;
    private TcpServer s;

    void Start()
    {
        s = new TcpServer(10, 100);
        s.Init(ReceiveCompleted);
        s.Start(new IPEndPoint(IPAddress.Any, port));
    }

    internal void ReceiveCompleted()
    {
        update = true;
    }

    void Update()
    {
        if (update)
        {
            if (dancing)
            {
                this.BroadcastMessage("OnReverse");
                update = false;
            }
            else
            {
                OnStartDancing();
            }
        }
    }

    public void OnStartDancing()
    {
        dancing = true;
        this.BroadcastMessage("OnStart");
    }

    public void OnStopDancing()
    {
        dancing = false;
        this.BroadcastMessage("OnStop");
    }
}