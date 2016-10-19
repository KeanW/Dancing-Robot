using HoloToolkit.Sharing;
using HoloToolkit.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.WSA;
using UnityEngine.VR.WSA.Sharing;

/// <summary>
/// Manages creating anchors and sharing the anchors with other clients.
/// </summary>
public class AnchorManager : Singleton<AnchorManager>
{
    /// <summary>
    /// Enum to track the progress through establishing a shared coordinate system.
    /// </summary>
    enum ImportExportState
    {
        // Overall states
        Start,
        Ready,
        Failed,
        // AnchorStore values
        ReadyToInitialize,
        RoomApiInitialized,
        // Anchor creation values
        InitialAnchorRequired,
        WaitingForAnchorLocation,
        CreatingInitialAnchor,
        ReadyToExportInitialAnchor,
        UploadingInitialAnchor,
        // Anchor values
        DataRequested,
        DataReady,
        Importing,
        Zeroing
    }

    ImportExportState currentState = ImportExportState.Start;

    public string StateName
    {
        get
        {
            return currentState.ToString();
        }
    }

    public bool AnchorEstablished
    {
        get
        {
            return currentState == ImportExportState.Ready;
        }
    }

    private AudioSource audioSource = null;

    //private bool anchoring = false;
    private bool placed = false;

    /// <summary>
    /// WorldAnchorTransferBatch is the primary object in serializing/deserializing anchors.
    /// </summary>
    WorldAnchorTransferBatch sharedAnchorInterface;

    /// <summary>
    /// Keeps track of stored anchor data blob.
    /// </summary>
    byte[] rawAnchorData = null;

    /// <summary>
    /// Keeps track of the name of the anchor we are exporting.
    /// </summary>
    string exportingAnchorName { get; set; }

    /// <summary>
    /// The datablob of the anchor.
    /// </summary>
    List<byte> exportingAnchorBytes = new List<byte>();

    /// <summary>
    /// Keeps track of if the sharing service is ready.
    /// We need the sharing service to be ready so we can
    /// upload and download data for sharing anchors.
    /// </summary>
    bool sharingServiceReady = false;

    /// <summary>
    /// The room manager API for the sharing service.
    /// </summary>
    RoomManager roomManager;

    /// <summary>
    /// Keeps track of the current room we are connected to.  Anchors
    /// are kept in rooms.
    /// </summary>
    Room currentRoom;

    /// <summary>
    /// Sometimes we'll see a really small anchor blob get generated.
    /// These tend to not work, so we have a minimum trustable size.
    /// </summary>
    const uint minTrustworthySerializedAnchorDataSize = 100000;

    /// <summary>
    /// Some room ID for indicating which room we are in.
    /// </summary>
    const long roomID = 8675309;

    /// <summary>
    /// Provides updates when anchor data is uploaded/downloaded.
    /// </summary>
    RoomManagerAdapter roomManagerCallbacks;

    private void Start()
    {
        Debug.Log("Import Export Manager starting");

        currentState = ImportExportState.ReadyToInitialize;

        //Wait for a notification that the sharing manager has been initialized (connected to sever)
        SharingStage.Instance.SharingManagerConnected += SharingManagerConnected;

        audioSource = this.gameObject.GetComponent<AudioSource>();

        //anchoring = SharingSessionTracker.Instance.UserIds.Count == 1;
        placed = false;
    }

    private void OnDestroy()
    {
        if (roomManagerCallbacks != null)
        {
            roomManagerCallbacks.AnchorsDownloadedEvent -= OnAnchorsDownloaded;
            roomManagerCallbacks.AnchorUploadedEvent -= RoomManagerCallbacks_AnchorUploaded;

            if (roomManager != null)
            {
                roomManager.RemoveListener(roomManagerCallbacks);
            }
        }
    }

    private void SharingManagerConnected(object sender, EventArgs e)
    {
        // Setup the room manager callbacks.
        roomManager = SharingStage.Instance.Manager.GetRoomManager();
        roomManagerCallbacks = new RoomManagerAdapter();

        roomManagerCallbacks.AnchorsDownloadedEvent += OnAnchorsDownloaded;
        roomManagerCallbacks.AnchorUploadedEvent += RoomManagerCallbacks_AnchorUploaded;
        roomManager.AddListener(roomManagerCallbacks);

        // We will register for session joined to indicate when the sharing service
        // is ready for us to make room related requests.
        SharingSessionTracker.Instance.SessionJoined += OnSessionJoined;
    }

    /// <summary>
    /// Called when anchor upload operations complete.
    /// </summary>
    private void RoomManagerCallbacks_AnchorUploaded(bool successful, XString failureReason)
    {
        if (successful)
        {
            currentState = ImportExportState.Ready;
        }
        else
        {
            Debug.Log("Upload failed " + failureReason);
            currentState = ImportExportState.Failed;
        }
    }

    /// <summary>
    /// Called when anchor download operations complete.
    /// </summary>
    private void OnAnchorsDownloaded(bool successful, AnchorDownloadRequest request, XString failureReason)
    {
        // If we downloaded anchor data successfully we should import the data.
        if (successful)
        {
            int datasize = request.GetDataSize();
            Debug.Log(datasize + " bytes ");
            rawAnchorData = new byte[datasize];

            request.GetData(rawAnchorData, datasize);
            currentState = ImportExportState.DataReady;
        }
        else
        {
            // If we failed, we can ask for the data again.
            Debug.Log("Anchor DL failed " + failureReason);
            MakeAnchorDataRequest();
        }
    }

    /// <summary>
    /// Called when a user (including the local user) joins a session.
    /// In this case we are using this event to signal that the sharing service is
    /// ready for us to make room related requests.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnSessionJoined(object sender, SharingSessionTracker.SessionJoinedEventArgs e)
    {
        // We don't need to get this event anymore.
        SharingSessionTracker.Instance.SessionJoined -= OnSessionJoined;

        // We still wait to wait a few seconds for everything to settle.
        Invoke("MarkSharingServiceReady", 5);
    }

    private void MarkSharingServiceReady()
    {
        sharingServiceReady = true;
    }

    /// <summary>
    /// Initializes the room api.
    /// </summary>
    private void InitRoomApi()
    {
        if (roomManager.GetRoomCount() == 0)
        {
            // There used to be a check here: we'll disable it

            //if (anchoring)
            {
                Debug.Log("Creating room ");
                // To keep anchors alive even if all users have left the session ...
                // Pass in true instead of false in CreateRoom.
                currentRoom = roomManager.CreateRoom(new XString("DefaultRoom"), roomID, false);
                currentState = ImportExportState.InitialAnchorRequired;
            }
        }
        else
        {
            Debug.Log("Joining room ");
            currentRoom = roomManager.GetRoom(0);
            roomManager.JoinRoom(currentRoom);
            currentState =
                (currentRoom.GetAnchorCount() > 0 ?
                    ImportExportState.RoomApiInitialized :
                    ImportExportState.InitialAnchorRequired);
        }

        if (currentRoom != null)
        {
            Debug.Log("In room: " + roomManager.GetCurrentRoom().GetName().GetString());
        }
    }

    public bool LocalUserHasLowestUserId()
    {
        long localUserId = RobotMessages.Instance.localUserID;
        foreach (long userid in SharingSessionTracker.Instance.UserIds)
        {
            if (userid < localUserId)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Kicks off the process of creating the shared space.
    /// </summary>
    private void StartAnchorProcess()
    {
        // First, are there any anchors in this room?
        int anchorCount = currentRoom.GetAnchorCount();

        Debug.Log(anchorCount + " anchors");

        // If there are anchors, we should attach to the first one.
        if (anchorCount > 0)
        {
            // Extract the name of the anchor.
            XString storedAnchorString = currentRoom.GetAnchorName(0);
            string storedAnchorName = storedAnchorString.GetString();

            // Attempt to attach to the anchor in our local anchor store.
            //if (AttachToCachedAnchor(storedAnchorName) == false)
            {
                Debug.Log("Starting room download");
                // If we cannot find the anchor by name, we will need the full data blob.
                MakeAnchorDataRequest();
            }
        }
    }

    /// <summary>
    /// Kicks off getting the datablob required to import the shared anchor.
    /// </summary>
    private void MakeAnchorDataRequest()
    {
        if (roomManager.DownloadAnchor(currentRoom, currentRoom.GetAnchorName(0)))
        {
            currentState = ImportExportState.DataRequested;
        }
        else
        {
            Debug.Log("Couldn't make the download request.");
            currentState = ImportExportState.Failed;
        }
    }

    private void Update()
    {
        switch (currentState)
        {
            // If the local anchor store is initialized.
            case ImportExportState.ReadyToInitialize:
                if (sharingServiceReady)
                {
                    InitRoomApi();
                }
                break;
            case ImportExportState.RoomApiInitialized:
                StartAnchorProcess();
                break;
            case ImportExportState.DataReady:
                // DataReady is set when the anchor download completes.
                currentState = ImportExportState.Importing;
                WorldAnchorTransferBatch.ImportAsync(rawAnchorData, OnImportComplete);
                break;
            case ImportExportState.InitialAnchorRequired:
                if (placed)
                    currentState = ImportExportState.CreatingInitialAnchor;
                break;
            case ImportExportState.CreatingInitialAnchor: // Added by Kean
                CreateAnchorLocally();
                break;
            case ImportExportState.WaitingForAnchorLocation: // Added by Kean
                break;
            case ImportExportState.ReadyToExportInitialAnchor:
                // We've created an anchor locally and it is ready to export.
                currentState = ImportExportState.UploadingInitialAnchor;
                Export();
                break;
        }
    }

    /// <summary>
    /// Starts establishing a new anchor.
    /// </summary>
    private void CreateAnchorLocally()
    {
        WorldAnchor anchor = GetComponent<WorldAnchor>();
        if (anchor == null)
        {
            anchor = gameObject.AddComponent<WorldAnchor>();
        }

        if (anchor.isLocated)
        {
            currentState = ImportExportState.ReadyToExportInitialAnchor;
        }
        else
        {
            currentState = ImportExportState.WaitingForAnchorLocation;
            anchor.OnTrackingChanged += OnTrackingChanged;
        }
    }

    /// <summary>
    /// Callback to trigger when an anchor has been 'found'.
    /// </summary>
    private void OnTrackingChanged(WorldAnchor self, bool located)
    {
        if (located)
        {
            Debug.Log("Found anchor, ready to export");
            currentState = ImportExportState.ReadyToExportInitialAnchor;
        }
        else
        {
            Debug.Log("Failed to locate local anchor (super bad!)");
            currentState = ImportExportState.Failed;
        }

        self.OnTrackingChanged -= OnTrackingChanged;
    }

    /// <summary>
    /// Called when a remote anchor has been deserialized
    /// </summary>
    /// <param name="status"></param>
    /// <param name="wat"></param>
    private void OnImportComplete(SerializationCompletionReason status, WorldAnchorTransferBatch wat)
    {
        if (status == SerializationCompletionReason.Succeeded && wat.GetAllIds().Length > 0)
        {
            Debug.Log("Import complete");

            string first = wat.GetAllIds()[0];
            Debug.Log("Anchor name: " + first);

            // Try zeroing out the local position & rotation

            gameObject.transform.localPosition = new Vector3();
            gameObject.transform.localRotation = new Quaternion();

            WorldAnchor anchor = wat.LockObject(first, gameObject);
            currentState = ImportExportState.Ready;

            if (audioSource != null)
                audioSource.Play();
        }
        else
        {
            Debug.Log("Import fail");
            currentState = ImportExportState.DataReady;
        }
    }

    /// <summary>
    /// Exports the currently created anchor.
    /// </summary>
    private void Export()
    {
        WorldAnchor anchor = GetComponent<WorldAnchor>();

        if (anchor == null)
        {
            Debug.Log("We should have made an anchor by now...");
            return;
        }

        exportingAnchorName = "robot-placement";

        sharedAnchorInterface = new WorldAnchorTransferBatch();
        sharedAnchorInterface.AddWorldAnchor(exportingAnchorName, anchor);
        WorldAnchorTransferBatch.ExportAsync(sharedAnchorInterface, WriteBuffer, OnExportComplete);
    }

    /// <summary>
    /// Called by the WorldAnchorTransferBatch as anchor data is available.
    /// </summary>
    /// <param name="data"></param>
    public void WriteBuffer(byte[] data)
    {
        exportingAnchorBytes.AddRange(data);
    }

    /// <summary>
    /// Called by the WorldAnchorTransferBatch when anchor exporting is complete.
    /// </summary>
    /// <param name="status"></param>
    private void OnExportComplete(SerializationCompletionReason status)
    {
        if (status == SerializationCompletionReason.Succeeded && exportingAnchorBytes.Count > minTrustworthySerializedAnchorDataSize)
        {
            Debug.Log("Uploading anchor: " + exportingAnchorName);
            roomManager.UploadAnchor(
                currentRoom,
                new XString(exportingAnchorName),
                exportingAnchorBytes.ToArray(),
                exportingAnchorBytes.Count);
        }
        else
        {
            Debug.Log("This anchor didn't work, trying again");
            currentState = ImportExportState.InitialAnchorRequired;
        }
    }

    public bool PlaceAnchor()
    {
        if (currentState == ImportExportState.InitialAnchorRequired)
        {
            currentState = ImportExportState.CreatingInitialAnchor;
            //anchoring = true;
            return true;
        }
        else
        {
            placed = true;
        }
        return false;
    }

    /*
    public void UpdateAnchor()
    {
        if (currentState == ImportExportState.Ready)
        {
            currentState = ImportExportState.RoomApiInitialized;
        }
    }
    */

    public void RemoveAnchor()
    {
        var anchor = gameObject.GetComponent<WorldAnchor>();
        if (anchor != null)
        {
            DestroyImmediate(anchor);
        }
    }

    public void ReplaceAnchor()
    {
        var anchor = gameObject.GetComponent<WorldAnchor>();
        if (anchor == null)
        {
            gameObject.AddComponent<WorldAnchor>();
        }
    }

    public bool IsAnchored()
    {
        var anchor = gameObject.GetComponent<WorldAnchor>();
        return anchor != null && anchor.isLocated;
    }
}
