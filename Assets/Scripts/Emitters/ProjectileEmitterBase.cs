﻿using UnityEngine;

public abstract class ProjectileEmitterBase : MonoBehaviour
{
    private Mesh Mesh;
    private Material Material;

    [SerializeField]
    protected float TimeToLive = 5;

    [SerializeField]
    protected Vector2 Direction = Vector2.up;

    [SerializeField]
    protected Gradient Color;

    [SerializeField]
    [Range(0.001f, 5f)]
    protected float INTERVAL = 0.1f;
    private float Interval;

    [SerializeField]
    [Range(0.001f, 10f)]
    protected float Speed = 1;

    [SerializeField]
    [Range(-10f, 10f)]
    protected float Acceleration = 0;

    [SerializeField]
    [Range(0.01f, 2f)]
    protected float Scale = 0.05f;
    
    [SerializeField]
    protected float RotationSpeed = 0;

    public bool BounceOffSurfaces;
    public ProjectileType ProjectileType;

    // Each emitter has its own ProjectileData pool
    protected Pool<ProjectileData> Projectiles;    

    // Collision layer
    private int LayerMask = 1;        
    private RaycastHit2D[] RaycastHitBuffer = new RaycastHit2D[1];

    // Current active projectiles from this emitter
    public int ActiveProjectileCount { get; private set; }   

    public void Start()
    {         
        Interval = INTERVAL;

        // If projectile type is not set, use default
        if (ProjectileType == null)
            ProjectileType = ProjectileManager.Instance.GetProjectileType(0);     
    }

    public void Initialize(int size)
    {
        Projectiles = new Pool<ProjectileData>(size);
    }

    public void UpdateEmitter()
    {
        Interval -= Time.deltaTime;
        UpdateProjectiles(Time.deltaTime);
    }

    public void ResolveLeakedTime()
    {
        // Spawn in new projectiles for next frame
        while (Interval <= 0)
        {
            float leakedTime = Mathf.Abs(Interval);
            Interval += INTERVAL;
            FireProjectile(Direction, leakedTime);
        }
    }
    
    // Function to rotate a vector by x degrees
    public static Vector2 Rotate(Vector2 v, float degrees)
    {
        float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
        float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

        float tx = v.x;
        float ty = v.y;

        v.x = (cos * tx) - (sin * ty);
        v.y = (sin * tx) + (cos * ty);

        return v;
    }

    protected abstract void FireProjectile(Vector2 direction, float leakedTime);
 
    private void UpdateProjectiles(float tick)
    {
        ActiveProjectileCount = 0;

        for (int i = 0; i < Projectiles.Nodes.Length; i++)
        {
            if (Projectiles.Nodes[i].Active)
            {
                Projectiles.Nodes[i].Item.TimeToLive -= tick;

                // Projectile is active
                if (Projectiles.Nodes[i].Item.TimeToLive > 0)
                {
                    // apply acceleration
                    Projectiles.Nodes[i].Item.Velocity *= (1 + Projectiles.Nodes[i].Item.Acceleration * tick);

                    // calculate where projectile will be at the end of this frame
                    Vector2 endPoint = Projectiles.Nodes[i].Item.Position + Projectiles.Nodes[i].Item.Velocity * tick;
                    float distance = (endPoint - Projectiles.Nodes[i].Item.Position).magnitude;

                    // Raycast towards where projectile is moving
                    if (Physics2D.RaycastNonAlloc(Projectiles.Nodes[i].Item.Position, Projectiles.Nodes[i].Item.Velocity.normalized, RaycastHitBuffer, distance, LayerMask) > 0)
                    {
                        // Put whatever hit code you want here such as damage events

                        // Collision was detected, should we bounce off or destroy the projectile?
                        if (BounceOffSurfaces)
                        {
                            // rudementary bounce -- will work well on static surfaces
                            Projectiles.Nodes[i].Item.Velocity = Vector2.Reflect(Projectiles.Nodes[i].Item.Velocity, RaycastHitBuffer[0].normal);

                            Projectiles.Nodes[i].Item.Position += Projectiles.Nodes[i].Item.Velocity * tick;
                            Projectiles.Nodes[i].Item.Color = Color.Evaluate(1 - Projectiles.Nodes[i].Item.TimeToLive / TimeToLive);

                            ProjectileManager.Instance.UpdateBufferData(ActiveProjectileCount, ProjectileType, Projectiles.Nodes[i].Item);

                            ActiveProjectileCount++;
                        }
                        else
                        {                            
                            Projectiles.Nodes[i].Item.TimeToLive = -1;
                            Projectiles.Return(Projectiles.Nodes[i].NodeIndex);
                        }
                    }
                    else
                    {
                        // No collision - move projectile
                        Projectiles.Nodes[i].Item.Position += Projectiles.Nodes[i].Item.Velocity * tick;
                        Projectiles.Nodes[i].Item.Color = Color.Evaluate(1 - Projectiles.Nodes[i].Item.TimeToLive / TimeToLive);

                        ProjectileManager.Instance.UpdateBufferData(ActiveProjectileCount, ProjectileType, Projectiles.Nodes[i].Item);

                        ActiveProjectileCount++;
                    }
                }
                else
                {
                    // End of life - return to pool
                    Projectiles.Return(Projectiles.Nodes[i].NodeIndex);
                }
            }
        }
    }

    public void ClearAllProjectiles()
    {
        for (int i = 0; i < Projectiles.Nodes.Length; i++)
        {
            if (Projectiles.Nodes[i].Active)
            {               
                Projectiles.Nodes[i].Item.TimeToLive = -1;
                Projectiles.Return(Projectiles.Nodes[i].NodeIndex);                
            }
        }
    }

}
