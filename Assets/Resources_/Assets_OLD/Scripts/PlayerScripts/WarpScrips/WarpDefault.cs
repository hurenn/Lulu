using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarpDefault : MonoBehaviour
{
    [SerializeField]
    bool inGround = true;
    [SerializeField]
    bool inTarget = true;
    // Use this for initialization
    void Start()
    {

    }

    public bool GroundCheck()
    {
        return inGround;
    }
    public bool TargetCheck()
    {
        return inTarget;
    }

    public bool DoubleCheck()
    {
        return inGround || inTarget;
    }

    // Update is called once per frame
    void Update()
    {
    }
    private void OnTriggerStay2D(Collider2D Collider)
    {
        if (Collider.tag.Contains("Ground") || Collider.tag.Contains("Enemy") || Collider.tag.Contains("Trap") || Collider.tag.Contains("Broken"))
            inGround = true;
        if (Collider.tag.Contains("WarpTarget"))
            inTarget = true;
    }
    private void OnTriggerExit2D(Collider2D Collider)
    {
        if (Collider.tag.Contains("Ground") || Collider.tag.Contains("Enemy") || Collider.tag.Contains("Trap") || Collider.tag.Contains("Broken"))
            inGround = false;
        if (Collider.tag.Contains("WarpTarget"))
            inTarget = false;
    }
}
