using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public Slider _musicSlider;
    public Slider _sfxSlider;

    void Start()
    {
        _musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1.0f);
        AudioManager.Instance.MusicVolume(_musicSlider.value);

        _sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1.0f);
        AudioManager.Instance.SFXVolume(_sfxSlider.value);
    }

    public void ToggleMusic()
    {
        AudioManager.Instance.ToggleMusic();
    }

    public void ToggleSFX()
    {
        AudioManager.Instance.ToggleSFX();
    }

    public void MusicVolume()
    {
        AudioManager.Instance.MusicVolume(_musicSlider.value);
    }

    public void SFXVolume()
    {
        AudioManager.Instance.SFXVolume(_sfxSlider.value);
    }
}
