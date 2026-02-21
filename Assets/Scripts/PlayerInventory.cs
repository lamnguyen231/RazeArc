using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Stats")]
    public int playerHealth;

    [Header("Keycards")]
    public bool hasRedKey = false;
    public bool hasBlueKey = false;
    public bool hasGreenKey = false;

    public void PickUpKey(string keyColor)
    {
        if (keyColor == "Red") hasRedKey = true;
        else if (keyColor == "Blue") hasBlueKey = true;
        else if (keyColor == "Green") hasGreenKey = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
