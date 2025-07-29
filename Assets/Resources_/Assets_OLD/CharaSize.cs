using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharaSize : MonoBehaviour
{
    string character;
    string characterName = "";
    Dictionary<string, Vector2> resize = new Dictionary<string, Vector2>()
    {
        {"ルル", new Vector2(130, 234) },
        {"マルリカ", new Vector2(83, 232) },
        {"ノード", new Vector2(123, 271) },
        {"ペペ", new Vector2(143, 300) },
        {"ミリー", new Vector2(80, 190) },
    };

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        character = transform.parent.GetChild(1).gameObject.GetComponent<Text>().text;
        if (character == characterName || resize.ContainsKey(key: character) == false)
            return;

        GetComponent<RectTransform>().sizeDelta = new Vector2(resize[character][0], resize[character][1]);
        characterName = character;
    }
}
