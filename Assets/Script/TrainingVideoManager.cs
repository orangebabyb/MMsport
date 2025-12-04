using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;

public class TrainingVideoManager : MonoBehaviour
{
    public VideoPlayer videoPlayer;

    // Video clips (Video1, Video2, Video3, etc.)
    public VideoClip VIDEO1;
    public VideoClip VIDEO2;
    public VideoClip VIDEO3;
    public VideoClip VIDEO4;
    public VideoClip VIDEO5;
    public VideoClip VIDEO6;

    // Dictionary to connect ButtonName -> VideoClip
    private Dictionary<string, VideoClip> videoMap;

    void Start()
    {
        InitializeVideoMap();
        PlaySelectedVideo();
    }

    void InitializeVideoMap()
    {
        videoMap = new Dictionary<string, VideoClip>()
        {
            { "MountainClimber", VIDEO1 },
            { "Plank", VIDEO2 },
            { "Superman", VIDEO3 },
            { "V-Crunch", VIDEO4 },
            { "Back Core", VIDEO5 },
            { "Bridge", VIDEO6 },
        };
    }

    void PlaySelectedVideo()
    {
        string selected = TrainingMode.SelectedTrainingMode;

        if (videoMap.ContainsKey(selected))
        {
            videoPlayer.clip = videoMap[selected];
            videoPlayer.Play();
        }
        else
        {
            Debug.LogError("No video mapped for: " + selected);
        }
    }
}