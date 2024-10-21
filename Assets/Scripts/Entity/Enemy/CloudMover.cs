using UnityEngine;

using Photon.Pun;
using NSMB.Utils;

public class CloudMover : KillableEntity {

    public float cloudTimer, speed, playerSearchRadius = 4, despawnDistance = 8;
    private Vector2 searchVector;

    public new void Start() {
        base.Start();
        //photonView.RPC(nameof(SpawnParticle), RpcTarget.All, $"Prefabs/Particle/CloudTimer", body.position + new Vector2(0.0f,0.1f));
        cloudTimer = 7.0f;
        searchVector = new Vector2(playerSearchRadius * 2, 100);
        left = photonView && photonView.InstantiationData != null && (bool) photonView.InstantiationData[0];

        Transform t = transform.GetChild(1);
        ParticleSystem ps = t.GetComponent<ParticleSystem>();
        ParticleSystem.ShapeModule shape = ps.shape;

        ps.Play();
        sRenderer.flipX = !left;
    }

    public new void FixedUpdate() {
        TickCounters();
        if (GameManager.Instance && GameManager.Instance.gameover) {
            body.velocity = Vector2.zero;
            body.angularVelocity = 0;
            animator.enabled = false;
            body.isKinematic = true;
            return;
        }
        if (Frozen) {
            body.velocity = Vector2.zero;
        } else {
            body.velocity = new(speed * (left ? -1 : 1), body.velocity.y);
        }

        if (!Frozen && photonView.IsMine )
            DespawnCheck();

        if (cloudTimer <= 0)
        {
            photonView.RPC(nameof(SpawnParticle), RpcTarget.All, $"Prefabs/Particle/CloudParts", body.position);
            PhotonNetwork.Destroy(photonView);
        }
    }
    public override void InteractWithPlayer(PlayerController player) {
        if (Frozen || player.Frozen)
            return;

        Vector2 damageDirection = (player.body.position - body.position).normalized;
        bool attackedFromAbove = Vector2.Dot(damageDirection, Vector2.up) > 0.5f;

        if (player.invincible > 0 || ((player.groundpound || player.drill) && player.state != Enums.PowerupState.MiniMushroom)
            || player.state == Enums.PowerupState.MegaMushroom) {

            photonView.RPC(nameof(SpawnParticle), RpcTarget.All, $"Prefabs/Particle/CloudParts", body.position);
            PhotonNetwork.Destroy(photonView);
            return;
        }
    }

    [PunRPC]
    protected void SpawnParticle(string particle, Vector2 worldPos)
    {
        Instantiate(Resources.Load(particle), worldPos, Quaternion.identity);
    }

    private void DespawnCheck() {
        foreach (PlayerController player in GameManager.Instance.players) {
            if (!player)
                continue;

            if (Utils.WrappedDistance(player.body.position, body.position) < despawnDistance)
                return;
        }

        PhotonNetwork.Destroy(photonView);
    }

    [PunRPC]
    public override void Kill() {
        SpecialKill(!left, false, 0);
    }

    [PunRPC]
    public override void SpecialKill(bool right, bool groundpound, int combo) {
        body.velocity = new Vector2(0, 2.5f);
        body.constraints = RigidbodyConstraints2D.None;
        body.angularVelocity = 400f * (right ? 1 : -1);
        body.gravityScale = 1.5f;
        body.isKinematic = false;
        hitbox.enabled = false;
        animator.speed = 0;
        gameObject.layer = LayerMask.NameToLayer("HitsNothing");
        if (groundpound)
            Instantiate(Resources.Load("Prefabs/Particle/EnemySpecialKill"), body.position + new Vector2(0, 0.5f), Quaternion.identity);

        dead = true;
        PlaySound(Enums.Sounds.Enemy_Shell_Kick);
    }

    public void OnDrawGizmosSelected() {
        if (!GameManager.Instance)
            return;

        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawCube(body.position, searchVector);
        //left border check
        if (body.position.x - playerSearchRadius < GameManager.Instance.GetLevelMinX())
            Gizmos.DrawCube(body.position + new Vector2(GameManager.Instance.levelWidthTile * 0.5f, 0), searchVector);
        //right border check
        if (body.position.x + playerSearchRadius > GameManager.Instance.GetLevelMaxX())
            Gizmos.DrawCube(body.position - new Vector2(GameManager.Instance.levelWidthTile * 0.5f, 0), searchVector);
    }

    void TickCounters()
    {
        float delta = Time.fixedDeltaTime;
        Utils.TickTimer(ref cloudTimer, 0, delta);
    }
}