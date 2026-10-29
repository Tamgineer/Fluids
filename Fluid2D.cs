using Godot;
using System;
using System.Diagnostics;

public partial class Fluid2D : Control
{
    int particleSize = 4;
    float restDensity = 0.003f;
    float stiffness = 1f;
    float gamma = 7; // This number is pretty much a magic number. I don't know how it affects the pressure formula.
    float g = 1;
    [Export] int ParticleCount = 100;
    [Export] Vector2[] Positions;
    [Export] Vector2[] Velocities;
    [Export] Vector2[] Accelerations;
    [Export] float[] Densities;
    float[] Pressures;
    float mass = 1.0f;
    //scale is important to make the values not ridiculous
    float scale = 10;

    //nb: h is usually referred to as smoothing radius
    float SmoothingRadius = 8;

    Random r;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        //the scale affects the smoothing radius
        SmoothingRadius *= scale;

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

        //CalculateDensity();
        //Sampling();

        //CalculateDensity();
        //CalculatePressure();
        //ComputeForces();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta)
    {
        QueueRedraw();
        CalculateDensity();
        CalculatePressure();
        ComputeForces();

        UpdateParticle((float)delta);
    }

    void ComputeForces()
    {
        for (int i = 0; i < ParticleCount; i++)
        {
            //Accelerations[i] = Vector2.Down * 98.1f;
            Accelerations[i] = Vector2.Zero;
            for (int j = 0; j < ParticleCount; j++)
            {
                if (i == j) continue;
                var rij = Positions[i] - Positions[j];

                if(rij.Length() > 0)
                {
                    var term = mass * ((Pressures[i] + Pressures[j]) / (2 * Densities[j]));
                    var gradient = SpikeyGradient(rij, SmoothingRadius);
                    Accelerations[i] -= gradient * term;
                    //Debug.Print($"{i}->{j}: {term}, {gradient}, {Accelerations[i]}");
                }
            }
        }
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
        if (distance >= 0 && distance < h)
        {
            float h2 = h * h;
            float r2 = distance * distance;

            return (4 / (MathF.PI * MathF.Pow(h, 8))) * Mathf.Pow(h2 - r2, 3);
        } else
        {
            return 0;
        }
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
                Densities[i] += WeightedDensityKernel(Positions[i].DistanceTo(Positions[j]), SmoothingRadius);
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
        if (density < float.Epsilon) return 0;
        return (gamma * stiffness / gamma) * Mathf.Pow(density / restDensity, gamma - 1);
    }

    //spikey gradient from https://www.youtube.com/watch?v=7wikWMujUu4
    static Vector2 SpikeyGradient(Vector2 rij, float h)
    {
        var r = rij.Length();
        if(r > float.Epsilon && r < h)
        {
            rij = rij.Normalized();
            var factor = -(30/(MathF.PI * MathF.Pow(h, 5))) * MathF.Pow(h - r, 2);
            return rij * factor;
        } else
        {
            return Vector2.Zero;
        }
    }
}
