using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Explosion : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Color color;
    // Start is called before the first frame update
    void Start()
    {
        
        transform.DOScale(new Vector3(1.2f, 1.2f, 1f), 0.6f);
        spriteRenderer.DOColor(new Color(1f, 1f, 1f, 0f), 0.6f);
        Destroy(gameObject, 1f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
