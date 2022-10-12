using UnityEngine;

public class NetworkVoiceChat : Unity.Netcode.NetworkBehaviour
{
    public int frequency = 44100;
    public int channels = 1;

    private AudioClip recording;
    private int lastPos;
    private int pos;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            recording = Microphone.Start(null, true, 1, frequency);
        }
        else
        {
            AudioSource audio = GetComponent<AudioSource>();
            audio.clip = AudioClip.Create("networkRecording", 1 * frequency, channels, frequency, false);
            audio.loop = true;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            Microphone.End(null);
        }
    }

    public void Update()
    {
        if (IsOwner)
        {
            pos = Microphone.GetPosition(null);

            if (pos > 0)
            {
                if (lastPos > pos)
                {
                    lastPos = 0;
                }

                if (pos - lastPos > 0)
                {
                    // Allocate the space for the sample.
                    float[] sample = new float[(pos - lastPos) * channels];

                    // Get the data from microphone.
                    recording.GetData(sample, lastPos);

                    ShareAudioServerRpc(sample, lastPos);

                    lastPos = pos;
                }
            }
        }
    }

    [Unity.Netcode.ServerRpc]
    public void ShareAudioServerRpc(float[] sample, int lastPos)
    {
        if (!IsServer) return;

        ShareAudioClientRpc(sample, lastPos);
    }

    [Unity.Netcode.ClientRpc]
    public void ShareAudioClientRpc(float[] sample, int lastPos)
    {
        if (IsOwner) return;

        AudioSource audio = GetComponent<AudioSource>();

        // Put the data in the audio source.
        audio.clip.SetData(sample, lastPos);

        if (!audio.isPlaying) audio.Play();
    }
}

