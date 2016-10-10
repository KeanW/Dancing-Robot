using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class SpeechManager : MonoBehaviour
{
    KeywordRecognizer keywordRecognizer = null;
    Dictionary<string, System.Action> keywords = new Dictionary<string, System.Action>();
    GameObject placer = null;

    void Start()
    {
        placer = GameObject.FindGameObjectWithTag("Placement");

        keywords.Add("Halt", () => this.BroadcastMessage("OnStop"));

        keywords.Add("Move", () => this.BroadcastMessage("OnStart"));

        keywords.Add("Quick", () => this.BroadcastMessage("OnQuick"));

        keywords.Add("Slow", () => this.BroadcastMessage("OnSlow"));

        keywords.Add("Reverse", () => this.BroadcastMessage("OnReverse"));

        keywords.Add("Stop", () =>
        {
            var focusObject = GazeGestureManager.Instance.FocusedObject;
            if (focusObject != null)
            {
                // Call the OnStop method on just the focused object

                focusObject.SendMessage("OnStop");
            }
        });

        keywords.Add("Spin", () =>
        {
            var focusObject = GazeGestureManager.Instance.FocusedObject;
            if (focusObject != null)
            {
                // Call the OnStart method on just the focused object

                focusObject.SendMessage("OnStart");
            }
        });

        keywords.Add("Bigger", () => this.SendMessage("OnBigger"));

        keywords.Add("Smaller", () => this.SendMessage("OnSmaller"));

        keywords.Add("Begin Dancing", () => this.SendMessage("OnStartDancing"));

        keywords.Add("Stop Dancing", () => this.SendMessage("OnStopDancing"));

        keywords.Add("Reset", () => placer.SendMessage("OnSelect"));

        // Tell the KeywordRecognizer about our keywords

        keywordRecognizer = new KeywordRecognizer(keywords.Keys.ToArray());

        // Register a callback for the KeywordRecognizer and start recognizing!

        keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_OnPhraseRecognized;
        keywordRecognizer.Start();
    }

    private void KeywordRecognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        System.Action keywordAction;
        if (keywords.TryGetValue(args.text, out keywordAction))
        {
            keywordAction.Invoke();
        }
    }
}