using UnityEngine;
using System;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance;
    
    public int currentCoins { get; private set; }
    
    // Event yang dipanggil otomatis saat koin bertambah/berkurang agar UI langsung update
    public Action OnCoinsUpdated;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Aktifkan baris di bawah ini jika CoinManager ingin dipertahankan pindah-pindah scene
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }

        LoadCoins();
    }

    public void AddCoins(int amount)
    {
        currentCoins += amount;
        SaveCoins();
        OnCoinsUpdated?.Invoke(); // Update UI
    }

    // 🔥 Fungsi ini tinggal Anda panggil di skrip Shop nantinya
    // Contoh: if(CoinManager.Instance.SpendCoins(50)) { // Beli skin }
    public bool SpendCoins(int amount)
    {
        if (currentCoins >= amount)
        {
            currentCoins -= amount;
            SaveCoins();
            OnCoinsUpdated?.Invoke(); // Update UI
            return true;
        }
        Debug.Log("Koin tidak cukup!");
        return false;
    }

    private void SaveCoins()
    {
        PlayerPrefs.SetInt("PlayerCoins", currentCoins);
        PlayerPrefs.Save();
    }

    private void LoadCoins()
    {
        currentCoins = PlayerPrefs.GetInt("PlayerCoins", 0);
    }
}