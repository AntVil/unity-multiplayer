using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoiceChat : MonoBehaviour
{
    public int frequency = 44100;
    AudioClip recording;
    int lastPos, pos;

    void Start()
    {
        recording = Microphone.Start(null, true, 1, frequency);
        
        AudioSource audio = GetComponent<AudioSource>();
        audio.clip = AudioClip.Create("test", 1 * frequency, recording.channels, frequency, false);
        audio.loop = true;

    }

    void Update()
    {
        if ((pos = Microphone.GetPosition(null)) > 0)
        {
            if (lastPos > pos) lastPos = 0;

            if (pos - lastPos > 0)
            {
                // Allocate the space for the sample.
                float[] sample = new float[(pos - lastPos) * recording.channels];

                // Get the data from microphone.
                recording.GetData(sample, lastPos);

                // Put the data in the audio source.
                AudioSource audio = GetComponent<AudioSource>();
                audio.clip.SetData(sample, lastPos);

                if (!audio.isPlaying) audio.Play();

                lastPos = pos;
            }
        }
    }

    void OnDestroy()
    {
        Microphone.End(null);
    }
}
