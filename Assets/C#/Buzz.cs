using UnityEngine;
using System;

public class Buzz : MonoBehaviour {

    public int parts = 6;

    private int stopped = 0;
    private int max = 0;
    private AudioSource audioSource;

    void Start ()
    {
        // Calculate the sum of all possible part flags

        max = 0;
	    for (int i=0; i < parts; i++)
        {
            max += (int)Math.Pow(2, i);
        }

        // Store the audio source for later access

        audioSource = this.gameObject.GetComponent<AudioSource>();
    }

    public void StartPart(int part)
    {
        // If our part is on the stopped list, remove it

        int flag = (int)Math.Pow(2, part);
        if ((stopped & flag) > 0)
        {
            stopped -= flag;
        }
           
        // Start the audio if it isn't already playing

        if (audioSource && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    public void StopPart(int part)
    {
        // If our part isn't on the stopped list, add it

        int flag = (int)Math.Pow(2, part);
        if ((stopped & flag) == 0)
        {
            stopped += flag;
        }

        // If all parts are stopped, stop the audio

        if (audioSource && (stopped == max))
        {
            audioSource.Stop();
        }
    }
}
