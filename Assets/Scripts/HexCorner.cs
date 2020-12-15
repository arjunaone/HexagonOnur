using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HexCorner : MonoBehaviour
{
    public List<Vector2Int> childHexagons;
    public int triangleRotation; // there are 2 types of corners. left aligned, right aligned
    public bool clicked;
    // Start is called before the first frame update
    void Awake()
    {
        childHexagons = new List<Vector2Int>();
        triangleRotation = 0;
    }

    // Update is called once per frame
    void Update()
    {
        //Selection event
        if (Input.GetMouseButtonUp(0))
        {
            if (clicked && !MainScr.instance.rotating && MainScr.instance.started)
            {
                
                MainScr.instance.selector.transform.position = transform.position;
                MainScr.instance.selector.transform.localRotation = Quaternion.Euler(0f, 0f, triangleRotation * 60f);
                MainScr.instance.selectedCorner = this;
                //for (int i = 0; i < childHexagons.Count; i++)
                //{
                    //MainScr.instance.hexList[childHexagons[i].x, childHexagons[i].y].gameObject.GetComponent<SpriteRenderer>().color = Color.white;
                //}
            }
            clicked = false;
        }
    }

    private void OnMouseDown()
    {
        clicked = true;
    }
}