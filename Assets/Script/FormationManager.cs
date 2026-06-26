using System.Collections.Generic;
using UnityEngine;

public class FormationManager : MonoBehaviour
{
    public static FormationManager Instance;

    [Header("Slot Markers")]
    public Transform slotsParent;
    private List<Transform> slotMarkers = new List<Transform>();
    
    [Header("Screen Adaptation")]
    public bool autoAdjustToScreen = true;
    [Range(0.1f, 1f)]
    public float screenWidthUsage = 0.8f;
    public bool keepOriginalY = true;
    public float fixedYPosition = 2.5f;
    
    private HashSet<Transform> usedSlots = new HashSet<Transform>();
    private Camera cam;
    private Dictionary<Transform, Vector3> originalPositions = new Dictionary<Transform, Vector3>();
    
    private bool isInitialized = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
            
        cam = Camera.main;
    }
    
    void Start()
    {
        InitializeSlots();
    }
    
    void InitializeSlots()
    {
        if (isInitialized) return;
        
        CollectSlotMarkers();
        
        if (autoAdjustToScreen)
        {
            AdjustSlotsToScreen();
        }
        
        isInitialized = true;
        Debug.Log($"FormationManager initialized with {slotMarkers.Count} slots");
    }

    void CollectSlotMarkers()
    {
        slotMarkers.Clear();
        originalPositions.Clear();
        
        if (slotsParent == null)
        {
            Debug.LogError("Slots Parent is not assigned!");
            return;
        }
        
        foreach (Transform child in slotsParent)
        {
            if (child.gameObject.activeSelf)
            {
                slotMarkers.Add(child);
                originalPositions[child] = child.position;
                Debug.Log($"Slot found: {child.name} at {child.position}");
            }
        }
    }

    public void AdjustSlotsToScreen()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        
        if (slotMarkers.Count == 0) CollectSlotMarkers();
        if (slotMarkers.Count == 0) return;
        
        float screenHeight = cam.orthographicSize * 2f;
        float screenWidth = screenHeight * cam.aspect;
        float usableWidth = screenWidth * screenWidthUsage;
        
        float minOriginalX = float.MaxValue;
        float maxOriginalX = float.MinValue;
        
        foreach (Transform slot in slotMarkers)
        {
            Vector3 originalPos = originalPositions[slot];
            if (originalPos.x < minOriginalX) minOriginalX = originalPos.x;
            if (originalPos.x > maxOriginalX) maxOriginalX = originalPos.x;
        }
        
        float originalWidth = maxOriginalX - minOriginalX;
        if (originalWidth <= 0.01f) originalWidth = (slotMarkers.Count - 1) * 2.5f;
        
        foreach (Transform slot in slotMarkers)
        {
            Vector3 originalPos = originalPositions[slot];
            float normalizedX = (originalPos.x - minOriginalX) / originalWidth;
            float newX = -usableWidth/2f + (normalizedX * usableWidth);
            float newY = keepOriginalY ? originalPos.y : fixedYPosition;
            
            slot.position = new Vector3(newX, newY, originalPos.z);
        }
        
        Debug.Log($"Slots adjusted! Width: {usableWidth:F2}");
    }

    public (int index, Vector3 position) GetFreeSlot()
    {
        if (!isInitialized) InitializeSlots();
        
        // 🔥 BUAT LIST SLOT YANG KOSONG
        List<int> freeSlots = new List<int>();
        for (int i = 0; i < slotMarkers.Count; i++)
        {
            if (!usedSlots.Contains(slotMarkers[i]))
            {
                freeSlots.Add(i);
            }
        }
        
        // 🔥 JIKA ADA SLOT KOSONG, PILIH RANDOM
        if (freeSlots.Count > 0)
        {
            int randomIndex = freeSlots[Random.Range(0, freeSlots.Count)];
            Transform slot = slotMarkers[randomIndex];
            usedSlots.Add(slot);
            Debug.Log($"Random slot {randomIndex} assigned at {slot.position}");
            return (randomIndex, slot.position);
        }
        
        // Fallback kalau penuh
        Debug.LogWarning("No free slots! Using fallback");
        return (-1, new Vector3(Random.Range(-3f, 3f), 2f, 0));
    }

        // 🔥 METHOD BARU UNTUK NODE ENEMY
    public (int index, Vector3 position) GetFreeSlotPinggir()
    {
        if (!isInitialized) InitializeSlots();
        
        float maxLeft = float.MaxValue;
        float maxRight = float.MinValue;
        int leftSlot = -1;
        int rightSlot = -1;
        
        for (int i = 0; i < slotMarkers.Count; i++)
        {
            if (!usedSlots.Contains(slotMarkers[i]))
            {
                float x = slotMarkers[i].position.x;
                
                if (x < maxLeft)
                {
                    maxLeft = x;
                    leftSlot = i;
                }
                
                if (x > maxRight)
                {
                    maxRight = x;
                    rightSlot = i;
                }
            }
        }
        
        // Pilih kiri atau kanan random
        if (leftSlot >= 0 && rightSlot >= 0)
        {
            int chosenSlot = Random.value > 0.5f ? leftSlot : rightSlot;
            Transform slot = slotMarkers[chosenSlot];
            usedSlots.Add(slot);
            return (chosenSlot, slot.position);
        }
        else if (leftSlot >= 0)
        {
            Transform slot = slotMarkers[leftSlot];
            usedSlots.Add(slot);
            return (leftSlot, slot.position);
        }
        else if (rightSlot >= 0)
        {
            Transform slot = slotMarkers[rightSlot];
            usedSlots.Add(slot);
            return (rightSlot, slot.position);
        }
        
        return GetFreeSlot();
    }

    public void ReleaseSlot(int index)
    {
        if (index >= 0 && index < slotMarkers.Count)
        {
            usedSlots.Remove(slotMarkers[index]);
        }
    }
    
    public void ResetAllSlots()
    {
        usedSlots.Clear();
        Debug.Log("All slots reset");
    }
    
    void OnDrawGizmos()
    {
        if (slotsParent == null) return;
        
        foreach (Transform child in slotsParent)
        {
            if (!child.gameObject.activeSelf) continue;
            
            bool isUsed = false;
            #if UNITY_EDITOR
            if (Application.isPlaying && Instance != null)
                isUsed = Instance.usedSlots.Contains(child);
            #endif
            
            Gizmos.color = isUsed ? Color.red : Color.green;
            Gizmos.DrawWireSphere(child.position, 0.3f);
        }
    }
}