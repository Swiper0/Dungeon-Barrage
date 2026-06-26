using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PowerUpSelectionUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject selectionPanel;
    public Text titleText;
    public Text waveText;

    [Header("Option Slots")]
    public PowerUpOptionUI option1;
    public PowerUpOptionUI option2;
    public PowerUpOptionUI option3;

    public void ShowSelection(PowerUpData[] options, int completedWave)
{
    selectionPanel.SetActive(true);
    waveText.text = $"Wave {completedWave} Complete!";

        option1.Setup(options[0], this);
        option2.Setup(options[1], this);
        option3.Setup(options[2], this);
    }

    public void OnOptionSelected(PowerUpData selected)
    {
        selectionPanel.SetActive(false);
        PowerUpManager.Instance.SelectPowerUp(selected);
    }
}