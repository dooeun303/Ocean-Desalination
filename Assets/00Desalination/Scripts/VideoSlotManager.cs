using System.Collections.Generic;
using UnityEngine;

public class VideoSlotManager : MonoBehaviour
{
    public static VideoSlotManager Instance;

    public Transform[] slots;

    private Dictionary<uint, Transform> userSlotMap = new();

    private void Awake()
    {
        Instance = this;
    }

    // ΩΩ∑‘ «“¥Á
    public Transform AssignSlot(uint uid)
    {
        if (userSlotMap.ContainsKey(uid))
            return userSlotMap[uid];

        foreach (var slot in slots)
        {
            if (slot.childCount == 0)
            {
                userSlotMap[uid] = slot;
                return slot;
            }
        }

        Debug.LogWarning("No empty video slot available");
        return null;
    }

    // ΩΩ∑‘ «ÿ¡¶ (?)
    public void ReleaseSlot(uint uid)
    {
        if (!userSlotMap.TryGetValue(uid, out var slot))
            return;

        foreach (Transform child in slot)
        {
            Destroy(child.gameObject);
        }

        userSlotMap.Remove(uid);
    }
}
