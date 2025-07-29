using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Thunder : MonoBehaviour
{
    public int Gems = 3;
    bool gemAppear = false;
    GameObject Gem;
    GameObject Scatter;
    RaycastHit2D hit;
    int groundMask = 1 << 9;
    Vector3 hitPos;
    //public int GemSize = 5;

    // Start is called before the first frame update
    void Start()
    {
        Gem = (GameObject)Resources.Load("GemScatter");
        //Gem.GetComponent<GemScatter>().size = GemSize;
    }

    // Update is called once per frame
    void Update()
    {
        hit = Physics2D.Raycast(transform.position + transform.right, new Vector3(-5, 0, 0), 5, groundMask);
        Debug.DrawRay(transform.position + transform.right, new Vector3(-5, 0, 0), Color.green, 2);

        if (!hit)
        {
            hit = Physics2D.Raycast(transform.position - transform.right, new Vector3(5, 0, 0), 5, groundMask);
            Debug.DrawRay(transform.position - transform.right, new Vector3(5, 0, 0), Color.green, 2);
        }

        if (hit)
        {
            hitPos = hit.point;
            if (hitPos.x < 0)
            {
                hitPos += transform.right;
            }
            else
            {
                hitPos -= transform.right;
            }
            if (hitPos.y < 0)
            {
                hitPos += transform.up;
            }
            else
            {
                hitPos -= transform.up;
            }

            if (!gemAppear)
            {
                StartCoroutine("GemAppear");
            }

        }
    }
    IEnumerator GemAppear()
    {
        gemAppear = true;
        yield return new WaitForSeconds(0.2f);
        CameraImpulse.StartImpulse();
        Gem.GetComponent<GemScatter>().size = Gems;
        Scatter = (GameObject)Instantiate(Gem, hitPos, Quaternion.identity, this.transform);
        Scatter.transform.parent = null;
        Scatter.transform.localScale = new Vector3(1, 1, 1);
        Scatter.GetComponent<GemScatter>().autoFollow = false;
        Destroy(Scatter, 3f);
        Destroy(this, 3f);
        //GemScatter.GetComponent<GemScatter>().size = 5;
    }
}
