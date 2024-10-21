using System.Collections;
using UnityEngine;

using Photon.Pun;
using NSMB.Utils;

public class RedCoinBouncer : MonoBehaviourPun {

    private static int ANY_GROUND_MASK = -1;

    [SerializeField] private float pulseAmount = 0.2f, pulseSpeed = 0.2f, moveSpeed = 3f, bounceAmount = 4f, deathBoostAmount = 20f, lifespan = 15f;

    public Rigidbody2D body;
    public bool stationary = true, passthrough = true, left = true, fast = false;

    private SpriteRenderer sRenderer;
    private Transform graphicTransform;
    private PhysicsEntity physics;
    private BoxCollider2D worldCollider;
    private float pulseEffectCounter;
    private bool canBounce;

    private float collecttimer = 2f;
    public bool Collectable { get; private set; }
    public bool Collected { get; set; }

    public void Start() {
        body = GetComponent<Rigidbody2D>();
        physics = GetComponent<PhysicsEntity>();
        sRenderer = GetComponentInChildren<SpriteRenderer>();
        worldCollider = GetComponent<BoxCollider2D>();

        graphicTransform = transform.Find("Graphic");

        moveSpeed = moveSpeed * Random.Range(0.4f, 1.4f);

        object[] data = photonView.InstantiationData;
        if (data != null) {
            //player dropped star

            stationary = false;
            passthrough = true;
            sRenderer.color = new(1, 1, 1, 0.55f);
            gameObject.layer = Layers.LayerHitsNothing;
            int direction = (int) data[0];
            left = direction <= 1;
            fast = direction == 0 || direction == 3;
            body.velocity = new(moveSpeed * (left ? -1 : 1) * (fast ? 2f : 1f), deathBoostAmount * Random.Range(1f, 2.2f));

            //death via pit boost
            if ((bool) data[3])
                body.velocity += Vector2.up * 5;

            body.isKinematic = false;
            worldCollider.enabled = true;
        } else {
            //shouldn't be possible... but just in case.

            GetComponent<Animator>().enabled = true;
            Collectable = true;
            body.isKinematic = true;
            body.velocity = Vector2.zero;
            stationary = true;
            GetComponent<CustomRigidbodySerializer>().enabled = false;

            StartCoroutine(PulseEffect());

            if (GameManager.Instance.musicEnabled)
                GameManager.Instance.sfx.PlayOneShot(Enums.Sounds.World_Star_Spawn.GetClip());
        }

        if (ANY_GROUND_MASK == -1)
            ANY_GROUND_MASK = LayerMask.GetMask("Ground", "PassthroughInvalid");
    }

    public void Update() {
        if (GameManager.Instance?.gameover ?? false)
            return;

        if (stationary) {
            return;
        }

        lifespan -= Time.deltaTime;
    }

    public void FixedUpdate() {
        collecttimer -= Time.deltaTime;
        if (collecttimer <= 0) {
            Collectable = true;
        }

        if (stationary)
            return;

        if (GameManager.Instance?.gameover ?? false) {
            body.velocity = Vector2.zero;
            body.isKinematic = true;
            return;
        }

        body.velocity = new(moveSpeed * (left ? -1 : 1) * 2f, body.velocity.y);
        moveSpeed = moveSpeed * 0.98f;

        canBounce |= body.velocity.y < 0;
        Collectable |= body.velocity.y < 0;

        HandleCollision();

        if (passthrough && Collectable && body.velocity.y <= 0 && !Utils.IsAnyTileSolidBetweenWorldBox(body.position + worldCollider.offset, worldCollider.size * transform.lossyScale) && !Physics2D.OverlapBox(body.position, Vector2.one / 3, 0, ANY_GROUND_MASK)) {
            passthrough = false;
            gameObject.layer = Layers.LayerEntity;
            sRenderer.color = Color.white;
        }
        if (!passthrough) {
            if (Utils.IsAnyTileSolidBetweenWorldBox(body.position + worldCollider.offset, worldCollider.size * transform.lossyScale)) {
                gameObject.layer = Layers.LayerHitsNothing;
            } else {
                gameObject.layer = Layers.LayerEntity;
            }
        }

        if (photonView.IsMine && (lifespan <= 0 || (!passthrough && body.position.y < GameManager.Instance.GetLevelMinY())))
            photonView.RPC("Crushed", RpcTarget.All);
    }

    private IEnumerator PulseEffect() {
        while (true) {
            pulseEffectCounter += Time.deltaTime;
            float sin = Mathf.Sin(pulseEffectCounter * pulseSpeed) * pulseAmount;
            graphicTransform.localScale = Vector3.one * 3f + new Vector3(sin, sin, 0);

            yield return null;
        }
    }

    private void HandleCollision() {
        physics.UpdateCollisions();

        if (physics.hitLeft || physics.hitRight) {
            if (photonView.IsMine)
                photonView.RPC("Turnaround", RpcTarget.All, physics.hitLeft);
            else
                Turnaround(physics.hitLeft);
        }

        if (physics.onGround && canBounce) {
            body.velocity = new(body.velocity.x, bounceAmount);
            if (bounceAmount != 0f) { bounceAmount = bounceAmount / 2.5f; }
            if (bounceAmount < 0.25f) { bounceAmount = 0f; }
            if (photonView.IsMine && physics.hitRoof)
                photonView.RPC("Crushed", RpcTarget.All);
        }
    }

    public void DisableAnimator() {
        GetComponent<Animator>().enabled = false;
    }

    [PunRPC]
    public void Crushed() {
        if (photonView.IsMine)
            PhotonNetwork.Destroy(gameObject);

        Instantiate(Resources.Load("Prefabs/Particle/Puff"), transform.position, Quaternion.identity);
    }

    [PunRPC]
    public void Turnaround(bool hitLeft) {
        left = !hitLeft;
        body.velocity = new(moveSpeed * (left ? -1 : 1), body.velocity.y);
    }
}
