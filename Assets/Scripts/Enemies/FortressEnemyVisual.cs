using System.Collections;
using UnityEngine;
using KingdomBorder.TwinTowers;

[RequireComponent(typeof(WaypointEnemy), typeof(EnemyAgent))]
public sealed class FortressEnemyVisual : MonoBehaviour
{
    private static Sprite barSprite;
    private EnemyAgent agent;
    private SpriteRenderer body, fill, back;
    private Transform barRoot;
    private float lastHealth, flashUntil, barWidth;
    private bool dying;

    private void Start()
    {
        agent = GetComponent<EnemyAgent>();
        body = GetComponent<SpriteRenderer>();
        lastHealth = agent.Health;
        if (barSprite == null)
            barSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0,0,Texture2D.whiteTexture.width,Texture2D.whiteTexture.height), new Vector2(0.5f,0.5f), Texture2D.whiteTexture.width);
        var root = new GameObject("Health Bar");
        root.transform.SetParent(transform, false); barRoot = root.transform;
        var definition = GetComponent<WaypointEnemy>().definition;
        barWidth = definition && definition.kind == FortressEnemyKind.TempestSorcerer ? 1.3f
            : definition && (definition.kind == FortressEnemyKind.ArmoredOrc || definition.kind == FortressEnemyKind.RiftGuard) ? 0.8f : 0.55f;
        back = MakeBar("Background", new Color(0.08f,0.05f,0.06f,0.95f), 100);
        fill = MakeBar("Health", new Color(0.3f,0.85f,0.3f), 101);
    }
    private SpriteRenderer MakeBar(string title, Color color, int order)
    {
        var go = new GameObject(title); go.transform.SetParent(barRoot,false);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite=barSprite; sr.color=color;
        sr.sharedMaterial=body.sharedMaterial; sr.sortingOrder=order; return sr;
    }
    private void LateUpdate()
    {
        if (dying || agent == null || body == null || fill == null) return;
        if (agent.Health < lastHealth) flashUntil = Time.time + 0.10f;
        lastHealth = agent.Health;
        if (Time.time < flashUntil) body.color = new Color(1,0.72f,0.65f,1);
        float ratio = Mathf.Clamp01(agent.Health / agent.maxHealth);
        barRoot.position = new Vector3(transform.position.x, body.bounds.max.y + 0.10f, transform.position.z);
        back.transform.localScale = new Vector3(barWidth+0.04f,0.09f,1);
        fill.transform.localScale = new Vector3(barWidth*ratio,0.05f,1);
        fill.transform.localPosition = new Vector3(-barWidth*(1-ratio)*0.5f,0,-0.01f);
        fill.color = Color.Lerp(new Color(0.95f,0.2f,0.12f),new Color(0.35f,0.85f,0.3f),ratio);
    }
    public void PlayDeath()
    {
        if (dying) return;
        dying=true;
        if (barRoot != null) barRoot.gameObject.SetActive(false);
        var animator=GetComponent<Animator>(); if(animator != null) animator.enabled=false;
        StartCoroutine(Death());
    }
    private IEnumerator Death()
    {
        var renderer=GetComponent<SpriteRenderer>();
        Vector3 scale=transform.localScale;
        for(float elapsed=0;elapsed<0.35f;elapsed+=Time.deltaTime)
        {
            float t=elapsed/0.35f;
            transform.localScale=new Vector3(scale.x*(1+0.15f*t),scale.y*(1-0.75f*t),scale.z);
            renderer.color=new Color(0.55f,0.55f,0.55f,1-t);
            yield return null;
        }
        Destroy(gameObject);
    }
}
