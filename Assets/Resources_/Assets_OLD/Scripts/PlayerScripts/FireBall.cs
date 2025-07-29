//using Cinemachine;
using UnityEngine;
using DG.Tweening;

public class FireBall : MonoBehaviour
{
    static bool shoot;
    public GameObject target;
    GameObject Explosion;

    public static void ShootFlag(bool b)
    {
        shoot = b;
    }


    // Update is called once per frame
    void Update()
    {
        if (GameManager.currentGameState != GameState.Playing)
            return;

        if (!target || target.GetComponent<Enemy>().HP <= 0)
        {
            DOTween.Clear();
            Destroy(gameObject);
        }
        if (shoot && gameObject)
        {
            DOTween.To
            (
                () => transform.position,       //何に
                x => transform.position = x,  //何を
                target.transform.position + target.transform.up * 2,     //どこまで(最終的な値)
                0.05f       //どれくらいの時間
            );
        }
        else
        {
            transform.position += (target.transform.position - transform.position) / 5000;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.Equals(target))
        {
            target.GetComponent<Enemy>().Damage(50, 0);
            GetComponent<SpriteRenderer>().enabled = false;
            Explosion = (GameObject)Instantiate(Resources.Load("Explosion_red"), transform.position, Quaternion.identity);
            Destroy(gameObject, 1f);
        }
    }
}
