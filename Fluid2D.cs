using Godot;
using System;
using System.Diagnostics;

public partial class Fluid2D : Control
{
    int particleSize = 4;
    float restDensity = 0.003f;
    float stiffness = 2;
    float gamma = 7;
    float g = 1;
    [Export] int ParticleCount = 100;
    [Export] Vector2[] Positions;
    Vector2[] Velocities;
    Vector2[] Accelerations;
    float[] Densities;
    float[] Pressures;
    float mass = 1.0f;
    //scale is important to make the values not ridiculous
    float scale = 10;

    //nb: h is usually referred to as smoothing radius
    Slider SmoothingRadius;

    Random r;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Positions = new Vector2[ParticleCount];
        Velocities = new Vector2[ParticleCount];
        Accelerations = new Vector2[ParticleCount];

        Densities = new float[ParticleCount];
        Pressures = new float[ParticleCount];
        r = new Random();

        int x = 0;
        int y = 0;

        for (int i = 0; i < ParticleCount; i++)
        {
            Positions[i] = new Vector2(x * (particleSize * 2) + particleSize, y * (particleSize * 2) + particleSize);
            Positions[i] *= scale;
            x++;
            if ((x * (particleSize * 2) + particleSize) >= Size.X)
            {
                x = 0;
                y++;
            }
        }

        SmoothingRadius = GetNode<Slider>("SmoothingRadius");

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta)
    {
        QueueRedraw();
        CalculateDensity();
        for (int i = 0; i < ParticleCount; i++)
        {
            Accelerations[i] = Vector2.Zero;
            for (int j = 0; j < ParticleCount; j++)
            {
                if (i == j) continue;
                float dst = Positions[i].DistanceTo(Positions[j]);
                if (dst > SmoothingRadius.Value) continue;
                Vector2 dir = dst > 0 ? (Positions[i] - Positions[j]).Normalized() : new Vector2((float)r.NextDouble() * 2 - 1, (float)r.NextDouble() * 2 - 1).Normalized();
                dst = dst == 0 ? 1 : dst;
                //float slope = CalculatePressureDerivative(Densities[i]) * WeightedDensityDerivative(dst, (float)SmoothingRadius.Value * scale);
                float slope = CalculatePressureDerivative(Densities[i]);
                Accelerations[i] += dir * slope;
            }
        }

        UpdateParticle((float)delta);
    }

    void UpdateParticle(float delta)
    {
        for (int i = 0; i < ParticleCount; i++)
        {
            Velocities[i] += Accelerations[i] * delta;
            Positions[i] += Velocities[i] * delta;

            if (Positions[i].X <= 0)
            {
                Positions[i].X = 0;
                Velocities[i].X *= -0.5f;
            }

            if (Positions[i].Y <= 0)
            {
                Positions[i].Y = 0;
                Velocities[i].Y *= -0.5f;
            }

            if (Positions[i].X >= Size.X * scale)
            {
                Positions[i].X = Size.X * scale;
                Velocities[i].X *= -0.5f;
            }

            if (Positions[i].Y >= Size.Y * scale)
            {
                Positions[i].Y = Size.Y * scale;
                Velocities[i].Y *= -0.5f;
            }
        }
    }

    public override void _Draw()
    {
        base._Draw();
        for (int i = 0; i < ParticleCount; i++)
        {
            DrawCircle(Positions[i] / scale, particleSize, Color.FromHsv(280 / 360, 1, 1));
        }

    }

    //NOTE: For 3D we need to use a different weight kernel
    //r is usually denoted as distance
    static float WeightedDensityKernel(float distance, float h)
    {
        if (distance <= 0 && distance > h) return 0;
        float h2 = h * h;
        float r2 = distance * distance;

        return (4 / (MathF.PI * MathF.Pow(h, 8))) * Mathf.Pow(h2 - r2, 3);
    }

    //I have no clue why this is the answer, just roll with it
    static float WeightedDensityDerivative(float dst, float radius)
    {
        if (dst > radius) return 0;
        return dst * (-24 / Mathf.Pi * Mathf.Pow(radius, 8));
    }

    void CalculateDensity()
    {
        for (int i = 0; i < ParticleCount; i++)
        {
            Densities[i] = 0;
            for (int j = 0; j < ParticleCount; j++)
            {
                if (i == j) { continue; }
                Densities[i] += WeightedDensityKernel(Positions[i].DistanceTo(Positions[j]), (float)SmoothingRadius.Value);
            }
        }
    }

    void CalculatePressure()
    {
        for (int i = 0; i < ParticleCount; i++)
        {
            var p = (stiffness * restDensity / gamma) * (MathF.Pow(Densities[i] / restDensity, gamma) - 1);
            Pressures[i] = p > 0 ? p : 0;
        }
    }

    //Did this myself heh >:3
    float CalculatePressureDerivative(float density)
    {
        return (gamma * stiffness / gamma) * Mathf.Pow(density / restDensity, gamma - 1);
    }
}
