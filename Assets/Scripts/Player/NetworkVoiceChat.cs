using UnityEngine;
using System;

public class NetworkVoiceChat : Unity.Netcode.NetworkBehaviour{
    public int frequency = 8000;
    public int channels = 1;
    public float minAmplitude = 0;

    private int recordingFrequency;

    private AudioClip recording;
    private int lastPos;
    private int pos;

    private float frequencyFactor;
    private float inverseFrequencyFactor;

    public override void OnNetworkSpawn(){
        if(IsOwner){
            // get lowest possible sample rate (less downsample calculations)
            int minFreq;
            int maxFreq;
            Microphone.GetDeviceCaps(null, out minFreq, out maxFreq);
            recordingFrequency = Math.Max(minFreq, frequency);

            // precalculate factors for later use
            frequencyFactor = (float)(((double)frequency) / ((double)recordingFrequency));
            inverseFrequencyFactor = (float)(1.0 / frequencyFactor);
            
            recording = Microphone.Start(null, true, 1, recordingFrequency);
        }else{
            AudioSource audio = GetComponent<AudioSource>();
            audio.clip = AudioClip.Create("networkRecording", 1 * frequency, channels, frequency, false);
            audio.loop = true;
        }
    }

    public override void OnNetworkDespawn(){
        if(IsOwner){
            Microphone.End(null);
        }
    }

    public void Update(){
        if(IsOwner){
            pos = Microphone.GetPosition(null);

            if(pos > 0){
                if(lastPos > pos){
                    lastPos = 0;
                }

                if(pos - lastPos > 0){
                    // Allocate the space for the sample.
                    float[] sample = new float[(pos - lastPos) * channels];

                    // Get the data from microphone.
                    recording.GetData(sample, lastPos);

                    bool isRelevant = false;
                    for(int i=0;i<sample.Length;i++){
                        if(Math.Abs(sample[i]) > minAmplitude){
                            isRelevant = true;
                            break;
                        }
                    }

                    if(isRelevant){
                        // convert and send data to everybody
                        ShareAudioServerRpc(
                            GetNetworkCommonFrequency(sample),
                            (int)Math.Floor(((float)lastPos) * frequencyFactor)
                        );
                    }else{
                        ShareSilenceServerRpc(
                            sample.Length,
                            (int)Math.Floor(((float)lastPos) * frequencyFactor)
                        );
                    }

                    lastPos = pos;
                }
            }
        }
    }

    private float[] GetNetworkCommonFrequency(float[] sample){
        int outputSize = (int)Math.Floor(((float)sample.Length) * frequencyFactor);

        // downsample by taking the closest sample point to the left
        float[] output = new float[outputSize];
        float step = 0;
        for(int i=0;i<output.Length;i++){
            output[i] = sample[(int)Math.Floor(step)];
            step += inverseFrequencyFactor;
        }

        return output;
    }

    [Unity.Netcode.ServerRpc]
    private void ShareAudioServerRpc(float[] sample, int lastPos){
        if(!IsServer) return;

        ShareAudioClientRpc(sample, lastPos);
    }

    [Unity.Netcode.ClientRpc]
    private void ShareAudioClientRpc(float[] sample, int lastPos){
        if(IsOwner) return;

        AudioSource audio = GetComponent<AudioSource>();

        // Put the data in the audio source.
        audio.clip.SetData(sample, lastPos);

        if(!audio.isPlaying) audio.Play();
    }

    [Unity.Netcode.ServerRpc]
    private void ShareSilenceServerRpc(int silenceLength, int lastPos){
        if(!IsServer) return;

        ShareSilenceClientRpc(silenceLength, lastPos);
    }

    [Unity.Netcode.ClientRpc]
    private void ShareSilenceClientRpc(int silenceLength, int lastPos){
        if(IsOwner) return;

        AudioSource audio = GetComponent<AudioSource>();

        // Put empty data in the audio source.
        audio.clip.SetData(new float[silenceLength], lastPos);

        if(!audio.isPlaying) audio.Play();
    }
}
