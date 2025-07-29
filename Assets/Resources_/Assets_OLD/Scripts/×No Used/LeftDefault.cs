using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeftDefault : MonoBehaviour
{
    public bool inGround = true;
    public bool inTarget = true;
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
    }
    private void OnTriggerStay2D(Collider2D Collider)
    {
        if (Collider.tag.Contains("Ground") || Collider.tag.Contains("itemB") || Collider.tag.Contains("Enemy") || Collider.tag.Contains("Trap") || Collider.tag.Contains("Broken"))
            inGround = true;
        if (Collider.tag.Contains("WarpTarget"))
            inTarget = true;
    }
    private void OnTriggerExit2D(Collider2D Collider)
    {
        if (Collider.tag.Contains("Ground") || Collider.tag.Contains("itemB") || Collider.tag.Contains("Enemy") || Collider.tag.Contains("Trap") || Collider.tag.Contains("Broken"))
            inGround = false;
        if (Collider.tag.Contains("WarpTarget"))
            inTarget = false;
    }
}
